namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Result of <see cref="IAuthenticationService.LogoutAsync"/>. Always succeeds -- logout is idempotent
/// and never reveals whether the supplied refresh token was valid.
/// </summary>
public sealed record LogoutResult
{
    // Private and empty: blocks public construction so only the Succeeded singleton below can
    // ever exist, guaranteeing every caller observes the exact same always-succeeds instance.
    private LogoutResult()
    {
    }

    /// <summary>The single possible result instance.</summary>
    public static LogoutResult Succeeded { get; } = new();
}
