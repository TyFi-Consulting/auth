using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TyFi.Auth.OtpMailer.Auth;

/// <summary>
/// Validates the Entra-issued bearer token sent with every OnOtpSend callback: signature, issuer,
/// audience (all per-tenant, from configuration), and that the caller is the fixed Microsoft
/// Authentication Events client (`azp`/`appid`). RS256 only to avoid algorithm-confusion attacks.
/// </summary>
public sealed class EntraCustomExtensionTokenValidator : IBearerTokenValidator
{
    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256];

    private readonly IOptions<OtpMailerAuthOptions> _options;
    private readonly IOpenIdConnectConfigurationCache _configurationCache;
    private readonly JsonWebTokenHandler _tokenHandler;
    private readonly ILogger<EntraCustomExtensionTokenValidator> _logger;

    public EntraCustomExtensionTokenValidator(
        IOptions<OtpMailerAuthOptions> options,
        IOpenIdConnectConfigurationCache configurationCache,
        ILogger<EntraCustomExtensionTokenValidator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configurationCache = configurationCache ?? throw new ArgumentNullException(nameof(configurationCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenHandler = new JsonWebTokenHandler();
    }

    /// <inheritdoc />
    public async Task<TokenValidationOutcome> ValidateAsync(string? authorizationHeaderValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return TokenValidationOutcome.Failure("missing_bearer_token");
        }

        var rawToken = authorizationHeaderValue["Bearer ".Length..].Trim();
        if (!_tokenHandler.CanReadToken(rawToken))
        {
            return TokenValidationOutcome.Failure("malformed_token");
        }

        var unvalidatedToken = _tokenHandler.ReadJsonWebToken(rawToken);
        var tenant = _options.Value.Tenants.FirstOrDefault(t => string.Equals(t.Issuer, unvalidatedToken.Issuer, StringComparison.OrdinalIgnoreCase));
        if (tenant is null)
        {
            _logger.LogWarning("Rejected OnOtpSend callback with unrecognized issuer {Issuer}.", unvalidatedToken.Issuer);
            return TokenValidationOutcome.Failure("unknown_issuer");
        }

        var configuration = await _configurationCache.GetConfigurationAsync(tenant.Issuer, cancellationToken).ConfigureAwait(false);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = tenant.Issuer,
            ValidAudience = tenant.Audience,
            ValidAlgorithms = AllowedAlgorithms,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        var result = await _tokenHandler.ValidateTokenAsync(rawToken, validationParameters).ConfigureAwait(false);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Key rotation: force a JWKS refresh and retry exactly once before failing.
            _configurationCache.RequestRefresh(tenant.Issuer);
            configuration = await _configurationCache.GetConfigurationAsync(tenant.Issuer, cancellationToken).ConfigureAwait(false);
            validationParameters.IssuerSigningKeys = configuration.SigningKeys;
            result = await _tokenHandler.ValidateTokenAsync(rawToken, validationParameters).ConfigureAwait(false);
        }

        if (!result.IsValid)
        {
            _logger.LogWarning(result.Exception, "OnOtpSend callback token failed validation for tenant {TenantId}.", tenant.TenantId);
            return TokenValidationOutcome.Failure("token_validation_failed");
        }

        var callerClientId = result.ClaimsIdentity.FindFirst("azp")?.Value ?? result.ClaimsIdentity.FindFirst("appid")?.Value;
        if (!string.Equals(callerClientId, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("OnOtpSend callback token has unexpected caller {CallerClientId} for tenant {TenantId}.", callerClientId, tenant.TenantId);
            return TokenValidationOutcome.Failure("unexpected_caller");
        }

        return TokenValidationOutcome.Success(tenant.TenantId);
    }
}
