namespace TyFi.Auth.Abstractions;

/// <summary>
/// Configurable claim-type mapping so Auth0-style (<c>permissions</c>) and Entra-style
/// (<c>roles</c>) tokens are both just configuration, never provider-specific code.
/// </summary>
public sealed class AuthClaimMappingOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "AuthClaimMapping";

    /// <summary>The claim type carrying the subject/user identifier. Defaults to the standard "sub" claim.</summary>
    public string SubjectClaimType { get; set; } = "sub";

    /// <summary>The claim type carrying the display name. Defaults to the standard "name" claim.</summary>
    public string NameClaimType { get; set; } = "name";

    /// <summary>The claim type carrying roles/permissions (e.g. "roles" for Entra, "permissions" for Auth0, or a custom URI claim).</summary>
    public string RolesClaimType { get; set; } = "roles";
}
