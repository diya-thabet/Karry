using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karry.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef migrations add` and `dotnet ef database update`.
/// Reads the connection string from the same configuration sources as the Api host.
/// </summary>
public sealed class KarryDbContextFactory : IDesignTimeDbContextFactory<KarryDbContext>
{
    public KarryDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("KarryDatabase")
            ?? throw new InvalidOperationException("Connection string 'KarryDatabase' is not configured for the design-time factory.");

        var optionsBuilder = new DbContextOptionsBuilder<KarryDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite());

        return new KarryDbContext(optionsBuilder.Options);
    }
}