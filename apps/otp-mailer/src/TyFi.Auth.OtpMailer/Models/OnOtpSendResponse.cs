using System.Text.Json.Serialization;

namespace TyFi.Auth.OtpMailer.Models;

/// <summary>
/// Response schema Entra expects from an OnOtpSend callback. See the Microsoft-provided reference
/// implementation at https://learn.microsoft.com/entra/identity-platform/custom-extension-email-otp-get-started.
/// </summary>
public sealed class OnOtpSendResponse
{
    [JsonPropertyName("data")]
    public required OnOtpSendResponseData Data { get; init; }

    public static OnOtpSendResponse ContinueWithDefaultBehavior() => new()
    {
        Data = new OnOtpSendResponseData
        {
            Actions = [new OnOtpSendResponseAction { ODataType = "microsoft.graph.OtpSend.continueWithDefaultBehavior" }],
        },
    };
}

public sealed class OnOtpSendResponseData
{
    [JsonPropertyName("@odata.type")]
    public string ODataType { get; init; } = "microsoft.graph.OnOtpSendResponseData";

    [JsonPropertyName("actions")]
    public required List<OnOtpSendResponseAction> Actions { get; init; }
}

public sealed class OnOtpSendResponseAction
{
    [JsonPropertyName("@odata.type")]
    public required string ODataType { get; init; }
}
