using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Karry.IntegrationTests;

/// <summary>
/// Boots the full Karry API for integration testing. Configuration is injected from the
/// environment (set by the CI Postgres service container); a real PostgreSQL database is
/// required, with migrations and seed enabled.
/// </summary>
public sealed class KarryApiFactory : WebApplicationFactory<Program>
{
    private static readonly Lazy<bool> PostgresAvailable = new(CheckPostgres);

    /// <summary>True when a PostgreSQL connection string is configured (set in CI).</summary>
    public static bool IsPostgresConfigured => PostgresAvailable.Value;

    private static bool CheckPostgres()
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__KarryDatabase");
        return !string.IsNullOrWhiteSpace(cs);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddEnvironmentVariables();
        });

        builder.UseEnvironment("Production");
    }
}