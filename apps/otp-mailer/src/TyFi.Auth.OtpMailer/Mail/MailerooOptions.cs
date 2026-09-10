namespace TyFi.Auth.OtpMailer.Mail;

public sealed class MailerooOptions
{
    public const string SectionName = "Maileroo";

    /// <summary>Keyed by tenant ID (the same tenant ID produced by <see cref="Auth.TokenValidationOutcome"/>).</summary>
    public Dictionary<string, MailerooSenderOption> Tenants { get; init; } = [];
}
