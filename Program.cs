using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Hubs;
using Nirvachak_AI.Infrastructure;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ?? Database ??????????????????????????????????????????????????
// On Railway: set DATABASE_PATH=/data/election.db  (volume mounted at /data)
// Locally:    defaults to election.db in working directory
var isProduction = !builder.Environment.IsDevelopment();

// Resolve the raw DB path from env var or config
var dbPathRaw = Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? (isProduction ? "/data/election.db" : "election.db");

// Strip "Data Source=" prefix to get the bare file path
var dbFilePart = dbPathRaw.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
    ? dbPathRaw["Data Source=".Length..].Trim()
    : dbPathRaw.Trim();

// Always resolve relative paths against the app's content root (project directory).
// This guarantees the same DB file is used regardless of the working directory —
// i.e., whether the app is launched via "dotnet run", Visual Studio, IIS Express, etc.
var dbFile = Path.IsPathRooted(dbFilePart)
    ? dbFilePart
    : Path.Combine(builder.Environment.ContentRootPath, dbFilePart);

// Build the final absolute connection string
var dbPath = $"Data Source={dbFile}";

// Log the active database path on every startup so it is visible in Railway deploy logs.
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"[DB] Active database path: {dbFile}");
if (isProduction && Environment.GetEnvironmentVariable("DATABASE_PATH") == null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[WARNING] DATABASE_PATH env var is not set. Defaulting to /data/election.db.");
    Console.WriteLine("[WARNING] Ensure a Railway Volume is mounted at /data for data persistence.");
}
Console.ResetColor();

// Ensure the directory exists (important for Railway volume path /data/)
var dbDir = Path.GetDirectoryName(dbFile);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
    Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(dbPath);
    // Child entities (DoorToDoorVisit, PhoneCallLog, etc.) have non-nullable VoterId FKs.
    // The global Voter query filter is intentional — child rows of a soft-deleted voter
    // are unreachable by design. Suppress the EF model-validation warning.
    options.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
            .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
});

// ?? Identity ??????????????????????????????????????????????????
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ?? Cookie Auth (Web) ?????????????????????????????????????????
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = Constants.Routes.LoginPath;
    options.LogoutPath = Constants.Routes.LogoutPath;
    options.AccessDeniedPath = Constants.Routes.AccessDeniedPath;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments(Constants.Routes.ApiPrefix))
            ctx.Response.StatusCode = 401;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments(Constants.Routes.ApiPrefix))
            ctx.Response.StatusCode = 403;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// ?? JWT Auth (Mobile API) ?????????????????????????????????????
var jwtKey = builder.Configuration[Constants.Jwt.ConfigKey]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it via environment variable or user secrets.");
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[Constants.Jwt.IssuerKey],
            ValidAudience = builder.Configuration[Constants.Jwt.AudienceKey],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ?? CORS (React Native / Expo) ????????????????????????????????
// Restrict to configured origins in production; fall back to AllowAnyOrigin in dev.
builder.Services.AddCors(o => o.AddPolicy(Constants.Policy.CorsAllowAll, p =>
{
    var allowedOrigins = builder.Configuration.GetSection(Constants.Cors.AllowedOriginsKey)
        .Get<string[]>();
    if (allowedOrigins is { Length: > 0 })
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

// ?? Application Services ??????????????????????????????????????
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<VoterImportService>();
builder.Services.AddScoped<ElectionDayService>();
builder.Services.AddScoped<VoterSlipService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PredictiveAnalyticsService>();
builder.Services.AddScoped<WinProbabilityService>();
builder.Services.AddScoped<IExotelService, ExotelService>();

// Named HTTP client for Exotel (30 s timeout)
builder.Services.AddHttpClient("exotel", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});

// Session (used for survey rate-limiting on public pages)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ?? SignalR ???????????????????????????????????????????????????
builder.Services.AddSignalR();

// ?? API Controllers ???????????????????????????????????????????
builder.Services.AddControllers();

// ?? Swagger / OpenAPI ?????????????????????????????????????????
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Election Campaign Tool API",
        Version = "v1",
        Description = "REST API for Web & Mobile App � India MLA & Ward Elections"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

// ?? Razor Pages ???????????????????????????????????????????????
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToFolder("/Survey");
});

var app = builder.Build();

// ?? Middleware Pipeline ???????????????????????????????????????
// Always expose Swagger (useful for Railway health-check & mobile API docs)
// Only expose Swagger in Development — do not expose API docs in production.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Election Campaign Tool API v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Railway terminates SSL at the load balancer — only redirect in local dev
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(Constants.Policy.CorsAllowAll);
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ElectionDayHub>("/hubs/electionday");

// ?? Seed Data ?????????????????????????????????????????????????
await SeedService.SeedAsync(app.Services);

// ?? PWA Icons � generated at startup (cross-platform, no external libs) ??
var wwwroot = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
PwaIconGenerator.EnsureIconsExist(wwwroot);

app.Run();
