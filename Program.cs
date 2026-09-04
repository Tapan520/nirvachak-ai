using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Hubs;
using Nirvachak_AI.Infrastructure;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ?? Database (MySQL) ??????????????????????????????????????????
// Priority:
//   1) MYSQL_CONNECTION_STRING env var (Railway / production)
//   2) ConnectionStrings:DefaultConnection (appsettings)
var connectionString =
    Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "MySQL connection string is not configured. Set MYSQL_CONNECTION_STRING or ConnectionStrings:DefaultConnection.");

// Avoid AutoDetect at design-time/startup failures when DB is temporarily unavailable.
// Prefer configured version; fall back to AutoDetect only when available.
ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = ServerVersion.Parse("8.0.36-mysql");
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("[DB] Provider: MySQL (Pomelo EF Core)");
Console.WriteLine($"[DB] ServerVersion: {serverVersion}");
Console.WriteLine($"[DB] Connection: {MaskConnectionString(connectionString)}");
Console.ResetColor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
    });

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
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ModuleAccessService>();

// ?? Background Services ??????????????????????????????????????
builder.Services.AddSingleton<BackupSettings>();
builder.Services.AddSingleton<DatabaseBackupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBackupService>());
builder.Services.AddHostedService<ExpenseBudgetAlertService>();
builder.Services.AddHostedService<SwingVoterAlertService>();
builder.Services.AddHostedService<DailyBriefingService>();
builder.Services.AddHostedService<NoonAlertService>();

// Named HTTP client for Exotel (30 s timeout)
builder.Services.AddHttpClient("exotel", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});

// Named HTTP client for Resend email API
builder.Services.AddHttpClient("resend", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.BaseAddress = new Uri("https://api.resend.com");
});

// Named HTTP client for Mailjet email API
builder.Services.AddHttpClient("mailjet", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.BaseAddress = new Uri("https://api.mailjet.com");
});

// Session (used for survey rate-limiting on public pages)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ?? SignalR ???????????????????????????????????????????????????
builder.Services.AddSignalR();

// ?? Rate Limiting (public survey pages) ??????????????????????
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("survey", limiterOptions =>
    {
        limiterOptions.PermitLimit         = 20;
        limiterOptions.Window              = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit          = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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
        Description = "REST API for Web & Mobile App - India MLA & Ward Elections"
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
    options.Conventions.AllowAnonymousToPage("/Account/TwoFactorLogin");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
    options.Conventions.AllowAnonymousToFolder("/Survey");
});

var app = builder.Build();

// ?? Middleware Pipeline ???????????????????????????????????????
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

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(Constants.Policy.CorsAllowAll);
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Nirvachak_AI.Infrastructure.Filters.ModuleAccessMiddleware>();
app.UseMiddleware<Nirvachak_AI.Infrastructure.Filters.VoterManagerAccessMiddleware>();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ElectionDayHub>("/hubs/electionday");

app.MapGet("/Survey/{**slug}", () => Results.Ok())
   .RequireRateLimiting("survey")
   .WithDisplayName("Survey-GET-RateLimit");
app.MapPost("/Survey/{**slug}", () => Results.Ok())
   .RequireRateLimiting("survey")
   .WithDisplayName("Survey-POST-RateLimit");

// ?? Health Check ???????????????????????????????????????????
app.MapGet("/health", async (AppDbContext db) =>
{
    var startTime = DateTime.UtcNow;
    var canConnect = false;
    double dbQueryMs = -1;
    int voterCount = -1;
    string? dataSource = null;

    try
    {
        canConnect = await db.Database.CanConnectAsync();
        dataSource = db.Database.GetDbConnection().DataSource;
        var dbQueryStart = DateTime.UtcNow;
        voterCount = await db.Voters.CountAsync();
        dbQueryMs = (DateTime.UtcNow - dbQueryStart).TotalMilliseconds;
    }
    catch
    {
        canConnect = false;
    }

    var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
    var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
    var assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

    return Results.Ok(new
    {
        status = canConnect ? "ok" : "degraded",
        provider = "mysql",
        build = "mysql-pomelo-20260904",
        assemblyVersion,
        time = DateTime.UtcNow,
        responseTimeMs = Math.Round(responseTime, 2),
        dbReady = canConnect,
        dbHost = dataSource,
        dbQueryMs = Math.Round(dbQueryMs, 2),
        voterCount,
        memoryUsageMB = Math.Round(memoryMB, 2),
        performance = dbQueryMs < 0 ? "unknown"
            : dbQueryMs < 100 ? "good"
            : dbQueryMs < 500 ? "acceptable"
            : "slow"
    });
}).AllowAnonymous();

// ?? Database Schema Initialisation (MySQL) ?????????????????
// Always apply pending EF migrations safely. Never drop or recreate the database.
using (var dbInitScope = app.Services.CreateScope())
{
    var initDb = dbInitScope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("[DB] Applying pending MySQL migrations (data preserved).");
    Console.ResetColor();

    try
    {
        await initDb.Database.MigrateAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[DB] MySQL migrations applied successfully.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[DB] Migration failed: {ex.Message}");
        Console.WriteLine("[DB] The application will continue; fix connection/migration before production use.");
        Console.ResetColor();
    }
}

// ?? Seed Data ?????????????????????????????????????????????????
await SeedService.SeedAsync(app.Services);

// ?? PWA Icons ?????????????????????????????????????????????????
var wwwroot = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
PwaIconGenerator.EnsureIconsExist(wwwroot);

app.Run();

static string MaskConnectionString(string value)
{
    // Hide password in logs while still showing host/db/user.
    return System.Text.RegularExpressions.Regex.Replace(
        value,
        "(Password|Pwd)=([^;]*)",
        "$1=****",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

