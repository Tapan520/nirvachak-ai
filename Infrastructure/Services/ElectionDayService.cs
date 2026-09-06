using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public class BoothTurnoutDto
{
    public int BoothNumber { get; set; }
    public string BoothName { get; set; } = string.Empty;
    public int TotalVoters { get; set; }
    public int VotedCount { get; set; }
    public double TurnoutPercent => TotalVoters > 0 ? Math.Round((double)VotedCount / TotalVoters * 100, 1) : 0;
}

public class ElectionDayService
{
    private readonly AppDbContext _db;
    public ElectionDayService(AppDbContext db) => _db = db;

    public async Task<bool> MarkVotedAsync(int voterId)
    {
        var voter = await _db.Voters.FindAsync(voterId);
        if (voter == null) return false;
        voter.ElectionDayStatus = ElectionDayStatus.Voted;
        voter.LastContactedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var booth = await _db.Booths.FirstOrDefaultAsync(b =>
            b.BoothNumber == voter.BoothNumber && b.ConstituencyId == voter.ConstituencyId);
        if (booth != null)
        {
            booth.VotedCount = await _db.Voters.CountAsync(v =>
                v.BoothNumber == voter.BoothNumber &&
                v.ConstituencyId == voter.ConstituencyId &&
                v.ElectionDayStatus == ElectionDayStatus.Voted);
            booth.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> MarkAbsentAsync(int voterId)
    {
        var voter = await _db.Voters.FindAsync(voterId);
        if (voter == null) return false;
        voter.ElectionDayStatus = ElectionDayStatus.Absent;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<BoothTurnoutDto>> GetLiveTurnoutAsync(int constituencyId)
    {
        // Compute counts live from Voters so booths whose stored Booth.TotalVoters
        // / VotedCount columns are stale (e.g. booths added after voter import)
        // still show correct numbers.
        var booths = await _db.Booths
            .Where(b => b.ConstituencyId == constituencyId)
            .OrderBy(b => b.BoothNumber)
            .Select(b => new { b.BoothNumber, b.BoothName })
            .ToListAsync();

        var stats = await _db.Voters
            .Where(v => v.ConstituencyId == constituencyId && !v.IsDeleted)
            .GroupBy(v => v.BoothNumber)
            .Select(g => new
            {
                BoothNumber = g.Key,
                Total = g.Count(),
                Voted = g.Count(v => v.ElectionDayStatus == ElectionDayStatus.Voted)
            })
            .ToDictionaryAsync(x => x.BoothNumber);

        return booths.Select(b =>
        {
            stats.TryGetValue(b.BoothNumber, out var s);
            return new BoothTurnoutDto
            {
                BoothNumber = b.BoothNumber,
                BoothName   = b.BoothName,
                TotalVoters = s?.Total ?? 0,
                VotedCount  = s?.Voted ?? 0
            };
        }).ToList();
    }

    public async Task<(int total, int voted, double percent)> GetConstituencyTurnoutAsync(int constituencyId)
    {
        var total = await _db.Voters.CountAsync(v => v.ConstituencyId == constituencyId && !v.IsDeleted);
        var voted = await _db.Voters.CountAsync(v => v.ConstituencyId == constituencyId && !v.IsDeleted && v.ElectionDayStatus == ElectionDayStatus.Voted);
        var percent = total > 0 ? Math.Round((double)voted / total * 100, 1) : 0;
        return (total, voted, percent);
    }
}
