namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// The catalog of valid permission names and which roles grant which permissions, configured once via
/// <c>TyFiAuthenticationBuilder.AddPermissions</c>. At token-issuance time, a user's roles are expanded
/// through <see cref="RoleAssignments"/> so the access token carries both the user's roles and every
/// permission those roles grant.
/// </summary>
public sealed class PermissionCatalogOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Auth:Identity:Permissions";

    /// <summary>All permission names a role may grant.</summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>Maps each role name to the permissions it grants.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> RoleAssignments { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>();
}
