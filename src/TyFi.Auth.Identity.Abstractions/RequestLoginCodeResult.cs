namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Result of <see cref="IAuthenticationService.RequestLoginCodeAsync"/>. Deliberately has only one
/// possible outcome: whether or not the email address is registered, the caller always sees the same
/// "a code was sent if this account exists" response, so the endpoint can never be used to enumerate
/// registered accounts.
/// </summary>
public sealed record RequestLoginCodeResult
{
    // Private and empty: blocks public construction so only the Sent singleton below can ever
    // exist, guaranteeing every caller observes the exact same enumeration-safe instance.
    private RequestLoginCodeResult()
    {
    }

    /// <summary>The single possible result instance.</summary>
    public static RequestLoginCodeResult Sent { get; } = new();
}
