namespace TyFi.Auth.Functions.Worker;

/// <summary>
/// Translates a resolved <c>[Authorize(Policy = "...")]</c> policy name to the literal role
/// string checked against the caller's roles. Replace via DI (register after
/// <c>AddTyFiAuthFunctionsAspNetCore</c>/<c>AddTyFiAuthFunctionsWorker</c> so the override wins)
/// when role names are runtime configuration rather than compile-time literals — an attribute
/// argument must be a compile-time constant, but many consumers source their actual role names
/// from Terraform/App Configuration/etc.
/// </summary>
public interface IAuthorizationRoleNameResolver
{
    /// <summary>Returns the literal role name to check for the given policy.</summary>
    string ResolveRoleName(string policy);
}
