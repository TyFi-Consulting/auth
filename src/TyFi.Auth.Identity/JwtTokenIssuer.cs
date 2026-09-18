using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TyFi.Auth.Abstractions;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>An issued access token and its expiry.</summary>
public readonly record struct AccessTokenIssuance(string Token, DateTimeOffset ExpiresUtc);

/// <summary>Issues signed JWT access tokens for authenticated users.</summary>
public interface ITokenIssuer
{
    /// <summary>Builds and signs an access token carrying the user's subject, email, roles, and expanded permissions.</summary>
    Task<AccessTokenIssuance> IssueAccessTokenAsync(AuthUserRecord user, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITokenIssuer" />
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly IOptions<AuthIdentityOptions> _options;
    private readonly IOptions<AuthClaimMappingOptions> _claimMapping;
    private readonly IOptions<PermissionCatalogOptions> _permissionCatalog;
    private readonly IClaimsEnricher _claimsEnricher;

    /// <summary>Creates the issuer.</summary>
    public JwtTokenIssuer(
        IOptions<AuthIdentityOptions> options,
        IOptions<AuthClaimMappingOptions> claimMapping,
        IOptions<PermissionCatalogOptions> permissionCatalog,
        IClaimsEnricher claimsEnricher)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _claimMapping = claimMapping ?? throw new ArgumentNullException(nameof(claimMapping));
        _permissionCatalog = permissionCatalog ?? throw new ArgumentNullException(nameof(permissionCatalog));
        _claimsEnricher = claimsEnricher ?? throw new ArgumentNullException(nameof(claimsEnricher));
    }

    /// <inheritdoc />
    public async Task<AccessTokenIssuance> IssueAccessTokenAsync(AuthUserRecord user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var mapping = _claimMapping.Value;
        var claims = new List<Claim>
        {
            new(mapping.SubjectClaimType, user.Id),
            new("email", user.Email),
        };

        // A user's roles are expanded through the permission catalog so the token carries both the
        // role names and every permission those roles grant, in the single configured claim type.
        var rolesAndPermissions = new HashSet<string>(user.Roles, StringComparer.Ordinal);
        foreach (var role in user.Roles)
        {
            if (_permissionCatalog.Value.RoleAssignments.TryGetValue(role, out var grantedPermissions))
            {
                foreach (var permission in grantedPermissions)
                {
                    rolesAndPermissions.Add(permission);
                }
            }
        }

        foreach (var value in rolesAndPermissions)
        {
            claims.Add(new Claim(mapping.RolesClaimType, value));
        }

        await _claimsEnricher.EnrichAsync(claims, user, cancellationToken).ConfigureAwait(false);

        var options = _options.Value;
        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var expiresUtc = now.Add(options.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expiresUtc,
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return new AccessTokenIssuance(token, expiresUtc);
    }
}
