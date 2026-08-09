using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public class ImportResult
{
    public int Total { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success { get; set; }
}

public class VoterCsvRow
{
    public string VoterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameLocal { get; set; }
    public string? FatherHusbandName { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public int BoothNumber { get; set; }
    public string? WardNumber { get; set; }
    public string? PannaNumber { get; set; }
    public int SerialNumber { get; set; }
}

public class VoterImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<VoterImportService> _logger;

    public VoterImportService(AppDbContext db, ILogger<VoterImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ImportResult> ImportFromCsvAsync(Stream fileStream, int constituencyId)
    {
        var result = new ImportResult();
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = null
            };
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, config);
            var rows = csv.GetRecords<VoterCsvRow>().ToList();
            result.Total = rows.Count;

            var existingIds = _db.Voters
                .IgnoreQueryFilters()
                .Where(v => v.ConstituencyId == constituencyId)
                .Select(v => v.VoterId)
                .ToHashSet();

            // Build a set of existing (normalised name + booth + serial) keys for fuzzy duplicate detection
            var existingKeys = _db.Voters
                .IgnoreQueryFilters()
                .Where(v => v.ConstituencyId == constituencyId)
                .Select(v => v.Name.ToLower() + "|" + v.BoothNumber + "|" + v.SerialNumber)
                .ToHashSet();

            // Detect duplicates within the import file itself
            var seenInFile    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenKeysInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var voters = new List<Voter>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.VoterId))
                {
                    result.Skipped++;
                    result.Errors.Add($"Row skipped — empty Voter ID (Name: {row.Name})");
                    continue;
                }
                var cleanId = row.VoterId.Trim();
                if (existingIds.Contains(cleanId))
                {
                    result.Skipped++;
                    result.Errors.Add($"Duplicate (already in DB): {cleanId} — {row.Name}");
                    continue;
                }
                if (!seenInFile.Add(cleanId))
                {
                    result.Skipped++;
                    result.Errors.Add($"Duplicate (within file): {cleanId} — {row.Name}");
                    continue;
                }
                // Fuzzy duplicate: same name + booth + serial already in DB or file
                var fuzzyKey = row.Name.Trim().ToLower() + "|" + row.BoothNumber + "|" + row.SerialNumber;
                if (existingKeys.Contains(fuzzyKey))
                {
                    result.Skipped++;
                    result.Errors.Add($"Probable duplicate (same name+booth+serial in DB): {cleanId} — {row.Name}");
                    continue;
                }
                if (!seenKeysInFile.Add(fuzzyKey))
                {
                    result.Skipped++;
                    result.Errors.Add($"Probable duplicate (same name+booth+serial within file): {cleanId} — {row.Name}");
                    continue;
                }
                voters.Add(new Voter
                {
                    VoterId = cleanId,
                    Name = row.Name.Trim(),
                    NameLocal = row.NameLocal,
                    FatherHusbandName = row.FatherHusbandName,
                    Age = row.Age,
                    Gender = row.Gender.Trim().ToUpper(),
                    MobileNumber = row.MobileNumber,
                    Address = row.Address,
                    BoothNumber = row.BoothNumber,
                    WardNumber = row.WardNumber,
                    PannaNumber = row.PannaNumber,
                    SerialNumber = row.SerialNumber,
                    ConstituencyId = constituencyId,
                    Sentiment = VoterSentiment.Unknown,
                    ImportedAt = DateTime.UtcNow
                });
                existingIds.Add(cleanId);
            }

            if (voters.Count > 0)
            {
                await _db.Voters.AddRangeAsync(voters);
                await _db.SaveChangesAsync();
                // Refresh booth voter counts
                var boothNums = voters.Select(v => v.BoothNumber).Distinct().ToList();
                var booths = await _db.Booths
                    .Where(b => b.ConstituencyId == constituencyId && boothNums.Contains(b.BoothNumber))
                    .ToListAsync();
                foreach (var booth in booths)
                {
                    booth.TotalVoters = await _db.Voters.CountAsync(v => v.BoothNumber == booth.BoothNumber && v.ConstituencyId == constituencyId);
                    booth.MaleVoters = await _db.Voters.CountAsync(v => v.BoothNumber == booth.BoothNumber && v.ConstituencyId == constituencyId && v.Gender == "M");
                    booth.FemaleVoters = await _db.Voters.CountAsync(v => v.BoothNumber == booth.BoothNumber && v.ConstituencyId == constituencyId && v.Gender == "F");
                }
                await _db.SaveChangesAsync();
            }
            result.Imported = voters.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing voters");
            result.Errors.Add(ex.Message);
            result.Success = false;
        }
        return result;
    }
}
