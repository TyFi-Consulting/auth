namespace TyFi.Auth.OtpMailer.Mail;

/// <summary>Sends the one-time-passcode email through the configured provider (Maileroo) for a given tenant.</summary>
public interface IOtpEmailSender
{
    /// <summary>Sends the OTP email for the given tenant's configured sender.</summary>
    /// <param name="tenantId">The tenant whose Maileroo sender configuration and API key should be used.</param>
    /// <param name="recipientEmail">The address the one-time code is sent to.</param>
    /// <param name="oneTimeCode">The one-time passcode issued by Entra.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    Task SendOtpEmailAsync(string tenantId, string recipientEmail, string oneTimeCode, CancellationToken cancellationToken);
}
