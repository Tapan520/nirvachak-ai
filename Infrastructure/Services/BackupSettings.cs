namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Singleton that holds the backup configuration controlled by SuperAdmin.
/// State is persisted to /data/backup_settings.json so it survives restarts.
/// </summary>
public class BackupSettings
{
    private readonly ILogger<BackupSettings> _logger;
    private readonly string _settingsFile;

    private bool _isEnabled = true;
    private int  _retentionDays = 7;
    private string _scheduleHour = "02"; // 2 AM UTC
    private string? _cloudWebhookUrl = null;

    public BackupSettings(ILogger<BackupSettings> logger)
    {
        _logger = logger;
        var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "/data/election.db";
        var dir = Path.GetDirectoryName(dbPath) ?? "/data";
        _settingsFile = Path.Combine(dir, "backup_settings.json");
        Load();
    }

    /// <summary>Whether automatic backups are enabled (controlled by SuperAdmin).</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; Save(); }
    }

    /// <summary>Number of backup files to retain (1–30).</summary>
    public int RetentionCount
    {
        get => _retentionDays;
        set { _retentionDays = Math.Clamp(value, 1, 30); Save(); }
    }

    /// <summary>UTC hour (0–23) at which the daily backup runs.</summary>
    public string ScheduleHour
    {
        get => _scheduleHour;
        set { _scheduleHour = value; Save(); }
    }

    /// <summary>Optional webhook URL to POST the backup file to (e.g. Make/Zapier/n8n).</summary>
    public string? CloudWebhookUrl
    {
        get => _cloudWebhookUrl;
        set { _cloudWebhookUrl = value; Save(); }
    }

    /// <summary>Timestamp of the last successful backup.</summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>Path of the last successful backup file.</summary>
    public string? LastBackupFile { get; set; }

    /// <summary>Last error message if backup failed, null if last backup succeeded.</summary>
    public string? LastError { get; set; }

    // ?? Persistence ??????????????????????????????????????????????

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsFile)) return;
            var json = File.ReadAllText(_settingsFile);
            var data = System.Text.Json.JsonSerializer.Deserialize<BackupSettingsData>(json);
        if (data == null) return;
            _isEnabled       = data.IsEnabled;
            _retentionDays   = data.RetentionCount;
            _scheduleHour    = data.ScheduleHour ?? "02";
            _cloudWebhookUrl = data.CloudWebhookUrl;
            LastBackupAt     = data.LastBackupAt;
            LastBackupFile   = data.LastBackupFile;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BackupSettings] Could not load settings — using defaults.");
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFile)!;
            Directory.CreateDirectory(dir);
            var data = new BackupSettingsData
            {
                IsEnabled      = _isEnabled,
                RetentionCount = _retentionDays,
                ScheduleHour   = _scheduleHour,
                LastBackupAt   = LastBackupAt,
                LastBackupFile = LastBackupFile,
                CloudWebhookUrl = _cloudWebhookUrl
            };
            var json = System.Text.Json.JsonSerializer.Serialize(data,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BackupSettings] Could not save settings.");
        }
    }

    private class BackupSettingsData
    {
        public bool     IsEnabled       { get; set; } = true;
        public int      RetentionCount  { get; set; } = 7;
        public string?  ScheduleHour    { get; set; } = "02";
        public DateTime? LastBackupAt   { get; set; }
        public string?  LastBackupFile  { get; set; }
        public string?  CloudWebhookUrl { get; set; }
    }
}
