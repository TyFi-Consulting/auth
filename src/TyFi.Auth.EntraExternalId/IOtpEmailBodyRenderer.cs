namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Renders the OTP email body. Register your own implementation to replace the default generic
/// template with your project's own copy/branding — the email template stays project-owned, not
/// baked into this shared package.
/// </summary>
public interface IOtpEmailBodyRenderer
{
    /// <summary>Renders the HTML body containing the given one-time code.</summary>
    string RenderHtml(string oneTimeCode);

    /// <summary>Renders the plain-text fallback body containing the given one-time code.</summary>
    string RenderPlainText(string oneTimeCode);
}
