using Karry.Domain.Common;
using Karry.Domain.Equipment;
using Karry.Domain.Maintenance;
using Karry.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Karry.Infrastructure.Persistence;

public class KarryDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentTenant? _currentTenant;

    public KarryDbContext(DbContextOptions<KarryDbContext> options, ICurrentTenant? currentTenant = null)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<WearPart> WearParts => Set<WearPart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KarryDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantFiltering();
        ApplyAuditTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantFiltering()
    {
        var tenantId = _currentTenant?.TenantId;

        if (tenantId is null)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.SetTenantId(tenantId.Value);
        }
    }

    private void ApplyAuditTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.Entity.MarkUpdated();
                    break;
            }
        }
    }
}