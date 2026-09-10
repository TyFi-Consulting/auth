namespace TyFi.Auth.OtpMailer.Mail;

public sealed class OtpEmailBodyRenderer : IOtpEmailBodyRenderer
{
    /// <inheritdoc />
    public string RenderHtml(string oneTimeCode) => $"""
        <html>
          <body style="font-family: sans-serif;">
            <p>Your verification code is:</p>
            <p style="font-size: 28px; font-weight: bold; letter-spacing: 4px;">{oneTimeCode}</p>
            <p>If you didn't request this code, you can safely ignore this email.</p>
          </body>
        </html>
        """;

    /// <inheritdoc />
    public string RenderPlainText(string oneTimeCode) =>
        $"Your verification code is: {oneTimeCode}. If you didn't request this code, you can safely ignore this email.";
}
