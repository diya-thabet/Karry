using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Karry.Infrastructure.Persistence;

/// <summary>
/// Seeds idempotent baseline data: the global permission catalog and the platform super-admin
/// user. Runs after migrations; safe to call on every startup.
/// </summary>
public sealed class DbSeeder
{
    private readonly KarryDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _adminEmail;
    private readonly string _adminPassword;
    private readonly string _adminName;

    public DbSeeder(KarryDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;

        _adminEmail = configuration["Seed:AdminEmail"] ?? "root@kar.app";
        _adminPassword = configuration["Seed:AdminPassword"] ?? throw new InvalidOperationException("Seed:AdminPassword is required.");
        _adminName = configuration["Seed:AdminName"] ?? "Platform Admin";
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedPlatformAdminAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Permissions
            .Select(p => new { p.Resource, p.Action })
            .ToListAsync(cancellationToken);

        var existingKeys = existing
            .Select(e => (e.Resource, (PermissionAction)e.Action))
            .ToHashSet();

        var toAdd = PermissionCatalog.Flatten()
            .Select(f => (f.Resource, f.Action))
            .Distinct()
            .Where(k => !existingKeys.Contains(k))
            .Select(k => Permission.Create(k.Resource, k.Action))
            .ToList();

        if (toAdd.Count > 0)
        {
            _dbContext.Permissions.AddRange(toAdd);
        }
    }

    private async Task SeedPlatformAdminAsync(CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(_adminEmail);
        var exists = await _dbContext.Users.AnyAsync(u => u.Email.Value == email.Value, cancellationToken);

        if (exists)
        {
            return;
        }

        var user = User.Create(
            tenantId: null,
            email,
            _adminName,
            _passwordHasher.Hash(_adminPassword),
            isPlatformAdmin: true,
            roleId: null,
            deviceId: string.Empty,
            createdBy: Guid.NewGuid());

        _dbContext.Users.Add(user);

        _dbContext.AuditLogEntries.Add(AuditLogEntry.Create(
            Guid.Empty,
            user.Id,
            "platform.admin.seeded",
            "user",
            user.Id.ToString(),
            before: null,
            after: email.Value,
            AuditOutcome.Succeeded));
    }
}