namespace TyFi.Auth.EntraExternalId;

/// <summary>Maileroo sending configuration for this project's single tenant.</summary>
public sealed class MailerooOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Maileroo";

    /// <summary>The Maileroo sending key (API key).</summary>
    public string? ApiKey { get; set; }

    /// <summary>The verified from-address for this project's Maileroo domain.</summary>
    public string? FromAddress { get; set; }

    /// <summary>The display name shown alongside <see cref="FromAddress"/>.</summary>
    public string? FromDisplayName { get; set; }

    /// <summary>The email subject line.</summary>
    public string? Subject { get; set; }

    /// <summary>True once <see cref="ApiKey"/>, <see cref="FromAddress"/>, and <see cref="Subject"/> are set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(FromAddress) && !string.IsNullOrWhiteSpace(Subject);
}
