using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Gokt.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core design-time tools (migrations, scaffolding).
/// Reads the connection string from the API project's appsettings or environment
/// without starting the full DI container.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Walk up from Infrastructure to the solution root, then into the API project
        var apiPath = Path.Combine(
            Directory.GetCurrentDirectory(),      // e.g., src/Gokt.Infrastructure at design time
            "..", "..", "Gokt");                  // → repo root / Gokt

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(apiPath))
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Fall back to a local dev connection if nothing is configured
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=gokt;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
