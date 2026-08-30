using Karry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karry.Api;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup when <c>Database:AutoMigrate</c> is true,
    /// and seeds baseline data when <c>Seed:Enabled</c> is true. In production, prefer running
    /// migrations explicitly in the deployment pipeline instead of on every app start.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KarryDbContext>();

        await dbContext.Database.MigrateAsync();

        if (configuration.GetValue<bool>("Seed:Enabled"))
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }
    }
}