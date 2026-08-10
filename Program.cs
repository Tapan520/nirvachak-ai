using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

var builder = WebApplication.CreateBuilder(args);

// ?? Database ??????????????????????????????????????????????????
// On Railway: set DATABASE_PATH=/data/election.db  (volume mounted at /data)
// Locally:    defaults to election.db in working directory
var isProduction = !builder.Environment.IsDevelopment();

// Resolve the raw DB path from env var or config.
// IMPORTANT: In production we must NOT fall back to the relative "election.db" path
// from appsettings.json — that resolves to /app/election.db inside the ephemeral
// container filesystem and data is lost on every redeploy.
// Priority: DATABASE_PATH env var → /data/election.db (Railway volume) in prod → appsettings fallback in dev only.
var dbPathRaw = Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? (isProduction
        ? "/data/election.db"
        : builder.Configuration.GetConnectionString("DefaultConnection") ?? "election.db");

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
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ?? Background Services ??????????????????????????????????????
builder.Services.AddSingleton<BackupSettings>();
builder.Services.AddSingleton<DatabaseBackupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBackupService>());
builder.Services.AddHostedService<ExpenseBudgetAlertService>();
builder.Services.AddHostedService<SwingVoterAlertService>();

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
    options.Conventions.AllowAnonymousToPage("/Account/TwoFactorLogin");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
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
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ElectionDayHub>("/hubs/electionday");

// Apply rate limit to public survey pages
app.MapGet("/Survey/{**slug}", () => Results.Ok())
   .RequireRateLimiting("survey")
   .WithDisplayName("Survey-GET-RateLimit");
app.MapPost("/Survey/{**slug}", () => Results.Ok())
   .RequireRateLimiting("survey")
   .WithDisplayName("Survey-POST-RateLimit");

// ?? Health Check ???????????????????????????????????????????
app.MapGet("/health", () =>
{
    var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "/data/election.db";
    var dbExists = File.Exists(dbPath);
    var dbSize = dbExists ? new FileInfo(dbPath).Length : 0;
    return Results.Ok(new
    {
        status  = "ok",
        time    = DateTime.UtcNow,
        db      = dbPath,
        dbReady = dbExists,
        dbBytes = dbSize
    });
}).AllowAnonymous();

// ?? Database Schema Initialisation ??????????????????????????
// Strategy (SAFE — never deletes existing data):
//   Fresh install (no DB file) → EnsureCreatedAsync() builds full schema, then
//                                 stamps all migration IDs so MigrateAsync is a no-op.
//   Existing DB               → MigrateAsync() safely applies only pending migrations
//                                 (adds columns / tables) without touching existing data.
//
// ⚠️  IMPORTANT: We intentionally do NOT delete the DB file on schema mismatch.
//     A missing column just means a pending migration — MigrateAsync handles it safely.
//     Deleting the DB would wipe all production data on Railway on every redeploy.
using (var dbInitScope = app.Services.CreateScope())
{
    var initDb = dbInitScope.ServiceProvider.GetRequiredService<AppDbContext>();
    bool dbExists = File.Exists(dbFile);

    if (!dbExists)
    {
        // ── Brand-new installation ─────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[DB] No database found — creating fresh schema via EnsureCreated.");
        Console.ResetColor();

        // Creates ALL tables/columns/indexes from the current EF model in one step.
        await initDb.Database.EnsureCreatedAsync();

        // Stamp every known migration as applied so future MigrateAsync calls are no-ops.
        await initDb.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                ""ProductVersion"" TEXT NOT NULL
            );");

        foreach (var migrationId in initDb.Database.GetMigrations())
        {
            await initDb.Database.ExecuteSqlRawAsync(
                $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{migrationId}', '8.0.11');");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[DB] Fresh schema created and all migrations stamped successfully.");
        Console.ResetColor();
    }
    else
    {
        // ── Existing database — apply pending migrations SAFELY ────────────
        // MigrateAsync() only adds missing tables/columns — it never deletes data.
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[DB] Existing database found — applying any pending migrations (data preserved).");
        Console.ResetColor();

        try
        {
            await initDb.Database.MigrateAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[DB] Migrations applied successfully.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            // Log the migration error but do NOT delete the DB — data is too valuable.
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[DB] Migration failed: {ex.Message}");
            Console.WriteLine("[DB] The application will continue with the existing schema.");
            Console.ResetColor();
        }
    }
}

// ?? WAL Mode ???????????????????????????????????????????????????
// Enable WAL (Write-Ahead Logging) so concurrent reads are not blocked by writes.
// Applied via PRAGMA because the connection string keyword is not supported by Microsoft.Data.Sqlite.
using (var walScope = app.Services.CreateScope())
{
    var walDb = walScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await walDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

// ?? Seed Data ?????????????????????????????????????????????????
await SeedService.SeedAsync(app.Services);

// ?? PWA Icons � generated at startup (cross-platform, no external libs) ??
var wwwroot = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
PwaIconGenerator.EnsureIconsExist(wwwroot);

app.Run();
