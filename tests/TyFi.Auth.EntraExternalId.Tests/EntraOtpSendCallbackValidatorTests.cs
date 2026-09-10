using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Cryptography;
using TyFi.Auth.Jwt;

namespace TyFi.Auth.EntraExternalId.Tests;

public sealed class EntraOtpSendCallbackValidatorTests : IDisposable
{
    private const string Issuer = "https://contoso.ciamlogin.com/tenant-guid/v2.0";
    private const string Audience = "api://function-app/tenant-app-id";

    private readonly RSA _rsa = RSA.Create(2048);

    private string IssueToken(string issuer, string audience, string? azp, DateTime? expires = null)
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
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        };
        return handler.CreateToken(descriptor);
    }

    private EntraOtpSendCallbackValidator CreateValidator(string issuer = Issuer, string audience = Audience)
    {
        var options = Options.Create(new EntraOtpSendOptions { Issuer = issuer, Audience = audience });

        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false)));

        var configCache = new Mock<IOpenIdConnectConfigurationCache>();
        configCache.Setup(c => c.GetConfigurationAsync(issuer, It.IsAny<CancellationToken>())).ReturnsAsync(configuration);

        return new EntraOtpSendCallbackValidator(options, configCache.Object, NullLogger<EntraOtpSendCallbackValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_Succeeds_ForWellFormedMicrosoftCallerToken()
    {
        var validator = CreateValidator();
        var token = IssueToken(Issuer, Audience, AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId);

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.True(outcome.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_Fails_WhenNotConfigured()
    {
        var options = Options.Create(new EntraOtpSendOptions());
        var validator = new EntraOtpSendCallbackValidator(options, Mock.Of<IOpenIdConnectConfigurationCache>(), NullLogger<EntraOtpSendCallbackValidator>.Instance);

        var outcome = await validator.ValidateAsync("Bearer whatever", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("Authentication is not configured.", outcome.FailureReason);
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
        var token = IssueToken(Issuer, "wrong-aud", AuthenticationEventsConstants.MicrosoftAuthenticationEventsClientId);

        var outcome = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(outcome.IsValid);
        Assert.Equal("token_validation_failed", outcome.FailureReason);
    }

    public void Dispose() => _rsa.Dispose();
}
