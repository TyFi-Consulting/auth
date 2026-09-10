using Microsoft.AspNetCore.Http;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Functions.AspNetCore;

/// <summary>Extension for retrieving the authenticated user from an <see cref="HttpContext"/>.</summary>
public static class HttpContextAuthenticationExtensions
{
    /// <summary>The key <see cref="AuthorizationAspNetCoreMiddleware"/> stores the authenticated user under.</summary>
    public const string UserContextItem = "TyFi.Auth.User";

    /// <summary>Gets the authenticated user for the current request. Throws if there is none (e.g. it's anonymous).</summary>
    public static AuthenticatedUser GetAuthenticatedUser(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (httpContext.Items.TryGetValue(UserContextItem, out var value) && value is AuthenticatedUser user)
        {
            return user;
        }

        throw new InvalidOperationException("The request does not have an authenticated user.");
    }
}
