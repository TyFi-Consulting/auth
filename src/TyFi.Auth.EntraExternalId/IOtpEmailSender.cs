namespace TyFi.Auth.EntraExternalId;

/// <summary>Sends the one-time-passcode email through the configured Maileroo sender.</summary>
public interface IOtpEmailSender
{
    /// <summary>Sends the OTP email.</summary>
    /// <param name="recipientEmail">The address the one-time code is sent to.</param>
    /// <param name="oneTimeCode">The one-time passcode issued by Entra.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    Task SendOtpEmailAsync(string recipientEmail, string oneTimeCode, CancellationToken cancellationToken);
}
