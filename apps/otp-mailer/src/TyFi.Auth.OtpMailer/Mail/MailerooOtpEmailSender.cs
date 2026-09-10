using Maileroo.DotNet.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TyFi.Auth.OtpMailer.Mail;

public sealed class MailerooOtpEmailSender : IOtpEmailSender
{
    /// <summary>Name of the pooled <see cref="HttpClient"/> registered for this sender via <see cref="IHttpClientFactory"/>.</summary>
    public const string HttpClientName = "MailerooOtpEmailSender";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<MailerooOptions> _options;
    private readonly IOtpEmailBodyRenderer _bodyRenderer;
    private readonly ILogger<MailerooOtpEmailSender> _logger;

    public MailerooOtpEmailSender(
        IHttpClientFactory httpClientFactory,
        IOptions<MailerooOptions> options,
        IOtpEmailBodyRenderer bodyRenderer,
        ILogger<MailerooOtpEmailSender> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bodyRenderer = bodyRenderer ?? throw new ArgumentNullException(nameof(bodyRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendOtpEmailAsync(string tenantId, string recipientEmail, string oneTimeCode, CancellationToken cancellationToken)
    {
        if (!_options.Value.Tenants.TryGetValue(tenantId, out var sender))
        {
            throw new InvalidOperationException($"No Maileroo sender configured for tenant '{tenantId}'.");
        }

        // MailerooClient stores its API key as a default header on the HttpClient it's given, so
        // every tenant (and therefore every distinct API key) must get its own HttpClient instance
        // rather than sharing one — otherwise concurrent requests for different tenants could race
        // and send one tenant's OTP using another tenant's sending key.
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var client = new MailerooClient(sender.ApiKey, httpClient: httpClient);

        var payload = new Dictionary<string, object?>
        {
            ["from"] = new EmailAddress(sender.FromAddress, sender.FromDisplayName),
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
            _logger.LogError(ex, "Maileroo send failed for tenant {TenantId}.", tenantId);
            throw new InvalidOperationException("Maileroo send failed.", ex);
        }
    }
}
