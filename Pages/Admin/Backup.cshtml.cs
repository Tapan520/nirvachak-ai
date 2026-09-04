using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Admin;

[Authorize(Roles = "SuperAdmin")]
public class BackupModel : PageModel
{
    private readonly BackupSettings _settings;
    private readonly DatabaseBackupService _backupService;
    private readonly UserManager<AppUser> _userManager;

    public BackupModel(BackupSettings settings,
        DatabaseBackupService backupService,
        UserManager<AppUser> userManager)
    {
        _settings      = settings;
        _backupService = backupService;
        _userManager   = userManager;
    }

    // ?? View properties ??????????????????????????????????????????
    public bool         IsEnabled      { get; set; }
    public int          RetentionCount { get; set; }
    public string       ScheduleHour   { get; set; } = "02";
    public string?      CloudWebhookUrl { get; set; }
    public DateTime?    LastBackupAt   { get; set; }
    public string?      LastBackupFile { get; set; }
    public string?      LastError      { get; set; }
    public List<FileInfo> BackupFiles  { get; set; } = new();

    public void OnGet() => LoadViewModel();

    // ?? Toggle Enable / Disable ??????????????????????????????????
    public IActionResult OnPostToggle()
    {
        _settings.IsEnabled = !_settings.IsEnabled;
        TempData["Message"] = _settings.IsEnabled
            ? "? Automatic backups have been ENABLED."
            : "?? Automatic backups have been DISABLED.";
        return RedirectToPage();
    }

    // ?? Save Settings ????????????????????????????????????????????
    public IActionResult OnPostSaveSettings(string scheduleHour, int retentionCount, string? cloudWebhookUrl)
    {
        _settings.ScheduleHour    = scheduleHour;
        _settings.RetentionCount  = retentionCount;
        _settings.CloudWebhookUrl = string.IsNullOrWhiteSpace(cloudWebhookUrl) ? null : cloudWebhookUrl.Trim();
        var msg = $"\u2705 Settings saved — daily backup at {scheduleHour}:00 UTC, keeping last {retentionCount} files.";
        if (!string.IsNullOrWhiteSpace(_settings.CloudWebhookUrl)) msg += " \u2601\ufe0f Cloud webhook configured.";
        TempData["Message"] = msg;
        return RedirectToPage();
    }

    // ?? Run Backup Now ???????????????????????????????????????????
    public async Task<IActionResult> OnPostRunNowAsync()
    {
        await _backupService.RunNowAsync();
        if (_settings.LastError == null)
            TempData["Message"] = $"? Manual backup completed: {_settings.LastBackupFile}";
        else
            TempData["Error"] = $"? Backup failed: {_settings.LastError}";
        return RedirectToPage();
    }

    // ?? Delete a Backup File ?????????????????????????????????????
    public IActionResult OnPostDelete(string fileName)
    {
        try
        {
            var backupDir = _settings.BackupDirectory;
            var fullPath = Path.GetFullPath(Path.Combine(backupDir, fileName));
            var allowedRoot = Path.GetFullPath(backupDir);

            // Prevent path traversal
            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["Message"] = $"??? Deleted backup: {fileName}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete backup: {ex.Message}";
        }
        return RedirectToPage();
    }

    // ?? Helpers ??????????????????????????????????????????????????
    private void LoadViewModel()
    {
        IsEnabled       = _settings.IsEnabled;
        RetentionCount  = _settings.RetentionCount;
        ScheduleHour    = _settings.ScheduleHour;
        CloudWebhookUrl = _settings.CloudWebhookUrl;
        LastBackupAt    = _settings.LastBackupAt;
        LastBackupFile  = _settings.LastBackupFile;
        LastError       = _settings.LastError;

        var backupDir = _settings.BackupDirectory;
        if (Directory.Exists(backupDir))
        {
            BackupFiles = Directory.GetFiles(backupDir, "election_backup_*.sql")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();
        }
    }
}
