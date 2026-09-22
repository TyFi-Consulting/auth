using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using TyFi.Auth.Abstractions;
using TyFi.Auth.Functions.Worker;

namespace TyFi.Auth.Functions.AspNetCore;

/// <summary>
/// Isolated-worker middleware that enforces [Authorize]/[AllowAnonymous] for functions using the
/// ASP.NET Core integration HTTP model (<c>ConfigureFunctionsWebApplication</c>;
/// <c>HttpRequest</c>/<c>IActionResult</c> triggers). Under this model the function's result is
/// executed by the host after this middleware unwinds, so rejections are delivered by replacing
/// the invocation result (<c>context.GetInvocationResult().Value</c>) rather than writing to the response directly.
/// </summary>
public sealed class AuthorizationAspNetCoreMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IAuthorizationRequirementResolver _requirementResolver;
    private readonly IBearerTokenAuthenticator _authenticator;
    private readonly IAuthorizationRoleNameResolver _roleNameResolver;
    private readonly IProblemResultFactory _problemResultFactory;
    private readonly ILogger<AuthorizationAspNetCoreMiddleware> _logger;

    /// <summary>Creates the middleware with its resolver, authenticator, and result-factory dependencies.</summary>
    public AuthorizationAspNetCoreMiddleware(
        IAuthorizationRequirementResolver requirementResolver,
        IBearerTokenAuthenticator authenticator,
        IAuthorizationRoleNameResolver roleNameResolver,
        IProblemResultFactory problemResultFactory,
        ILogger<AuthorizationAspNetCoreMiddleware> logger)
    {
        _requirementResolver = requirementResolver ?? throw new ArgumentNullException(nameof(requirementResolver));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _roleNameResolver = roleNameResolver ?? throw new ArgumentNullException(nameof(roleNameResolver));
        _problemResultFactory = problemResultFactory ?? throw new ArgumentNullException(nameof(problemResultFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Backward-compatible overload for consumers instantiating this middleware directly without a
    /// role-name resolver; defaults to <see cref="DefaultAuthorizationRoleNameResolver"/>.
    /// </summary>
    public AuthorizationAspNetCoreMiddleware(
        IAuthorizationRequirementResolver requirementResolver,
        IBearerTokenAuthenticator authenticator,
        IProblemResultFactory problemResultFactory,
        ILogger<AuthorizationAspNetCoreMiddleware> logger)
        : this(requirementResolver, authenticator, new DefaultAuthorizationRoleNameResolver(), problemResultFactory, logger)
    {
    }

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var requirement = _requirementResolver.Resolve(context);
        if (requirement.AllowAnonymous)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!requirement.RequireAuthenticatedOnly && string.IsNullOrWhiteSpace(requirement.RequiredPolicy))
        {
            _logger.LogError("Function {FunctionName} has no authorization policy.", context.FunctionDefinition.Name);
            context.GetInvocationResult().Value = _problemResultFactory.Create(HttpStatusCode.InternalServerError, "Authorization policy missing");
            return;
        }

        var authorizationHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();

        BearerTokenAuthenticationResult authenticationResult;
        try
        {
            authenticationResult = await _authenticator.AuthenticateAsync(authorizationHeader, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException)
        {
            _logger.LogError(exception, "Authentication is unavailable.");
            context.GetInvocationResult().Value = _problemResultFactory.Create(HttpStatusCode.ServiceUnavailable, "Authentication service unavailable");
            return;
        }

        if (!authenticationResult.IsAuthenticated || authenticationResult.User is null)
        {
            context.GetInvocationResult().Value = _problemResultFactory.Create(HttpStatusCode.Unauthorized, "Access token required");
            return;
        }

        if (!requirement.RequireAuthenticatedOnly)
        {
            var roleName = _roleNameResolver.ResolveRoleName(requirement.RequiredPolicy!);
            if (!authenticationResult.User.Roles.Contains(roleName))
            {
                context.GetInvocationResult().Value = _problemResultFactory.Create(HttpStatusCode.Forbidden, "Insufficient permission");
                return;
            }
        }

        httpContext.Items[HttpContextAuthenticationExtensions.UserContextItem] = authenticationResult.User;
        await next(context).ConfigureAwait(false);
    }
}
