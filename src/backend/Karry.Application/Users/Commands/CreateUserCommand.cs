using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Users.Commands;

public sealed record CreateUserCommand(CreateUserRequest Input) : IRequest<CreateUserResponse>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentSession _session;

    public CreateUserCommandHandler(
        IRepository<User> users,
        IRepository<Role> roles,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentSession session)
    {
        _users = users;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _session = session;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _session.TenantId
            ?? throw new ForbiddenException("Users must be created within a tenant.");
        var actor = _session.UserId ?? Guid.Empty;
        var input = request.Input;

        var role = await _roles.GetByIdAsync(input.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        if (role.TenantId != tenantId)
        {
            throw new ForbiddenException("Role does not belong to the current tenant.");
        }

        var email = EmailAddress.Create(input.Email);
        var duplicate = await _users.AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("A user with that email already exists.");
        }

        var passwordResult = PasswordPolicy.Validate(input.Password);
        if (!passwordResult.IsValid)
        {
            throw new ConflictException(string.Join(" ", passwordResult.Errors));
        }

        var user = User.Create(
            tenantId,
            email,
            input.Name,
            _passwordHasher.Hash(input.Password),
            isPlatformAdmin: false,
            roleId: role.Id,
            deviceId: string.Empty,
            createdBy: actor);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(user.Id, user.Email.Value);
    }
}