using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Functions.Worker;

/// <summary>
/// Isolated-worker middleware that enforces [Authorize]/[AllowAnonymous] on every HTTP-triggered
/// function. A function with neither attribute fails closed (500) rather than silently allowing
/// anonymous access.
/// </summary>
public sealed class AuthorizationWorkerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IAuthorizationRequirementResolver _requirementResolver;
    private readonly IBearerTokenAuthenticator _authenticator;
    private readonly IAuthorizationRoleNameResolver _roleNameResolver;
    private readonly IProblemResponseWriter _problemResponseWriter;
    private readonly ILogger<AuthorizationWorkerMiddleware> _logger;

    /// <summary>Creates the middleware with its resolver, authenticator, and response-writer dependencies.</summary>
    public AuthorizationWorkerMiddleware(
        IAuthorizationRequirementResolver requirementResolver,
        IBearerTokenAuthenticator authenticator,
        IAuthorizationRoleNameResolver roleNameResolver,
        IProblemResponseWriter problemResponseWriter,
        ILogger<AuthorizationWorkerMiddleware> logger)
    {
        _requirementResolver = requirementResolver ?? throw new ArgumentNullException(nameof(requirementResolver));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _roleNameResolver = roleNameResolver ?? throw new ArgumentNullException(nameof(roleNameResolver));
        _problemResponseWriter = problemResponseWriter ?? throw new ArgumentNullException(nameof(problemResponseWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Backward-compatible overload for consumers instantiating this middleware directly without a
    /// role-name resolver; defaults to <see cref="DefaultAuthorizationRoleNameResolver"/>.
    /// </summary>
    public AuthorizationWorkerMiddleware(
        IAuthorizationRequirementResolver requirementResolver,
        IBearerTokenAuthenticator authenticator,
        IProblemResponseWriter problemResponseWriter,
        ILogger<AuthorizationWorkerMiddleware> logger)
        : this(requirementResolver, authenticator, new DefaultAuthorizationRoleNameResolver(), problemResponseWriter, logger)
    {
    }

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requirement = _requirementResolver.Resolve(context);
        if (requirement.AllowAnonymous)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var request = await context.GetHttpRequestDataAsync().ConfigureAwait(false);
        if (request is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!requirement.RequireAuthenticatedOnly && string.IsNullOrWhiteSpace(requirement.RequiredPolicy))
        {
            _logger.LogError("Function {FunctionName} has no authorization policy.", context.FunctionDefinition.Name);
            context.GetInvocationResult().Value = await _problemResponseWriter.WriteAsync(
                request, HttpStatusCode.InternalServerError, "Authorization policy missing", context.CancellationToken).ConfigureAwait(false);
            return;
        }

        request.Headers.TryGetValues("Authorization", out var authorizationValues);
        var authorizationHeader = authorizationValues?.FirstOrDefault();

        BearerTokenAuthenticationResult authenticationResult;
        try
        {
            authenticationResult = await _authenticator.AuthenticateAsync(authorizationHeader, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException)
        {
            _logger.LogError(exception, "Authentication is unavailable.");
            context.GetInvocationResult().Value = await _problemResponseWriter.WriteAsync(
                request, HttpStatusCode.ServiceUnavailable, "Authentication service unavailable", context.CancellationToken).ConfigureAwait(false);
            return;
        }

        if (!authenticationResult.IsAuthenticated || authenticationResult.User is null)
        {
            context.GetInvocationResult().Value = await _problemResponseWriter.WriteAsync(
                request, HttpStatusCode.Unauthorized, "Access token required", context.CancellationToken).ConfigureAwait(false);
            return;
        }

        if (!requirement.RequireAuthenticatedOnly)
        {
            var roleName = _roleNameResolver.ResolveRoleName(requirement.RequiredPolicy!);
            if (!authenticationResult.User.Roles.Contains(roleName))
            {
                context.GetInvocationResult().Value = await _problemResponseWriter.WriteAsync(
                    request, HttpStatusCode.Forbidden, "Insufficient permission", context.CancellationToken).ConfigureAwait(false);
                return;
            }
        }

        context.Items[FunctionContextAuthenticationExtensions.UserContextItem] = authenticationResult.User;
        await next(context).ConfigureAwait(false);
    }
}
