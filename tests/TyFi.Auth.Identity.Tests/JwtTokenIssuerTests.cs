using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TyFi.Auth.Abstractions;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity.Tests;

public sealed class JwtTokenIssuerTests
{
    private const string SigningKey = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY="; // 32 bytes, base64

    private static JwtTokenIssuer CreateIssuer(
        PermissionCatalogOptions? permissionCatalog = null,
        IClaimsEnricher? claimsEnricher = null)
    {
        var options = new AuthIdentityOptions
        {
            Issuer = "https://issuer.example",
            Audience = "api://audience",
            SigningKey = SigningKey,
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
        };

        return new JwtTokenIssuer(
            Options.Create(options),
            Options.Create(new AuthClaimMappingOptions()),
            Options.Create(permissionCatalog ?? new PermissionCatalogOptions()),
            claimsEnricher ?? new NoOpClaimsEnricher());
    }

    private static JsonWebToken Decode(string token) => new(token);

    [Fact]
    public async Task IssueAccessTokenAsync_IncludesSubjectAndEmailClaims()
    {
        var issuer = CreateIssuer();
        var user = new AuthUserRecord { Id = "user-1", Email = "a@b.com" };

        var result = await issuer.IssueAccessTokenAsync(user, CancellationToken.None);
        var jwt = Decode(result.Token);

        Assert.Equal("user-1", jwt.GetClaim("sub").Value);
        Assert.Equal("a@b.com", jwt.GetClaim("email").Value);
        Assert.Equal("https://issuer.example", jwt.Issuer);
        Assert.Equal("api://audience", jwt.Audiences.Single());
    }

    [Fact]
    public async Task IssueAccessTokenAsync_ExpandsRolesToGrantedPermissions()
    {
        var permissionCatalog = new PermissionCatalogOptions
        {
            RoleAssignments = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Admin"] = ["WidgetRead", "WidgetWrite"],
            },
        };
        var issuer = CreateIssuer(permissionCatalog);
        var user = new AuthUserRecord { Id = "user-1", Email = "a@b.com", Roles = ["Admin"] };

        var result = await issuer.IssueAccessTokenAsync(user, CancellationToken.None);
        var jwt = Decode(result.Token);

        var roleClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("WidgetRead", roleClaims);
        Assert.Contains("WidgetWrite", roleClaims);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_IsSignedWithConfiguredKey()
    {
        var issuer = CreateIssuer();
        var user = new AuthUserRecord { Id = "user-1", Email = "a@b.com" };

        var result = await issuer.IssueAccessTokenAsync(user, CancellationToken.None);

        var handler = new JsonWebTokenHandler();
        var validationResult = await handler.ValidateTokenAsync(
            result.Token,
            new TokenValidationParameters
            {
                ValidIssuer = "https://issuer.example",
                ValidAudience = "api://audience",
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(SigningKey)),
            });

        Assert.True(validationResult.IsValid);
    }
}
