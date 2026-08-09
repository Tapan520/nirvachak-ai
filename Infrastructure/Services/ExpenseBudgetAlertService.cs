using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Background service that checks EC expense budget every hour.
/// Logs a warning when spending crosses 80% and 90% of the ?40 lakh limit.
/// </summary>
public class ExpenseBudgetAlertService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpenseBudgetAlertService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private const decimal EcBudgetLimit = 4_000_000m;

    public ExpenseBudgetAlertService(IServiceScopeFactory scopeFactory,
        ILogger<ExpenseBudgetAlertService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckBudgetsAsync();
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckBudgetsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var constituencies = await db.Constituencies.ToListAsync();
            foreach (var c in constituencies)
            {
                var total = (decimal)(await db.Expenses
                    .Where(e => e.ConstituencyId == c.Id)
                    .SumAsync(e => (double?)e.Amount) ?? 0);

                var pct = EcBudgetLimit > 0
                    ? (int)Math.Round(total / EcBudgetLimit * 100)
                    : 0;

                if (pct >= 90)
                    _logger.LogWarning(
                        "[EC Budget] ?? CRITICAL: {Name} has spent ?{Total:N0} ({Pct}% of EC limit ?{Limit:N0}).",
                        c.Name, total, pct, EcBudgetLimit);
                else if (pct >= 80)
                    _logger.LogWarning(
                        "[EC Budget] ?? WARNING: {Name} has spent ?{Total:N0} ({Pct}% of EC limit ?{Limit:N0}).",
                        c.Name, total, pct, EcBudgetLimit);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EC Budget] Budget check failed.");
        }
    }
}
