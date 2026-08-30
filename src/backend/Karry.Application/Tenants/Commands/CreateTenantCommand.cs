using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Tenants;
using Karry.Domain.Units;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Tenants.Commands;

public sealed record CreateTenantCommand(CreateTenantRequest Input) : IRequest<CreateTenantResponse>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CreateTenantResponse>
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<User> _users;
    private readonly IRepository<TenantUnitPreference> _tenantUnitPrefs;
    private readonly IRepository<AuditLogEntry> _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public CreateTenantCommandHandler(
        IRepository<Tenant> tenants,
        IRepository<Role> roles,
        IRepository<User> users,
        IRepository<TenantUnitPreference> tenantUnitPrefs,
        IRepository<AuditLogEntry> audit,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentSession session,
        IClock clock)
    {
        _tenants = tenants;
        _roles = roles;
        _users = users;
        _tenantUnitPrefs = tenantUnitPrefs;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _session = session;
        _clock = clock;
    }

    public async Task<CreateTenantResponse> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var actor = _session.UserId ?? Guid.Empty;

        var tenant = Tenant.Create(input.Name, input.Country, input.Currency, input.Timezone, input.Locale, actor);
        await _tenants.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tenantId = tenant.Id;

        await SeedSystemRolesAsync(tenantId, actor, cancellationToken);
        await _tenantUnitPrefs.AddAsync(TenantUnitPreference.Create(tenantId, MassUnit.MetricTon, VolumeUnit.CubicMeter), cancellationToken);

        if (!string.IsNullOrWhiteSpace(input.AdminEmail)
            && !string.IsNullOrWhiteSpace(input.AdminPassword)
            && !string.IsNullOrWhiteSpace(input.AdminName))
        {
            var adminRole = await _roles.FirstOrDefaultAsync(
                r => r.TenantId == tenantId && r.Code == SystemRoles.Admin, cancellationToken)
                ?? throw new ConflictException("Admin role could not be provisioned.");

            var email = EmailAddress.Create(input.AdminEmail);
            var duplicate = await _users.AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
            if (duplicate)
            {
                throw new ConflictException("A user with that email already exists.");
            }

            var passwordResult = PasswordPolicy.Validate(input.AdminPassword);
            if (!passwordResult.IsValid)
            {
                throw new ConflictException(string.Join(" ", passwordResult.Errors));
            }

            var admin = User.Create(
                tenantId,
                email,
                input.AdminName,
                _passwordHasher.Hash(input.AdminPassword),
                isPlatformAdmin: false,
                roleId: adminRole.Id,
                deviceId: string.Empty,
                createdBy: actor);
            await _users.AddAsync(admin, cancellationToken);
        }

        await _audit.AddAsync(
            AuditLogEntry.Create(
                tenantId,
                actor == Guid.Empty ? null : actor,
                "tenant.created",
                "tenant",
                tenantId.ToString(),
                before: null,
                after: input.Name,
                AuditOutcome.Succeeded,
                deviceId: null),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateTenantResponse(tenantId, tenant.Name);
    }

    private async Task SeedSystemRolesAsync(Guid tenantId, Guid createdBy, CancellationToken cancellationToken)
    {
        foreach (var roleCode in SystemRoles.All)
        {
            var grants = PermissionCatalog.ForRole(roleCode)
                .SelectMany(kv => kv.Value.Select(a => Permission.Create(kv.Key, a)))
                .ToList();

            var role = Role.Create(tenantId, roleCode, roleCode, null, grants, createdBy);
            await _roles.AddAsync(role, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}