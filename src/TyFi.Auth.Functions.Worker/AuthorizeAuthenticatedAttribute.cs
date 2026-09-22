namespace TyFi.Auth.Functions.Worker;

/// <summary>
/// Marks a function as requiring authentication only — no specific role — for endpoints that
/// must work for any signed-in caller regardless of role membership (including one with no role
/// assigned yet, e.g. a "who am I" profile check right after registration). Mutually exclusive
/// with <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>'s <c>Policy</c>: if a
/// function also carries an explicit policy, the policy takes precedence and this attribute is
/// ignored.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuthorizeAuthenticatedAttribute : Attribute
{
}
