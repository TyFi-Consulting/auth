namespace TyFi.Auth.Identity.Abstractions;

/// <summary>The catalog of valid role names, configured once via <c>TyFiAuthenticationBuilder.AddRoles</c>.</summary>
public sealed class RoleCatalogOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Auth:Identity:Roles";

    /// <summary>All role names a user may be assigned.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
