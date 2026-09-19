using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TyFi.Auth.Abstractions;
using TyFi.Auth.Identity;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Jwt.Tests;

/// <summary>
/// Proves the actual round trip: a token issued by <c>TyFi.Auth.Identity</c>'s
/// <see cref="JwtTokenIssuer"/> validates through this package's real
/// <see cref="JwtBearerTokenAuthenticator"/> -- not a hand-rolled <c>ValidateTokenAsync</c> call.
/// </summary>
public sealed class IdentityJwtRoundTripTests
{
    [Fact]
    public async Task TokenIssuedByIdentity_ValidatesThroughTheRealJwtAuthenticator()
    {
        const string issuer = "https://therapy-scheduling.example/";
        const string audience = "api://therapy-scheduling";
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var signingKey = Convert.ToBase64String(keyBytes);

        var identityOptions = Options.Create(new AuthIdentityOptions
        {
            Issuer = issuer,
            Audience = audience,
            SigningKey = signingKey,
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
        });
        var permissionCatalog = Options.Create(new PermissionCatalogOptions
        {
            RoleAssignments = new Dictionary<string, IReadOnlyList<string>> { ["Admin"] = ["WidgetRead", "WidgetWrite"] },
        });
        var claimMapping = Options.Create(new AuthClaimMappingOptions());

        var tokenIssuer = new JwtTokenIssuer(identityOptions, claimMapping, permissionCatalog, new NoOpClaimsEnricher());
        var user = new AuthUserRecord { Id = Guid.NewGuid().ToString(), Email = "a@b.com", Roles = ["Admin"] };
        var issued = await tokenIssuer.IssueAccessTokenAsync(user, CancellationToken.None);

        var authenticatorOptions = Options.Create(new JwtBearerTokenAuthenticatorOptions
        {
            Issuer = issuer,
            Audience = audience,
            ValidAlgorithms = ["HS256"],
            SigningKey = signingKey,
        });
        var authenticator = new JwtBearerTokenAuthenticator(
            authenticatorOptions,
            new AuthorizationHeaderParser(),
            Moq.Mock.Of<IOpenIdConnectConfigurationCache>(),
            NullLogger<JwtBearerTokenAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync($"Bearer {issued.Token}", CancellationToken.None);

        Assert.True(result.IsAuthenticated);
        Assert.Equal(user.Id, result.User!.Subject);
        Assert.Contains("Admin", result.User.Roles);
        Assert.Contains("WidgetRead", result.User.Roles);
        Assert.Contains("WidgetWrite", result.User.Roles);
    }
}
