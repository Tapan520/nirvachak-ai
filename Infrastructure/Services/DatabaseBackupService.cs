using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Hosted background service that creates a daily MySQL dump backup.
/// Enabled/disabled and configured by SuperAdmin via <see cref="BackupSettings"/>.
/// </summary>
public class DatabaseBackupService : BackgroundService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly BackupSettings _settings;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IWebHostEnvironment env,
        BackupSettings settings,
        IHttpClientFactory httpFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _env = env;
        _settings = settings;
        _httpFactory = httpFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("[Backup] Skipped - development environment.");
            return;
        }

        _logger.LogInformation("[Backup] Service started. Scheduled hour (UTC): {Hour}:00",
            _settings.ScheduleHour);

        var lastRanDate = DateTime.MinValue.Date;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollInterval, stoppingToken);

            if (!_settings.IsEnabled) continue;

            var now = DateTime.UtcNow;
            var targetHour = int.TryParse(_settings.ScheduleHour, out var h) ? h : 2;

            if (now.Hour == targetHour && now.Date != lastRanDate)
            {
                lastRanDate = now.Date;
                await PerformBackupAsync();
            }
        }
    }

    /// <summary>Trigger a backup immediately (called from SuperAdmin UI).</summary>
    public async Task RunNowAsync()
    {
        _logger.LogInformation("[Backup] Manual backup triggered by SuperAdmin.");
        await PerformBackupAsync();
    }

    private async Task PerformBackupAsync()
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogWarning("[Backup] Backup is disabled - skipping.");
            return;
        }

        try
        {
            var connectionString =
                Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("MySQL connection string is not configured.");

            var parts = ParseConnectionString(connectionString);
            var backupDir = _settings.BackupDirectory;
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFile = Path.Combine(backupDir, $"election_backup_{timestamp}.sql");

            var dumpExe = ResolveMysqldumpPath();
            if (dumpExe == null)
                throw new InvalidOperationException(
                    "mysqldump was not found. Install MySQL client tools or ensure mysqldump is on PATH.");

            var args =
                $"--host={Quote(parts.Server)} --port={parts.Port} --user={Quote(parts.User)} " +
                $"--single-transaction --routines --triggers --databases {Quote(parts.Database)} " +
                $"--result-file={Quote(backupFile)}";

            var psi = new ProcessStartInfo
            {
                FileName = dumpExe,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Prefer env var so password is not visible in process args.
            if (!string.IsNullOrEmpty(parts.Password))
                psi.Environment["MYSQL_PWD"] = parts.Password;

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start mysqldump process.");

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(backupFile) || new FileInfo(backupFile).Length == 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(stderr)
                        ? $"mysqldump failed with exit code {process.ExitCode}."
                        : stderr.Trim());
            }

            var size = new FileInfo(backupFile).Length;
            _logger.LogInformation("[Backup] Backup created: {File} ({Size:N0} bytes)", backupFile, size);

            _settings.LastBackupAt = DateTime.UtcNow;
            _settings.LastBackupFile = Path.GetFileName(backupFile);
            _settings.LastError = null;

            PruneOldBackups(backupDir);

            if (!string.IsNullOrWhiteSpace(_settings.CloudWebhookUrl))
                await UploadToWebhookAsync(backupFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] Backup failed.");
            _settings.LastError = ex.Message;
        }
    }

    private void PruneOldBackups(string backupDir)
    {
        var files = Directory.GetFiles(backupDir, "election_backup_*.sql")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var old in files.Skip(_settings.RetentionCount))
        {
            File.Delete(old);
            _logger.LogInformation("[Backup] Pruned old backup: {File}", old);
        }
    }

    private async Task UploadToWebhookAsync(string backupFile)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(backupFile);
            var b64 = Convert.ToBase64String(fileBytes);
            var fileName = Path.GetFileName(backupFile);
            var payload = new
            {
                fileName,
                fileSizeBytes = fileBytes.Length,
                timestamp = DateTime.UtcNow.ToString("o"),
                contentBase64 = b64
            };
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            var response = await client.PostAsJsonAsync(_settings.CloudWebhookUrl, payload);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("[Backup] Cloud upload succeeded -> {Url}", _settings.CloudWebhookUrl);
            else
                _logger.LogWarning("[Backup] Cloud upload returned {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] Cloud upload failed.");
        }
    }

    private static string? ResolveMysqldumpPath()
    {
        var candidates = new[]
        {
            @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 8.4\bin\mysqldump.exe",
            @"C:\Program Files\MySQL\MySQL Server 9.0\bin\mysqldump.exe",
            "/usr/bin/mysqldump",
            "/usr/local/bin/mysqldump",
            "mysqldump"
        };

        foreach (var c in candidates)
        {
            if (c.Equals("mysqldump", StringComparison.OrdinalIgnoreCase))
                return c;

            if (File.Exists(c))
                return c;
        }

        return null;
    }

    private static (string Server, int Port, string User, string Password, string Database) ParseConnectionString(string cs)
    {
        string Get(params string[] keys)
        {
            foreach (var key in keys)
            {
                var m = Regex.Match(cs, $@"(?:^|;)\s*{Regex.Escape(key)}\s*=\s*([^;]*)", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            return string.Empty;
        }

        var server = Get("Server", "Data Source", "Host");
        if (string.IsNullOrWhiteSpace(server)) server = "127.0.0.1";

        var portText = Get("Port");
        var port = int.TryParse(portText, out var p) ? p : 3306;

        var user = Get("User", "User ID", "Uid");
        if (string.IsNullOrWhiteSpace(user)) user = "root";

        var password = Get("Password", "Pwd");
        var database = Get("Database", "Initial Catalog");
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Connection string is missing Database name.");

        return (server, port, user, password, database);
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}

