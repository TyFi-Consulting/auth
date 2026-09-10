using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TyFi.Auth.EntraExternalId;

/// <inheritdoc cref="IOnOtpSendRequestHandler" />
public sealed class OnOtpSendRequestHandler : IOnOtpSendRequestHandler
{
    private readonly IOnOtpSendCallbackValidator _callbackValidator;
    private readonly IOtpEmailSender _emailSender;
    private readonly IOptions<EntraOtpSendOptions> _options;
    private readonly ILogger<OnOtpSendRequestHandler> _logger;

    /// <summary>Creates the handler using the given validator, email sender, options, and logger.</summary>
    public OnOtpSendRequestHandler(
        IOnOtpSendCallbackValidator callbackValidator,
        IOtpEmailSender emailSender,
        IOptions<EntraOtpSendOptions> options,
        ILogger<OnOtpSendRequestHandler> logger)
    {
        _callbackValidator = callbackValidator ?? throw new ArgumentNullException(nameof(callbackValidator));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OnOtpSendHandlerResult> HandleAsync(string? authorizationHeaderValue, Stream requestBody, CancellationToken cancellationToken)
    {
        var validationOutcome = await _callbackValidator.ValidateAsync(authorizationHeaderValue, cancellationToken).ConfigureAwait(false);
        if (!validationOutcome.IsValid)
        {
            return OnOtpSendHandlerResult.Failure(401, validationOutcome.FailureReason ?? "unauthorized");
        }

        var payload = await JsonSerializer.DeserializeAsync<OnOtpSendRequest>(requestBody, cancellationToken: cancellationToken).ConfigureAwait(false);
        var identifier = payload?.Data?.OtpContext?.Identifier;
        var oneTimeCode = payload?.Data?.OtpContext?.OneTimeCode;
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(oneTimeCode))
        {
            return OnOtpSendHandlerResult.Failure(400, "missing_otp_context");
        }

        try
        {
            // Bounded well under Entra's ~2s response timeout so a slow/hung provider fails fast
            // into a 502 instead of the whole callback timing out (error 1003005 CustomExtensionTimedOut).
            using var timeoutCts = new CancellationTokenSource(_options.Value.ResponseTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await _emailSender.SendOtpEmailAsync(identifier, oneTimeCode, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Our own response-time budget expired, not the caller's cancellation — treat as a send failure.
                throw new InvalidOperationException("Maileroo send exceeded the response time budget.");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to send OTP email.");
            return OnOtpSendHandlerResult.Failure(502, "email_send_failed");
        }

        return OnOtpSendHandlerResult.Ok(OnOtpSendResponse.ContinueWithDefaultBehavior());
    }
}
