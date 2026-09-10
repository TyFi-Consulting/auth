using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TyFi.Auth.OtpMailer.Handling;

namespace TyFi.Auth.OtpMailer.Functions;

public sealed class OnOtpSendFunction
{
    private readonly IOnOtpSendRequestHandler _handler;
    private readonly ILogger<OnOtpSendFunction> _logger;

    public OnOtpSendFunction(IOnOtpSendRequestHandler handler, ILogger<OnOtpSendFunction> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("OnOtpSend")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request.Headers.Authorization, request.Body, cancellationToken).ConfigureAwait(false);
        if (result.Response is not null)
        {
            return new OkObjectResult(result.Response);
        }

        _logger.LogWarning("OnOtpSend callback rejected with {StatusCode}: {Reason}", result.StatusCode, result.FailureReason);
        return new ObjectResult(new { error = result.FailureReason }) { StatusCode = result.StatusCode };
    }
}
