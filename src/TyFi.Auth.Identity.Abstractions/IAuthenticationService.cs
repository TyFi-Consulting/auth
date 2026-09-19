namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Orchestrates passwordless (email one-time-code) registration, login, token refresh, and logout.
/// This is the single entry point a hosting Function App calls into; it never throws for expected
/// failure cases (wrong code, expired token, etc.) -- those are values on the returned result.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Creates a new account for the given email (or, if one already exists, does nothing extra) and
    /// sends a one-time code. Always returns the same result regardless of whether the account already
    /// existed, so this endpoint can never be used to enumerate registered accounts.
    /// </summary>
    Task<RegisterResult> RegisterAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a login code to the given email if an account exists. Always returns the same result
    /// regardless of whether the account exists, to avoid leaking which emails are registered.
    /// </summary>
    Task<RequestLoginCodeResult> RequestLoginCodeAsync(string email, CancellationToken cancellationToken);

    /// <summary>Verifies a pending one-time code and, on success, issues an access/refresh token pair.</summary>
    Task<VerifyCodeResult> VerifyCodeAsync(string email, string code, CancellationToken cancellationToken);

    /// <summary>Exchanges a valid, unused refresh token for a new access/refresh token pair (rotation).</summary>
    Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the token family associated with the given refresh token.</summary>
    Task<LogoutResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
