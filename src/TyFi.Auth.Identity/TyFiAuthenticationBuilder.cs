using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>
/// Fluent builder returned by <c>AddAuthentication</c> for configuring roles, permissions, and optional
/// claim enrichment. Wraps the same <see cref="IServiceCollection"/> passed to <c>AddAuthentication</c>.
/// </summary>
public sealed class TyFiAuthenticationBuilder
{
    /// <summary>The underlying service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Creates the builder around the given service collection.</summary>
    public TyFiAuthenticationBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Declares the catalog of valid role names a user may be assigned.</summary>
    public TyFiAuthenticationBuilder AddRoles(params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        Services.Configure<RoleCatalogOptions>(o => o.Roles = roles);
        return this;
    }

    /// <summary>
    /// Declares the catalog of valid permission names and which roles grant which permissions. At
    /// token-issuance time, each user's roles are expanded through <paramref name="assignments"/> so the
    /// access token carries both role names and every permission those roles grant.
    /// </summary>
    public TyFiAuthenticationBuilder AddPermissions(string[] permissions, IReadOnlyDictionary<string, string[]> assignments)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        ArgumentNullException.ThrowIfNull(assignments);

        Services.Configure<PermissionCatalogOptions>(o =>
        {
            o.Permissions = permissions;
            o.RoleAssignments = assignments.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value);
        });
        return this;
    }

    /// <summary>Registers a project-specific <see cref="IClaimsEnricher"/>, replacing the no-op default.</summary>
    public TyFiAuthenticationBuilder WithClaimsEnricher<TEnricher>() where TEnricher : class, IClaimsEnricher
    {
        Services.Replace(ServiceDescriptor.Scoped<IClaimsEnricher, TEnricher>());
        return this;
    }
}
