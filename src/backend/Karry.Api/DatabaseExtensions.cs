using Karry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karry.Api;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup when <c>Database:AutoMigrate</c> is true,
    /// and runs the optional seed callback. In production, prefer running migrations explicitly
    /// in the deployment pipeline instead of on every app start.
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
    }
}