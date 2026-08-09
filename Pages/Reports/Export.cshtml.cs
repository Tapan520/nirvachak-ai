using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Reports;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class ExportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly BackupSettings _backupSettings;

    public ExportModel(AppDbContext db, UserManager<AppUser> userManager, BackupSettings backupSettings)
    {
        _db             = db;
        _userManager    = userManager;
        _backupSettings = backupSettings;
    }

    public int ConstituencyId { get; set; }
    public bool HasConstituency { get; set; }
    public bool IsAdmin { get; set; }
    public List<Ward> Wards { get; set; } = new();
    public List<FileInfo> Backups { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        ConstituencyId = user?.ConstituencyId ?? 0;
        HasConstituency = ConstituencyId > 0;

        if (HasConstituency)
        {
            Wards = await _db.Wards
                .Where(w => w.ConstituencyId == ConstituencyId)
                .OrderBy(w => w.WardNumber)
                .ToListAsync();
        }

        // Show backup file list to SuperAdmin
        if (IsAdmin)
        {
            var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "/data/election.db";
            var backupDir = Path.Combine(Path.GetDirectoryName(dbPath) ?? "/data", "backups");
            if (Directory.Exists(backupDir))
            {
                Backups = Directory.GetFiles(backupDir, "election_backup_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();
            }
        }
    }
}
