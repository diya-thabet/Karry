using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record LogoutCommand(LogoutRequest Input) : IRequest<Unit>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRepository<RefreshToken> _tokens;
    private readonly IRepository<AuditLogEntry> _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutCommandHandler(
        IRepository<RefreshToken> tokens,
        IRepository<AuditLogEntry> audit,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tokens = tokens;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = RefreshTokenHasher.Hash(request.Input.RefreshToken);
        var token = await _tokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is not null)
        {
            token.RevokeFamilyEntry(_clock.UtcNow);
            _tokens.Update(token);

            await _audit.AddAsync(
                AuditLogEntry.Create(
                    Guid.Empty,
                    token.UserId,
                    "logout",
                    "refresh_token",
                    token.Id.ToString(),
                    before: null,
                    after: null,
                    AuditOutcome.Succeeded),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}