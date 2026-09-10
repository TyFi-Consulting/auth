using System.Text.Json;
using Microsoft.Extensions.Logging;
using TyFi.Auth.OtpMailer.Auth;
using TyFi.Auth.OtpMailer.Mail;
using TyFi.Auth.OtpMailer.Models;

namespace TyFi.Auth.OtpMailer.Handling;

public sealed class OnOtpSendRequestHandler : IOnOtpSendRequestHandler
{
    private readonly IBearerTokenValidator _tokenValidator;
    private readonly IOtpEmailSender _emailSender;
    private readonly ILogger<OnOtpSendRequestHandler> _logger;

    public OnOtpSendRequestHandler(IBearerTokenValidator tokenValidator, IOtpEmailSender emailSender, ILogger<OnOtpSendRequestHandler> logger)
    {
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OnOtpSendHandlerResult> HandleAsync(string? authorizationHeaderValue, Stream requestBody, CancellationToken cancellationToken)
    {
        var tokenOutcome = await _tokenValidator.ValidateAsync(authorizationHeaderValue, cancellationToken).ConfigureAwait(false);
        if (!tokenOutcome.IsValid || tokenOutcome.TenantId is null)
        {
            return OnOtpSendHandlerResult.Failure(401, tokenOutcome.FailureReason ?? "unauthorized");
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
            await _emailSender.SendOtpEmailAsync(tokenOutcome.TenantId, identifier, oneTimeCode, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to send OTP email for tenant {TenantId}.", tokenOutcome.TenantId);
            return OnOtpSendHandlerResult.Failure(502, "email_send_failed");
        }

        return OnOtpSendHandlerResult.Ok(OnOtpSendResponse.ContinueWithDefaultBehavior());
    }
}
