using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace TyFi.Auth.Functions.Worker;

/// <inheritdoc cref="IAuthorizationRequirementResolver" />
public sealed class AuthorizationRequirementResolver : IAuthorizationRequirementResolver
{
    /// <inheritdoc />
    public AuthorizationRequirement Resolve(FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entryPoint = context.FunctionDefinition.EntryPoint;
        var separatorIndex = entryPoint.LastIndexOf('.');
        if (separatorIndex < 0)
        {
            // Fail closed: an unresolvable entry point requires an explicit policy, which will never be found.
            return new AuthorizationRequirement(false, null);
        }

        var assembly = Assembly.LoadFrom(context.FunctionDefinition.PathToAssembly);
        var type = assembly.GetType(entryPoint[..separatorIndex]);
        var method = type?.GetMethod(entryPoint[(separatorIndex + 1)..]);

        var allowAnonymous = method?.GetCustomAttribute<AllowAnonymousAttribute>() is not null
            || type?.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
        var authorize = method?.GetCustomAttribute<AuthorizeAttribute>()
            ?? type?.GetCustomAttribute<AuthorizeAttribute>();
        var authenticatedOnly = method?.GetCustomAttribute<AuthorizeAuthenticatedAttribute>() is not null
            || type?.GetCustomAttribute<AuthorizeAuthenticatedAttribute>() is not null;
        return new AuthorizationRequirement(allowAnonymous, authorize?.Policy, authenticatedOnly && authorize?.Policy is null);
    }
}
