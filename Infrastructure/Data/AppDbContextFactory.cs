using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nirvachak_AI.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF tools (migrations) without requiring a live MySQL connection
/// for server-version auto-detect.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Port=3306;Database=nirvachak_ai;User=root;Password=;CharSet=utf8mb4;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.36-mysql"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
