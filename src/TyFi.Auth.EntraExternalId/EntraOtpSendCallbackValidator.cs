using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TyFi.Auth.Jwt;

namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Validates the Entra-issued bearer token sent with every OnOtpSend callback: signature, issuer,
/// audience (from configuration), and that the caller is the fixed Microsoft Authentication Events
/// client (`azp`/`appid`). RS256 only to avoid algorithm-confusion attacks. Fails closed when
/// unconfigured.
/// </summary>
public sealed class EntraOtpSendCallbackValidator : IOnOtpSendCallbackValidator
{
    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256];

    private readonly IOptions<EntraOtpSendOptions> _options;
    private readonly IOpenIdConnectConfigurationCache _configurationCache;
    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly ILogger<EntraOtpSendCallbackValidator> _logger;

    /// <summary>Creates a new validator using the given options, JWKS cache, and logger.</summary>
    public EntraOtpSendCallbackValidator(
        IOptions<EntraOtpSendOptions> options,
        IOpenIdConnectConfigurationCache configurationCache,
        ILogger<EntraOtpSendCallbackValidator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configurationCache = configurationCache ?? throw new ArgumentNullException(nameof(configurationCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OnOtpSendCallbackValidationOutcome> ValidateAsync(string? authorizationHeaderValue, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.IsConfigured)
        {
            return OnOtpSendCallbackValidationOutcome.Failure("Authentication is not configured.");
        }

        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return OnOtpSendCallbackValidationOutcome.Failure("missing_bearer_token");
        }

        var rawToken = authorizationHeaderValue["Bearer ".Length..].Trim();
        if (!_tokenHandler.CanReadToken(rawToken))
        {
            return OnOtpSendCallbackValidationOutcome.Failure("malformed_token");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            ValidAlgorithms = AllowedAlgorithms,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        var configuration = await _configurationCache.GetConfigurationAsync(options.Issuer!, cancellationToken).ConfigureAwait(false);
        validationParameters.IssuerSigningKeys = configuration.SigningKeys;

        var result = await _tokenHandler.ValidateTokenAsync(rawToken, validationParameters).ConfigureAwait(false);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Key rotation: force a JWKS refresh and retry exactly once before failing.
            _configurationCache.RequestRefresh(options.Issuer!);
            configuration = await _configurationCache.GetConfigurationAsync(options.Issuer!, cancellationToken).ConfigureAwait(false);
            validationParameters.IssuerSigningKeys = configuration.SigningKeys;
            result = await _tokenHandler.ValidateTokenAsync(rawToken, validationParameters).ConfigureAwait(false);
        }

        if (!result.IsValid)
        {
            _logger.LogWarning(result.Exception, "OnOtpSend callback token failed validation.");
            return OnOtpSendCallbackValidationOutcome.Failure("token_validation_failed");
        }

        var callerClientId = result.ClaimsIdentity.FindFirst("azp")?.Value ?? result.ClaimsIdentity.FindFirst("appid")?.Value;
        if (!string.Equals(callerClientId, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("OnOtpSend callback token has unexpected caller {CallerClientId}.", callerClientId);
            return OnOtpSendCallbackValidationOutcome.Failure("unexpected_caller");
        }

        return OnOtpSendCallbackValidationOutcome.Success();
    }
}
