using Microsoft.Data.Sqlite;
using System.Net.Http.Json;

namespace Nirvachak_AI.Infrastructure.Services;


/// <summary>
/// Hosted background service that creates a daily SQLite backup of the production database.
/// Enabled/disabled and configured by SuperAdmin via <see cref="BackupSettings"/>.
/// </summary>
public class DatabaseBackupService : BackgroundService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly BackupSettings _settings;
    private readonly IHttpClientFactory _httpFactory;

    // Poll every minute to check if it's time to run the scheduled backup.
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IWebHostEnvironment env,
        BackupSettings settings,
        IHttpClientFactory httpFactory)
    {
        _logger      = logger;
        _env         = env;
        _settings    = settings;
        _httpFactory = httpFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("[Backup] Skipped — development environment.");
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
            _logger.LogWarning("[Backup] Backup is disabled — skipping.");
            return;
        }

        try
        {
            var dbPath = ResolveDbPath();
            if (!File.Exists(dbPath))
            {
                var err = $"Source DB not found at {dbPath}.";
                _logger.LogWarning("[Backup] {Err}", err);
                _settings.LastError = err;
                return;
            }

            var backupDir = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
            Directory.CreateDirectory(backupDir);

            var timestamp  = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFile = Path.Combine(backupDir, $"election_backup_{timestamp}.db");

            using var source = new SqliteConnection($"Data Source={dbPath}");
            using var dest   = new SqliteConnection($"Data Source={backupFile}");
            await source.OpenAsync();
            await dest.OpenAsync();
            source.BackupDatabase(dest);

            var size = new FileInfo(backupFile).Length;
            _logger.LogInformation("[Backup] ? Backup created: {File} ({Size:N0} bytes)", backupFile, size);

            _settings.LastBackupAt   = DateTime.UtcNow;
            _settings.LastBackupFile = Path.GetFileName(backupFile);
            _settings.LastError      = null;

            PruneOldBackups(backupDir);

            // Upload to cloud webhook if configured
            if (!string.IsNullOrWhiteSpace(_settings.CloudWebhookUrl))
                await UploadToWebhookAsync(backupFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] ? Backup failed.");
            _settings.LastError = ex.Message;
        }
    }

    private void PruneOldBackups(string backupDir)
    {
        var files = Directory.GetFiles(backupDir, "election_backup_*.db")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var old in files.Skip(_settings.RetentionCount))
        {
            File.Delete(old);
            _logger.LogInformation("[Backup] Pruned old backup: {File}", old);
        }
    }

    private static string ResolveDbPath()
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "/data/election.db";
        return raw.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
            ? raw["Data Source=".Length..].Trim()
            : raw.Trim();
    }

    private async Task UploadToWebhookAsync(string backupFile)
    {
        try
        {
            var fileBytes  = await File.ReadAllBytesAsync(backupFile);
            var b64        = Convert.ToBase64String(fileBytes);
            var fileName   = Path.GetFileName(backupFile);
            var payload    = new
            {
                fileName,
                fileSizeBytes = fileBytes.Length,
                timestamp     = DateTime.UtcNow.ToString("o"),
                contentBase64 = b64
            };
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            var response = await client.PostAsJsonAsync(_settings.CloudWebhookUrl, payload);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("[Backup] ?? Cloud upload succeeded ? {Url}", _settings.CloudWebhookUrl);
            else
                _logger.LogWarning("[Backup] ?? Cloud upload returned {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] ?? Cloud upload failed.");
        }
    }
}
