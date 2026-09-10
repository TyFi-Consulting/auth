using Microsoft.Extensions.DependencyInjection;
using TyFi.Auth.Functions.Worker;

namespace TyFi.Auth.Functions.AspNetCore;

/// <summary>
/// DI registration for the ASP.NET Core integration authorization pieces. You must also register
/// the middleware on the host builder: <c>builder.UseMiddleware&lt;AuthorizationAspNetCoreMiddleware&gt;()</c>.
/// </summary>
public static class AspNetCoreServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAuthorizationRequirementResolver"/> and <see cref="IProblemResultFactory"/>.</summary>
    public static IServiceCollection AddTyFiAuthFunctionsAspNetCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationRequirementResolver, AuthorizationRequirementResolver>();
        services.AddSingleton<IProblemResultFactory, JsonProblemResultFactory>();
        return services;
    }
}
