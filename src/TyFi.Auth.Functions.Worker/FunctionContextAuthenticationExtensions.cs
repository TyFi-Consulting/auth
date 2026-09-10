using Microsoft.Azure.Functions.Worker;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Functions.Worker;

/// <summary>Extension for retrieving the authenticated user from a <see cref="FunctionContext"/>.</summary>
public static class FunctionContextAuthenticationExtensions
{
    /// <summary>The key <see cref="AuthorizationWorkerMiddleware"/> stores the authenticated user under.</summary>
    public const string UserContextItem = "TyFi.Auth.User";

    /// <summary>Gets the authenticated user for the current invocation. Throws if the function has no authenticated user (e.g. it's anonymous).</summary>
    public static AuthenticatedUser GetAuthenticatedUser(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Items.TryGetValue(UserContextItem, out var value) && value is AuthenticatedUser user)
        {
            return user;
        }

        throw new InvalidOperationException("The function does not have an authenticated user.");
    }
}
