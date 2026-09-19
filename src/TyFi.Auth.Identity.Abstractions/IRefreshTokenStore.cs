namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Storage seam for refresh tokens. Implemented by the consuming project and registered as the
/// <c>TRefreshTokenStore</c> type parameter of <c>AddAuthentication</c>. Required (not optional/in-memory
/// by default) because refresh tokens must survive process restarts and scale-out to multiple instances.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>Persists a newly issued refresh token.</summary>
    Task StoreAsync(RefreshTokenRecord token, CancellationToken cancellationToken);

    /// <summary>Finds a refresh token by its keyed hash.</summary>
    Task<RefreshTokenRecord?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Marks every token in the given family as revoked (logout, or reuse-of-rotated-token detection).</summary>
    Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically rotates a refresh token: if <paramref name="oldTokenHash"/> is still active (not
    /// expired, rotated, or revoked), marks it rotated and persists <paramref name="newToken"/> as its
    /// replacement in the same operation. Returns <see langword="false"/>, persisting nothing, if
    /// another request has already rotated, revoked, or outlasted <paramref name="oldTokenHash"/>
    /// first -- callers must treat that as reuse (a token-theft signal), never retry blindly.
    /// </summary>
    Task<bool> TryRotateAsync(string oldTokenHash, RefreshTokenRecord newToken, CancellationToken cancellationToken);
}
