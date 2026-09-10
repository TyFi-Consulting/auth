using Microsoft.Azure.Functions.Worker;

namespace TyFi.Auth.Functions.Worker;

/// <summary>Resolves the [Authorize]/[AllowAnonymous] policy for the function being invoked.</summary>
public interface IAuthorizationRequirementResolver
{
    /// <summary>Resolves the authorization requirement for the current invocation.</summary>
    AuthorizationRequirement Resolve(FunctionContext context);
}
