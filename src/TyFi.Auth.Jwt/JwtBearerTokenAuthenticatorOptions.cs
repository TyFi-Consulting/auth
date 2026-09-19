namespace TyFi.Auth.Jwt;

/// <summary>
/// Options for <see cref="JwtBearerTokenAuthenticator"/>. Leaving <see cref="Issuer"/> or
/// <see cref="Audience"/> empty disables authentication entirely (<see cref="IsConfigured"/> is
/// false) — useful for local development environments that have no identity provider configured.
/// </summary>
public sealed class JwtBearerTokenAuthenticatorOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Auth:Jwt";

    /// <summary>The expected `iss` claim, e.g. "https://{tenant}.ciamlogin.com/{tenantId}/v2.0" or "https://{domain}/".</summary>
    public string? Issuer { get; set; }

    /// <summary>The expected `aud` claim: this API's audience/identifier.</summary>
    public string? Audience { get; set; }

    /// <summary>Allowed signing algorithms. Defaults to RS256 only, to avoid algorithm-confusion attacks.</summary>
    public IReadOnlyList<string> ValidAlgorithms { get; set; } = ["RS256"];

    /// <summary>Clock skew tolerance applied to token lifetime validation.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Claim-type mapping for subject/name/roles.</summary>
    public Abstractions.AuthClaimMappingOptions ClaimMapping { get; set; } = new();

    /// <summary>
    /// Base64-encoded symmetric key (HMAC-SHA256, at least 32 bytes). When set, validation uses this
    /// static key directly instead of OIDC discovery -- for self-issued tokens (e.g.
    /// <c>TyFi.Auth.Identity</c>) that have no discovery document or JWKS endpoint to fetch a public
    /// key from. Must be paired with <c>ValidAlgorithms: ["HS256"]</c>; it is not set automatically,
    /// so a misconfiguration fails closed instead of silently accepting an unintended algorithm.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>True once both <see cref="Issuer"/> and <see cref="Audience"/> are set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience);

    /// <summary>True when <see cref="SigningKey"/> is set, so validation should skip OIDC discovery.</summary>
    public bool UsesStaticSigningKey => !string.IsNullOrWhiteSpace(SigningKey);
}
