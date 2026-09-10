namespace TyFi.Auth.OtpMailer.Mail;

/// <summary>Renders the OTP email body. Extracted so <see cref="MailerooOtpEmailSender"/> stays free of helper methods.</summary>
public interface IOtpEmailBodyRenderer
{
    /// <summary>Renders the HTML body containing the given one-time code.</summary>
    string RenderHtml(string oneTimeCode);

    /// <summary>Renders the plain-text fallback body containing the given one-time code.</summary>
    string RenderPlainText(string oneTimeCode);
}
