using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Convenience adapter so a consuming Function app's `[Function("OnOtpSend")]` stub (which must
/// live in the consuming app itself — isolated-worker function indexing does not scan referenced
/// library assemblies) can be a one-line call into <see cref="IOnOtpSendRequestHandler"/>.
/// </summary>
public static class OnOtpSendRequestHandlerHttpExtensions
{
    /// <summary>Handles the callback from an ASP.NET Core integration <see cref="HttpRequest"/>, returning the matching <see cref="IActionResult"/>.</summary>
    public static async Task<IActionResult> HandleHttpAsync(this IOnOtpSendRequestHandler handler, HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(request);

        var result = await handler.HandleAsync(request.Headers.Authorization, request.Body, cancellationToken).ConfigureAwait(false);
        if (result.Response is not null)
        {
            return new OkObjectResult(result.Response);
        }

        return new ObjectResult(new { error = result.FailureReason }) { StatusCode = result.StatusCode };
    }
}
