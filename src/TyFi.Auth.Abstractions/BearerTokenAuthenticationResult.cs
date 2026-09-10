namespace TyFi.Auth.Abstractions;

/// <summary>The outcome of authenticating a single bearer token. A failure is a value, not an exception —
/// only genuine infrastructure faults (e.g. JWKS endpoint unreachable) should propagate as exceptions.</summary>
public sealed record BearerTokenAuthenticationResult(bool IsAuthenticated, AuthenticatedUser? User, string? FailureReason)
{
    /// <summary>Creates a successful result for the given user.</summary>
    public static BearerTokenAuthenticationResult Authenticated(AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new BearerTokenAuthenticationResult(true, user, null);
    }

    /// <summary>Creates a failed result with the given reason.</summary>
    public static BearerTokenAuthenticationResult Failed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new BearerTokenAuthenticationResult(false, null, reason);
    }
}
