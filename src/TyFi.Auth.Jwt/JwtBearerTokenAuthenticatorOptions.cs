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

    /// <summary>True once both <see cref="Issuer"/> and <see cref="Audience"/> are set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience);
}
