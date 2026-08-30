using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record VerifyTwoFactorRequest(string Secret, string Code);

public sealed record VerifyTwoFactorCommand(VerifyTwoFactorRequest Input) : IRequest<Unit>;

public sealed class VerifyTwoFactorCommandHandler : IRequestHandler<VerifyTwoFactorCommand, Unit>
{
    private static readonly TimeSpan TotpClockSkew = TimeSpan.FromSeconds(30);

    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentSession _session;
    private readonly ITotpService _totp;
    private readonly IClock _clock;

    public VerifyTwoFactorCommandHandler(
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        ICurrentSession session,
        ITotpService totp,
        IClock clock)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _session = session;
        _totp = totp;
        _clock = clock;
    }

    public async Task<Unit> Handle(VerifyTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var userId = _session.UserId ?? throw new ForbiddenException("Not authenticated.");

        if (!_totp.Validate(input.Secret, input.Code, TotpClockSkew))
        {
            throw new ConflictException("The verification code is invalid.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.EnableTwoFactor(input.Secret, userId);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}