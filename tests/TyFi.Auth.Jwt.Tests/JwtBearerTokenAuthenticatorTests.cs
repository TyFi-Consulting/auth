using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Jwt.Tests;

public sealed class JwtBearerTokenAuthenticatorTests : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    private string IssueToken(string issuer, string audience, IDictionary<string, object>? claims = null, DateTime? expires = null)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims ?? new Dictionary<string, object>(),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256),
        };
        return handler.CreateToken(descriptor);
    }

    private JwtBearerTokenAuthenticator CreateAuthenticator(string issuer, string audience, AuthClaimMappingOptions? claimMapping = null)
    {
        var options = Options.Create(new JwtBearerTokenAuthenticatorOptions
        {
            Issuer = issuer,
            Audience = audience,
            ClaimMapping = claimMapping ?? new AuthClaimMappingOptions(),
        });

        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false)));

        var configCache = new Mock<IOpenIdConnectConfigurationCache>();
        configCache.Setup(c => c.GetConfigurationAsync(issuer, It.IsAny<CancellationToken>())).ReturnsAsync(configuration);

        return new JwtBearerTokenAuthenticator(options, new AuthorizationHeaderParser(), configCache.Object, NullLogger<JwtBearerTokenAuthenticator>.Instance);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_WhenNotConfigured()
    {
        var options = Options.Create(new JwtBearerTokenAuthenticatorOptions());
        var authenticator = new JwtBearerTokenAuthenticator(
            options, new AuthorizationHeaderParser(), Mock.Of<IOpenIdConnectConfigurationCache>(), NullLogger<JwtBearerTokenAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync("Bearer whatever", CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Authentication is not configured.", result.FailureReason);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_WhenBearerTokenMissing()
    {
        var authenticator = CreateAuthenticator("https://issuer.example/", "aud");

        var result = await authenticator.AuthenticateAsync(null, CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Missing bearer token.", result.FailureReason);
    }

    [Fact]
    public async Task AuthenticateAsync_Succeeds_ForEntraShapedToken()
    {
        const string issuer = "https://contoso.ciamlogin.com/tenant-guid/v2.0";
        const string audience = "api://contoso-app";
        var authenticator = CreateAuthenticator(issuer, audience);
        var token = IssueToken(issuer, audience, new Dictionary<string, object>
        {
            ["sub"] = "entra-subject",
            ["name"] = "Jane Entra",
            ["roles"] = new[] { "Admin", "Therapist" },
        });

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("entra-subject", result.User!.Subject);
        Assert.Equal("Jane Entra", result.User.Name);
        Assert.Equal(["Admin", "Therapist"], result.User.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_Succeeds_ForAuth0ShapedToken_ViaClaimMappingConfig()
    {
        const string issuer = "https://tyfi.auth0.com/";
        const string audience = "https://api.example.com";
        var claimMapping = new AuthClaimMappingOptions { RolesClaimType = "permissions" };
        var authenticator = CreateAuthenticator(issuer, audience, claimMapping);
        var token = IssueToken(issuer, audience, new Dictionary<string, object>
        {
            ["sub"] = "auth0|abc123",
            ["name"] = "Jane Auth0",
            ["permissions"] = new[] { "sessions:read", "sessions:write" },
        });

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("auth0|abc123", result.User!.Subject);
        Assert.Equal(["sessions:read", "sessions:write"], result.User.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_ForExpiredToken()
    {
        const string issuer = "https://issuer.example/";
        const string audience = "aud";
        var authenticator = CreateAuthenticator(issuer, audience);
        var token = IssueToken(issuer, audience, new Dictionary<string, object> { ["sub"] = "abc" }, DateTime.UtcNow.AddMinutes(-30));

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Token validation failed.", result.FailureReason);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_ForWrongAudience()
    {
        const string issuer = "https://issuer.example/";
        var authenticator = CreateAuthenticator(issuer, "expected-aud");
        var token = IssueToken(issuer, "wrong-aud", new Dictionary<string, object> { ["sub"] = "abc" });

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Token validation failed.", result.FailureReason);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_WhenSubjectClaimMissing()
    {
        const string issuer = "https://issuer.example/";
        const string audience = "aud";
        var authenticator = CreateAuthenticator(issuer, audience);
        var token = IssueToken(issuer, audience, new Dictionary<string, object> { ["name"] = "No Subject" });

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Token has no subject.", result.FailureReason);
    }

    [Fact]
    public async Task AuthenticateAsync_Succeeds_ForStaticSymmetricKey_WithoutCallingConfigurationCache()
    {
        const string issuer = "https://self-issued.example/";
        const string audience = "api://self";
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var signingKey = Convert.ToBase64String(keyBytes);

        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { ["sub"] = "user-1", ["roles"] = new[] { "Admin" } },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256),
        };
        var token = handler.CreateToken(descriptor);

        var options = Options.Create(new JwtBearerTokenAuthenticatorOptions
        {
            Issuer = issuer,
            Audience = audience,
            ValidAlgorithms = ["HS256"],
            SigningKey = signingKey,
        });
        var configCache = new Mock<IOpenIdConnectConfigurationCache>(MockBehavior.Strict);
        var authenticator = new JwtBearerTokenAuthenticator(
            options, new AuthorizationHeaderParser(), configCache.Object, NullLogger<JwtBearerTokenAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("user-1", result.User!.Subject);
        Assert.Equal(["Admin"], result.User.Roles);
        configCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_ForStaticSymmetricKey_WhenSignedWithADifferentKey()
    {
        const string issuer = "https://self-issued.example/";
        const string audience = "api://self";
        var signingKeyBytes = new byte[32];
        RandomNumberGenerator.Fill(signingKeyBytes);
        var wrongKeyBytes = new byte[32];
        RandomNumberGenerator.Fill(wrongKeyBytes);

        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { ["sub"] = "user-1" },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(wrongKeyBytes), SecurityAlgorithms.HmacSha256),
        };
        var token = handler.CreateToken(descriptor);

        var options = Options.Create(new JwtBearerTokenAuthenticatorOptions
        {
            Issuer = issuer,
            Audience = audience,
            ValidAlgorithms = ["HS256"],
            SigningKey = Convert.ToBase64String(signingKeyBytes),
        });
        var authenticator = new JwtBearerTokenAuthenticator(
            options, new AuthorizationHeaderParser(), Mock.Of<IOpenIdConnectConfigurationCache>(), NullLogger<JwtBearerTokenAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync($"Bearer {token}", CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Equal("Token validation failed.", result.FailureReason);
    }

    public void Dispose() => _rsa.Dispose();
}
