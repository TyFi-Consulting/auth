using Microsoft.Extensions.Options;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>
/// Issues access/refresh token pairs and persists the refresh token. Fresh issuance (login) and
/// rotation (refresh) are deliberately separate operations: rotation must atomically retire the old
/// token and persist its replacement in one step, which a generic "issue" method can't express.
/// </summary>
public interface ITokenIssuanceCoordinator
{
    /// <summary>Issues and persists a brand-new access/refresh token pair, starting a new token family.</summary>
    Task<AuthTokens> IssueAsync(AuthUserRecord user, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically rotates the refresh token identified by <paramref name="oldTokenHash"/> (part of
    /// <paramref name="familyId"/>) to a new access/refresh token pair in the same family. Returns
    /// <see langword="null"/> if another request already won the race to rotate this token first --
    /// the caller must treat that as reuse, not retry.
    /// </summary>
    Task<AuthTokens?> RotateAsync(AuthUserRecord user, string familyId, string oldTokenHash, CancellationToken cancellationToken);
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
    public async Task<AuthTokens> IssueAsync(AuthUserRecord user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(user, cancellationToken).ConfigureAwait(false);
        var refreshTokenIssuance = _refreshTokenGenerator.Generate(existingFamilyId: null);
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

    /// <inheritdoc />
    public async Task<AuthTokens?> RotateAsync(AuthUserRecord user, string familyId, string oldTokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(oldTokenHash);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(user, cancellationToken).ConfigureAwait(false);
        var refreshTokenIssuance = _refreshTokenGenerator.Generate(familyId);
        var now = _timeProvider.GetUtcNow();
        var refreshTokenExpiresUtc = now.Add(_options.Value.RefreshTokenLifetime);
        var newRecord = new RefreshTokenRecord
        {
            TokenHash = _secretHasher.Hash(refreshTokenIssuance.PlainTextToken),
            FamilyId = refreshTokenIssuance.FamilyId,
            UserId = user.Id,
            ExpiresUtc = refreshTokenExpiresUtc,
        };

        var rotated = await _refreshTokenStore.TryRotateAsync(oldTokenHash, newRecord, cancellationToken).ConfigureAwait(false);
        if (!rotated)
        {
            return null;
        }

        return new AuthTokens(accessToken.Token, accessToken.ExpiresUtc, refreshTokenIssuance.PlainTextToken, refreshTokenExpiresUtc);
    }
}

