namespace TyFi.Auth.Functions.Worker;

/// <inheritdoc cref="IAuthorizationRoleNameResolver" />
/// <remarks>Pass-through default: the policy name is already the literal role name.</remarks>
public sealed class DefaultAuthorizationRoleNameResolver : IAuthorizationRoleNameResolver
{
    /// <inheritdoc />
    public string ResolveRoleName(string policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        return policy;
    }
}
