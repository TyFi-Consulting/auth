using System.Text.Json.Serialization;

namespace TyFi.Auth.EntraExternalId;

/// <summary>The OnOtpSend callback request payload Entra sends.</summary>
public sealed class OnOtpSendRequest
{
    /// <summary>The payload's data envelope.</summary>
    [JsonPropertyName("data")]
    public OnOtpSendRequestData? Data { get; init; }
}

/// <summary>The `data` envelope of an <see cref="OnOtpSendRequest"/>.</summary>
public sealed class OnOtpSendRequestData
{
    /// <summary>The OTP context containing the recipient and code.</summary>
    [JsonPropertyName("otpContext")]
    public OtpContext? OtpContext { get; init; }
}

/// <summary>The recipient and one-time code for a single OnOtpSend callback.</summary>
public sealed class OtpContext
{
    /// <summary>The recipient's email address.</summary>
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    /// <summary>The one-time passcode to send.</summary>
    [JsonPropertyName("onetimecode")]
    public string? OneTimeCode { get; init; }
}
