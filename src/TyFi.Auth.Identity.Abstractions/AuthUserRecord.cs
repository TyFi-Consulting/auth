namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// The data <see cref="IUserAccountStore"/> persists for one user. The consuming project maps this
/// to/from its own storage (EF Core entity, table row, etc.) -- this type is a data contract, not an
/// ORM entity.
/// </summary>
public sealed class AuthUserRecord
{
    /// <summary>Stable, unique user identifier (the project decides the format, e.g. a GUID string).</summary>
    public required string Id { get; set; }

    /// <summary>Normalized (lowercased, trimmed) email address. Also the sign-in identifier.</summary>
    public required string Email { get; set; }

    /// <summary>Role names currently assigned to this user, drawn from the catalog configured via <c>AddRoles</c>.</summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>Hash of the current pending one-time login/registration code, or null if none is outstanding.</summary>
    public string? PendingCodeHash { get; set; }

    /// <summary>Expiry of <see cref="PendingCodeHash"/>, or null if none is outstanding.</summary>
    public DateTimeOffset? PendingCodeExpiresUtc { get; set; }

    /// <summary>Number of failed verification attempts against the current <see cref="PendingCodeHash"/>.</summary>
    public int PendingCodeAttempts { get; set; }

    /// <summary>Consecutive failed login attempts, reset to zero on a successful login.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>If set and in the future, the account is locked out and code verification is refused.</summary>
    public DateTimeOffset? LockoutEndUtc { get; set; }

    /// <summary>When the account was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>When the account last completed a successful login, or null if never.</summary>
    public DateTimeOffset? LastLoginUtc { get; set; }

    /// <summary>When the current <see cref="PendingCodeHash"/> (if any) was sent, used to throttle resends.</summary>
    public DateTimeOffset? LastCodeSentUtc { get; set; }

    /// <summary>
    /// Opaque optimistic-concurrency token, owned entirely by the store implementation -- callers
    /// never set it. Lets <see cref="IUserAccountStore.UpdateAsync"/> detect and reject a lost race
    /// between two concurrent reads of the same account (see its documentation for the contract).
    /// </summary>
    public string? ConcurrencyStamp { get; set; }
}
