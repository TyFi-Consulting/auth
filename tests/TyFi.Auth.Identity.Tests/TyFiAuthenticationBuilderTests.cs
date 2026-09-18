using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity.Tests;

public sealed class TyFiAuthenticationBuilderTests
{
    [Fact]
    public void AddRoles_ConfiguresRoleCatalog()
    {
        var services = new ServiceCollection();
        var builder = new TyFiAuthenticationBuilder(services);

        builder.AddRoles("Admin", "User");

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOptions<RoleCatalogOptions>>().Value;
        Assert.Equal(["Admin", "User"], catalog.Roles);
    }

    [Fact]
    public void AddPermissions_ConfiguresPermissionCatalogAndRoleAssignments()
    {
        var services = new ServiceCollection();
        var builder = new TyFiAuthenticationBuilder(services);

        builder.AddPermissions(
            ["WidgetRead", "WidgetWrite"],
            new Dictionary<string, string[]>
            {
                ["Admin"] = ["WidgetRead", "WidgetWrite"],
                ["User"] = ["WidgetRead"],
            });

        var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IOptions<PermissionCatalogOptions>>().Value;

        Assert.Equal(["WidgetRead", "WidgetWrite"], catalog.Permissions);
        Assert.Equal(["WidgetRead", "WidgetWrite"], catalog.RoleAssignments["Admin"]);
        Assert.Equal(["WidgetRead"], catalog.RoleAssignments["User"]);
    }

    [Fact]
    public void WithClaimsEnricher_ReplacesDefaultRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClaimsEnricher, NoOpClaimsEnricher>();
        var builder = new TyFiAuthenticationBuilder(services);

        builder.WithClaimsEnricher<CustomClaimsEnricher>();

        var provider = services.BuildServiceProvider();
        Assert.IsType<CustomClaimsEnricher>(provider.GetRequiredService<IClaimsEnricher>());
    }

    private sealed class CustomClaimsEnricher : IClaimsEnricher
    {
        public Task EnrichAsync(IList<System.Security.Claims.Claim> claims, AuthUserRecord user, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
