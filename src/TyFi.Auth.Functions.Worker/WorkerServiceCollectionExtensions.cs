using Microsoft.Extensions.DependencyInjection;

namespace TyFi.Auth.Functions.Worker;

/// <summary>
/// DI registration for the Functions isolated-worker authorization pieces. You must also register
/// the middleware on the host builder: <c>builder.UseMiddleware&lt;AuthorizationWorkerMiddleware&gt;()</c>.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAuthorizationRequirementResolver"/> and <see cref="IProblemResponseWriter"/>.</summary>
    public static IServiceCollection AddTyFiAuthFunctionsWorker(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationRequirementResolver, AuthorizationRequirementResolver>();
        services.AddSingleton<IProblemResponseWriter, JsonProblemResponseWriter>();
        return services;
    }
}
