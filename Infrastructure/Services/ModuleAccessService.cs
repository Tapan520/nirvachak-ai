using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.ModuleAccess;

namespace Nirvachak_AI.Infrastructure.Services;

public class ModuleAccessService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public ModuleAccessService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<HashSet<string>> GetDisabledSubModulesAsync(int constituencyId)
    {
        var cacheKey = $"module-access-disabled:{constituencyId}";

        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached != null)
            return cached;

        var permissions = await _db.ConstituencyModulePermissions
            .AsNoTracking()
            .Where(p => p.ConstituencyId == constituencyId)
            .ToListAsync();

        var allKeys = ModuleAccessCatalog.AllSubModules.Select(s => s.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var disabled = permissions
            .Where(p => !p.IsEnabled && allKeys.Contains(p.SubModuleKey))
            .Select(p => p.SubModuleKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _cache.Set(cacheKey, disabled, TimeSpan.FromMinutes(2));
        return disabled;
    }

    public async Task<bool> IsEnabledAsync(int? constituencyId, string subModuleKey)
    {
        if (!constituencyId.HasValue || string.IsNullOrWhiteSpace(subModuleKey))
            return true;

        var disabled = await GetDisabledSubModulesAsync(constituencyId.Value);
        return !disabled.Contains(subModuleKey);
    }

    public void Invalidate(int constituencyId)
    {
        _cache.Remove($"module-access-disabled:{constituencyId}");
    }
}
