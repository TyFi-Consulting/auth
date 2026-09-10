namespace TyFi.Auth.EntraExternalId;

/// <summary>Orchestrates a single OnOtpSend callback: token validation, payload parsing, and email dispatch.</summary>
public interface IOnOtpSendRequestHandler
{
    /// <summary>Handles one OnOtpSend callback invocation end to end.</summary>
    /// <param name="authorizationHeaderValue">The raw `Authorization` header value from the incoming request.</param>
    /// <param name="requestBody">The raw JSON request body stream.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task<OnOtpSendHandlerResult> HandleAsync(string? authorizationHeaderValue, Stream requestBody, CancellationToken cancellationToken);
}
