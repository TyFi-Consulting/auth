namespace TyFi.Auth.EntraExternalId;

/// <summary>The outcome of handling a single OnOtpSend callback.</summary>
public sealed record OnOtpSendHandlerResult(int StatusCode, OnOtpSendResponse? Response, string? FailureReason)
{
    /// <summary>Creates a successful (200) result.</summary>
    public static OnOtpSendHandlerResult Ok(OnOtpSendResponse response) => new(200, response, null);

    /// <summary>Creates a failure result with the given status code and reason.</summary>
    public static OnOtpSendHandlerResult Failure(int statusCode, string reason) => new(statusCode, null, reason);
}
