using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record DisableTwoFactorCommand() : IRequest<Unit>;

public sealed class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Unit>
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentSession _session;

    public DisableTwoFactorCommandHandler(
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        ICurrentSession session)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _session = session;
    }

    public async Task<Unit> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var userId = _session.UserId ?? throw new ForbiddenException("Not authenticated.");

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.DisableTwoFactor(userId);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}