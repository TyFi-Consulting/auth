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

    /// <summary>Persists changes to an existing user record.</summary>
    Task UpdateAsync(AuthUserRecord user, CancellationToken cancellationToken);
}
