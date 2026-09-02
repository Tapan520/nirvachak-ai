using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.ModuleAccess;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Admin.Constituencies;

[Authorize(Roles = "SuperAdmin")]
public class ModulesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ModuleAccessService _moduleAccessService;

    public ModulesModel(AppDbContext db, ModuleAccessService moduleAccessService)
    {
        _db = db;
        _moduleAccessService = moduleAccessService;
    }

    public Constituency? Constituency { get; set; }
    public IReadOnlyList<ModuleGroup> Catalog => ModuleAccessCatalog.Modules;
    public List<Constituency> SourceConstituencies { get; set; } = new();

    [BindProperty]
    public Dictionary<string, bool> ModuleState { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [BindProperty]
    public int? SourceConstituencyId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Constituency = await _db.Constituencies.FirstOrDefaultAsync(c => c.Id == id);
        if (Constituency == null) return NotFound();
        await LoadSourceConstituenciesAsync(id);

        var existing = await _db.ConstituencyModulePermissions
            .Where(p => p.ConstituencyId == id)
            .ToListAsync();

        var map = existing.ToDictionary(p => p.SubModuleKey, p => p.IsEnabled, StringComparer.OrdinalIgnoreCase);

        ModuleState = ModuleAccessCatalog.AllSubModules
            .ToDictionary(s => s.Key, s => map.GetValueOrDefault(s.Key, true), StringComparer.OrdinalIgnoreCase);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        Constituency = await _db.Constituencies.FirstOrDefaultAsync(c => c.Id == id);
        if (Constituency == null) return NotFound();

        var existing = await _db.ConstituencyModulePermissions
            .Where(p => p.ConstituencyId == id)
            .ToListAsync();

        var existingMap = existing.ToDictionary(p => p.SubModuleKey, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var module in ModuleAccessCatalog.Modules)
        {
            foreach (var sub in module.SubModules)
            {
                var isEnabled = ModuleState.GetValueOrDefault(sub.Key, true);

                if (existingMap.TryGetValue(sub.Key, out var row))
                {
                    row.IsEnabled = isEnabled;
                    row.ModuleKey = module.Key;
                    row.SubModuleKey = sub.Key;
                    row.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.ConstituencyModulePermissions.Add(new ConstituencyModulePermission
                    {
                        ConstituencyId = id,
                        ModuleKey = module.Key,
                        SubModuleKey = sub.Key,
                        IsEnabled = isEnabled,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        _moduleAccessService.Invalidate(id);

        TempData["Message"] = $"Module access updated for '{Constituency.Name}'.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCopyAsync(int id)
    {
        Constituency = await _db.Constituencies.FirstOrDefaultAsync(c => c.Id == id);
        if (Constituency == null) return NotFound();

        if (!SourceConstituencyId.HasValue || SourceConstituencyId.Value == id)
        {
            TempData["Error"] = "Please select a different constituency to copy from.";
            return RedirectToPage(new { id });
        }

        var source = await _db.Constituencies.FirstOrDefaultAsync(c => c.Id == SourceConstituencyId.Value);
        if (source == null)
        {
            TempData["Error"] = "Source constituency not found.";
            return RedirectToPage(new { id });
        }

        var sourceRows = await _db.ConstituencyModulePermissions
            .Where(p => p.ConstituencyId == SourceConstituencyId.Value)
            .ToListAsync();

        var sourceMap = ModuleAccessCatalog.AllSubModules
            .ToDictionary(s => s.Key, _ => true, StringComparer.OrdinalIgnoreCase);

        foreach (var row in sourceRows)
            sourceMap[row.SubModuleKey] = row.IsEnabled;

        var targetRows = await _db.ConstituencyModulePermissions
            .Where(p => p.ConstituencyId == id)
            .ToListAsync();

        var targetMap = targetRows.ToDictionary(p => p.SubModuleKey, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var module in ModuleAccessCatalog.Modules)
        {
            foreach (var sub in module.SubModules)
            {
                var isEnabled = sourceMap.GetValueOrDefault(sub.Key, true);
                if (targetMap.TryGetValue(sub.Key, out var row))
                {
                    row.ModuleKey = module.Key;
                    row.SubModuleKey = sub.Key;
                    row.IsEnabled = isEnabled;
                    row.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.ConstituencyModulePermissions.Add(new ConstituencyModulePermission
                    {
                        ConstituencyId = id,
                        ModuleKey = module.Key,
                        SubModuleKey = sub.Key,
                        IsEnabled = isEnabled,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        _moduleAccessService.Invalidate(id);

        TempData["Message"] = $"Copied module settings from '{source.Name}' to '{Constituency.Name}'.";
        return RedirectToPage(new { id });
    }

    private async Task LoadSourceConstituenciesAsync(int currentConstituencyId)
    {
        SourceConstituencies = await _db.Constituencies
            .Where(c => c.Id != currentConstituencyId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
