using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Hubs;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ?? Database ??????????????????????????????????????????????????
// On Railway: set DATABASE_PATH=/data/election.db  (volume mounted at /data)
// Locally:    defaults to election.db in working directory
var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=election.db";

// Ensure the directory exists (important for Railway volume path /data/)
var dbFile = dbPath.Replace("Data Source=", "").Trim();
var dbDir  = Path.GetDirectoryName(dbFile);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
    Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(dbPath.StartsWith("Data Source=") ? dbPath : $"Data Source={dbPath}"));

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
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = 401;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = 403;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// ?? JWT Auth (Mobile API) ?????????????????????????????????????
var jwtKey = builder.Configuration["Jwt:Key"]
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ?? CORS (React Native / Expo) ????????????????????????????????
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ?? Application Services ??????????????????????????????????????
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<VoterImportService>();
builder.Services.AddScoped<ElectionDayService>();
builder.Services.AddScoped<VoterSlipService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PredictiveAnalyticsService>();
builder.Services.AddScoped<WinProbabilityService>();

// Session (used for survey rate-limiting on public pages)
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
app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Election Campaign Tool API v1"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Railway terminates SSL at the load balancer — only redirect in local dev
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ElectionDayHub>("/hubs/electionday");

app.MapGet("/api/ElectionDayStats", async (int constituencyId, AppDbContext db) =>
{
    var total = await db.Voters.CountAsync(v => v.ConstituencyId == constituencyId);
    var voted = await db.Voters.CountAsync(v => v.ConstituencyId == constituencyId
        && v.ElectionDayStatus == ElectionDayStatus.Voted);
    var percent = total > 0 ? Math.Round((double)voted / total * 100, 1) : 0;
    return Results.Ok(new { total, voted, percent });
});

// ?? Seed Data ?????????????????????????????????????????????????
await SeedService.SeedAsync(app.Services);

// ?? PWA Icons � generated at startup (cross-platform, no external libs) ??
var wwwroot = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
PwaIconGenerator.EnsureIconsExist(wwwroot);

app.Run();
