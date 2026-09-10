namespace TyFi.Auth.OtpMailer.Mail;

/// <summary>Per-tenant Maileroo sending configuration.</summary>
public sealed class MailerooSenderOption
{
    public required string ApiKey { get; init; }

    public required string FromAddress { get; init; }

    public string? FromDisplayName { get; init; }

    public required string Subject { get; init; }
}
