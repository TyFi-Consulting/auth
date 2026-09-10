using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Jwt;

/// <summary>
/// Provider-agnostic OIDC/JWT bearer-token authenticator. Behavior is entirely configuration-driven
/// (issuer, audience, claim-type mapping), so validating an Auth0 token vs. an Entra token is a
/// config-only difference — no provider-specific code lives here.
/// </summary>
public sealed class JwtBearerTokenAuthenticator : IBearerTokenAuthenticator
{
    private readonly IOptions<JwtBearerTokenAuthenticatorOptions> _options;
    private readonly IAuthorizationHeaderParser _headerParser;
    private readonly IOpenIdConnectConfigurationCache _configurationCache;
    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly ILogger<JwtBearerTokenAuthenticator> _logger;

    /// <summary>Creates a new validator using the given options, header parser, and JWKS cache.</summary>
    public JwtBearerTokenAuthenticator(
        IOptions<JwtBearerTokenAuthenticatorOptions> options,
        IAuthorizationHeaderParser headerParser,
        IOpenIdConnectConfigurationCache configurationCache,
        ILogger<JwtBearerTokenAuthenticator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _headerParser = headerParser ?? throw new ArgumentNullException(nameof(headerParser));
        _configurationCache = configurationCache ?? throw new ArgumentNullException(nameof(configurationCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BearerTokenAuthenticationResult> AuthenticateAsync(string? authorizationHeader, CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (!options.IsConfigured)
        {
            return BearerTokenAuthenticationResult.Failed("Authentication is not configured.");
        }

        var token = _headerParser.ParseBearerToken(authorizationHeader);
        if (token is null)
        {
            return BearerTokenAuthenticationResult.Failed("Missing bearer token.");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            ValidAlgorithms = options.ValidAlgorithms,
            ClockSkew = options.ClockSkew,
        };

        var configuration = await _configurationCache.GetConfigurationAsync(options.Issuer!, cancellationToken).ConfigureAwait(false);
        validationParameters.IssuerSigningKeys = configuration.SigningKeys;

        var result = await _tokenHandler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Key rotation: force a JWKS refresh and retry exactly once before failing.
            _configurationCache.RequestRefresh(options.Issuer!);
            configuration = await _configurationCache.GetConfigurationAsync(options.Issuer!, cancellationToken).ConfigureAwait(false);
            validationParameters.IssuerSigningKeys = configuration.SigningKeys;
            result = await _tokenHandler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);
        }

        if (!result.IsValid)
        {
            _logger.LogWarning(result.Exception, "Bearer token validation failed.");
            return BearerTokenAuthenticationResult.Failed("Token validation failed.");
        }

        var claimMapping = options.ClaimMapping;
        var subject = result.ClaimsIdentity.FindFirst(claimMapping.SubjectClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return BearerTokenAuthenticationResult.Failed("Token has no subject.");
        }

        var name = result.ClaimsIdentity.FindFirst(claimMapping.NameClaimType)?.Value;
        var roles = result.ClaimsIdentity.FindAll(claimMapping.RolesClaimType).Select(claim => claim.Value).ToList();
        var principal = new System.Security.Claims.ClaimsPrincipal(result.ClaimsIdentity);
        var user = AuthenticatedUser.Create(principal, options.Issuer!, subject, name, roles);
        return BearerTokenAuthenticationResult.Authenticated(user);
    }
}
