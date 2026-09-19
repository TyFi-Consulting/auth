namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Result of <see cref="IAuthenticationService.RegisterAsync"/>. Deliberately has only one possible
/// outcome: whether the email was brand new or already had an account, the caller always sees the
/// same "check your email for a code" response, so this endpoint can never be used to enumerate
/// registered accounts -- which would otherwise defeat the same protection built into
/// <see cref="IAuthenticationService.RequestLoginCodeAsync"/>. An existing account receives a login
/// code (not a fresh registration) transparently.
/// </summary>
public sealed record RegisterResult
{
    // Private and empty: blocks public construction so only the Started singleton below can ever
    // exist, guaranteeing every caller observes the exact same enumeration-safe instance.
    private RegisterResult()
    {
    }

    /// <summary>The single possible result instance.</summary>
    public static RegisterResult Started { get; } = new();
}
