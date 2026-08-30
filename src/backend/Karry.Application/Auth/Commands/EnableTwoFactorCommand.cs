using Karry.Application.Auth;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record EnableTwoFactorRequest(string? DeviceId = null);

public sealed record EnableTwoFactorResponse(string Secret, string ProvisioningUri);

public sealed record EnableTwoFactorCommand(EnableTwoFactorRequest Input) : IRequest<EnableTwoFactorResponse>;

public sealed class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private readonly ITotpService _totp;
    private readonly IClock _clock;

    public EnableTwoFactorCommandHandler(ITotpService totp, IClock clock)
    {
        _totp = totp;
        _clock = clock;
    }

    public Task<EnableTwoFactorResponse> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var secret = _totp.GenerateSecret();
        var uri = _totp.BuildProvisioningUri("Karry", "karry", secret);
        return Task.FromResult(new EnableTwoFactorResponse(secret, uri));
    }
}