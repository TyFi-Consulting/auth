using Maileroo.DotNet.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TyFi.Auth.EntraExternalId;

/// <inheritdoc cref="IOtpEmailSender" />
public sealed class MailerooOtpEmailSender : IOtpEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<MailerooOptions> _options;
    private readonly IOtpEmailBodyRenderer _bodyRenderer;
    private readonly ILogger<MailerooOtpEmailSender> _logger;

    /// <summary>Creates the sender using the given HTTP client, options, body renderer, and logger.</summary>
    public MailerooOtpEmailSender(
        HttpClient httpClient,
        IOptions<MailerooOptions> options,
        IOtpEmailBodyRenderer bodyRenderer,
        ILogger<MailerooOtpEmailSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bodyRenderer = bodyRenderer ?? throw new ArgumentNullException(nameof(bodyRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendOtpEmailAsync(string recipientEmail, string oneTimeCode, CancellationToken cancellationToken)
    {
        var sender = _options.Value;
        if (!sender.IsConfigured)
        {
            throw new InvalidOperationException("Maileroo sender is not configured.");
        }

        // A single fixed sending key for the app's lifetime, so reusing one HttpClient across
        // calls is safe here (unlike a multi-tenant deployment sharing one key-mutating client).
        var client = new MailerooClient(sender.ApiKey!, httpClient: _httpClient);
        var payload = new Dictionary<string, object?>
        {
            ["from"] = new EmailAddress(sender.FromAddress!, sender.FromDisplayName),
            ["to"] = new List<EmailAddress> { new(recipientEmail) },
            ["subject"] = sender.Subject,
            ["html"] = _bodyRenderer.RenderHtml(oneTimeCode),
            ["plain"] = _bodyRenderer.RenderPlainText(oneTimeCode),
        };

        try
        {
            await client.SendBasicEmailAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _logger.LogError(ex, "Maileroo send failed.");
            throw new InvalidOperationException("Maileroo send failed.", ex);
        }
    }
}
