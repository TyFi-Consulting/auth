using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Cryptography;
using TyFi.Auth.OtpMailer.Auth;

namespace TyFi.Auth.OtpMailer.Tests.Auth;

public sealed class EntraCustomExtensionTokenValidatorTests : IDisposable
{
    private const string Issuer = "https://contoso.ciamlogin.com/tenant-guid/v2.0";
    private const string Audience = "api://function-app/tenant-app-id";
    private const string TenantId = "tenant-guid";

    private readonly RSA _rsa = RSA.Create(2048);

    private string IssueToken(string issuer, string audience, string? azp, DateTime? expires = null, string algorithm = SecurityAlgorithms.RsaSha256)
    {
        var key = new RsaSecurityKey(_rsa);
        var handler = new JsonWebTokenHandler();
        var claims = new Dictionary<string, object>();
        if (azp is not null)
        {
            claims["azp"] = azp;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, algorithm),
        };
        return handler.CreateToken(descriptor);
    }

    private EntraCustomExtensionTokenValidator CreateValidator()
    {
        var options = Options.Create(new OtpMailerAuthOptions
        {
            Tenants = [new TenantAuthOption { TenantId = TenantId, Issuer = Issuer, Audience = Audience }],
        });

        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false)));

        var configCache = new Mock<IOpenIdConnectConfigurationCache>();
        configCache.Setup(c => c.GetConfigurationAsync(Issuer, It.IsAny<CancellationToken>())).ReturnsAsync(configuration);

        return new EntraCustomExtensionTokenValidator(options, configCache.Object, NullLogger<EntraCustomExtensionTokenValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_Succeeds_ForWellFormedMicrosoftCallerToken()
    {
        var validator = CreateValidator();
        var token = IssueToken(Issuer, Audience, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId);

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.True(outcome.IsValid);
        Assert.Equal(TenantId, outcome.TenantId);
    }

    [Fact]
    public async Task ValidateAsync_Fails_WhenAuthorizationHeaderMissing()
    {
        var validator = CreateValidator();

        var outcome = await validator.ValidateAsync(null, CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("missing_bearer_token", outcome.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Fails_ForUnknownIssuer()
    {
        var validator = CreateValidator();
        var token = IssueToken("https://someone-else.ciamlogin.com/other/v2.0", Audience, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId);

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("unknown_issuer", outcome.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Fails_WhenCallerIsNotMicrosoftAuthenticationEvents()
    {
        var validator = CreateValidator();
        var token = IssueToken(Issuer, Audience, "some-other-client-id");

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("unexpected_caller", outcome.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Fails_ForExpiredToken()
    {
        var validator = CreateValidator();
        var token = IssueToken(Issuer, Audience, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId, DateTime.UtcNow.AddMinutes(-30));

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("token_validation_failed", outcome.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Fails_ForWrongAudience()
    {
        var validator = CreateValidator();
        var token = IssueToken(Issuer, "api://someone-else/other-app-id", AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId);

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("token_validation_failed", outcome.FailureReason);
    }

    public void Dispose() => _rsa.Dispose();
}
