using TyFi.Auth.OtpMailer.Models;

namespace TyFi.Auth.OtpMailer.Handling;

public sealed record OnOtpSendHandlerResult(int StatusCode, OnOtpSendResponse? Response, string? FailureReason)
{
    public static OnOtpSendHandlerResult Ok(OnOtpSendResponse response) => new(200, response, null);

    public static OnOtpSendHandlerResult Failure(int statusCode, string reason) => new(statusCode, null, reason);
}
