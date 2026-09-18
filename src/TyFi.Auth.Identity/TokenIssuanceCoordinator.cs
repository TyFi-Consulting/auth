using Microsoft.Extensions.Options;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>
/// Issues a fresh access/refresh token pair for a user and persists the refresh token. Shared by both
/// the initial login (no existing token family) and refresh rotation (carries the prior family id).
/// </summary>
public interface ITokenIssuanceCoordinator
{
    /// <summary>Issues and persists a new access/refresh token pair for the given user.</summary>
    Task<AuthTokens> IssueAsync(AuthUserRecord user, string? existingFamilyId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITokenIssuanceCoordinator" />
public sealed class TokenIssuanceCoordinator : ITokenIssuanceCoordinator
{
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ISecretHasher _secretHasher;
    private readonly IOptions<AuthIdentityOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the coordinator.</summary>
    public TokenIssuanceCoordinator(
        ITokenIssuer tokenIssuer,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenStore refreshTokenStore,
        ISecretHasher secretHasher,
        IOptions<AuthIdentityOptions> options,
        TimeProvider timeProvider)
    {
        _tokenIssuer = tokenIssuer ?? throw new ArgumentNullException(nameof(tokenIssuer));
        _refreshTokenGenerator = refreshTokenGenerator ?? throw new ArgumentNullException(nameof(refreshTokenGenerator));
        _refreshTokenStore = refreshTokenStore ?? throw new ArgumentNullException(nameof(refreshTokenStore));
        _secretHasher = secretHasher ?? throw new ArgumentNullException(nameof(secretHasher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<AuthTokens> IssueAsync(AuthUserRecord user, string? existingFamilyId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(user, cancellationToken).ConfigureAwait(false);
        var refreshTokenIssuance = _refreshTokenGenerator.Generate(existingFamilyId);
        var now = _timeProvider.GetUtcNow();
        var refreshTokenExpiresUtc = now.Add(_options.Value.RefreshTokenLifetime);

        await _refreshTokenStore.StoreAsync(
            new RefreshTokenRecord
            {
                TokenHash = _secretHasher.Hash(refreshTokenIssuance.PlainTextToken),
                FamilyId = refreshTokenIssuance.FamilyId,
                UserId = user.Id,
                ExpiresUtc = refreshTokenExpiresUtc,
            },
            cancellationToken).ConfigureAwait(false);

        return new AuthTokens(accessToken.Token, accessToken.ExpiresUtc, refreshTokenIssuance.PlainTextToken, refreshTokenExpiresUtc);
    }
}
