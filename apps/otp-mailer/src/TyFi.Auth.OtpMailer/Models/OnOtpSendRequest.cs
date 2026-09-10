using System.Text.Json.Serialization;

namespace TyFi.Auth.OtpMailer.Models;

public sealed class OnOtpSendRequest
{
    [JsonPropertyName("data")]
    public OnOtpSendRequestData? Data { get; init; }
}

public sealed class OnOtpSendRequestData
{
    [JsonPropertyName("otpContext")]
    public OtpContext? OtpContext { get; init; }
}

public sealed class OtpContext
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("onetimecode")]
    public string? OneTimeCode { get; init; }
}
