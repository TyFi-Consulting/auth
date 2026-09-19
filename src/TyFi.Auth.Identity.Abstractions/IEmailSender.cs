namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Email delivery seam. Implemented by the consuming project (e.g. backed by Maileroo) and registered
/// as the <c>TEmailSender</c> type parameter of <c>AddAuthentication</c>. Owns branding, copy, and the
/// actual send call -- this library only ever hands it a plain code and expiry.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends a login/registration one-time code to the given address.</summary>
    Task SendLoginCodeAsync(string emailAddress, string code, TimeSpan validFor, CancellationToken cancellationToken);
}
