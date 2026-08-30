using Karry.Application.Auth;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Common;
using Karry.Domain.Identity;
using Karry.Infrastructure.Persistence;

namespace Karry.Infrastructure.Auth;

public sealed class TokenIssuer : ITokenIssuer
{
    private readonly IRepository<RefreshToken> _tokens;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ISecureRandom _secureRandom;
    private readonly IUnitOfWork _unitOfWork;

    public TokenIssuer(
        IRepository<RefreshToken> tokens,
        IAccessTokenService accessTokenService,
        ISecureRandom secureRandom,
        IUnitOfWork unitOfWork)
    {
        _tokens = tokens;
        _accessTokenService = accessTokenService;
        _secureRandom = secureRandom;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokensResponse> IssueAsync(
        Guid userId,
        Guid? tenantId,
        string name,
        string? roleCode,
        IEnumerable<string> permissions,
        string deviceId,
        TimeSpan refreshLifetime,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken,
        Guid? familyId = null,
        Guid? parentTokenId = null)
    {
        var accessToken = _accessTokenService.CreateAccessToken(userId, tenantId, name, roleCode ?? string.Empty, permissions, deviceId);

        var rawRefresh = _secureRandom.RandomHex(RefreshToken.TokenByteLength);
        var refreshToken = RefreshToken.Create(
            userId,
            RefreshTokenHasher.Hash(rawRefresh),
            familyId ?? Guid.Empty,
            deviceId,
            DateTime.UtcNow.Add(refreshLifetime),
            ipAddress,
            userAgent);

        await _tokens.AddAsync(refreshToken, cancellationToken);

        if (parentTokenId is not null)
        {
            var parent = await _tokens.GetByIdAsync(parentTokenId.Value, cancellationToken);
            parent?.Revoke(refreshToken.Id, DateTime.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(accessToken, rawRefresh, refreshToken.Id);
    }
}