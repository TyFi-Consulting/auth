namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Storage seam for user accounts. Implemented by the consuming project (e.g. backed by EF Core) and
/// registered as the <c>TUserAccountStore</c> type parameter of <c>AddAuthentication</c>.
/// </summary>
public interface IUserAccountStore
{
    /// <summary>Finds a user by normalized (lowercased, trimmed) email address.</summary>
    Task<AuthUserRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Finds a user by id.</summary>
    Task<AuthUserRecord?> FindByIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Persists a newly created user and returns the stored record.</summary>
    Task<AuthUserRecord> CreateAsync(AuthUserRecord user, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to an existing user record, conditioned on <paramref name="user"/>'s
    /// <see cref="AuthUserRecord.ConcurrencyStamp"/> still matching what's currently stored (the value
    /// as read from a prior <c>FindBy*Async</c> call -- callers must never set it themselves). Returns
    /// <see langword="false"/> without persisting anything if another update already won the race
    /// first; the caller must treat that as a lost race, not retry blindly against stale data. On
    /// success, implementations should also assign a new value to <see cref="AuthUserRecord.ConcurrencyStamp"/>
    /// on <paramref name="user"/> so a caller performing further updates in the same request sees the
    /// fresh stamp.
    /// </summary>
    Task<bool> UpdateAsync(AuthUserRecord user, CancellationToken cancellationToken);
}
