using System.Text.Json.Serialization;

namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Response schema Entra expects from an OnOtpSend callback. See the Microsoft-provided reference
/// implementation at https://learn.microsoft.com/entra/identity-platform/custom-extension-email-otp-get-started.
/// </summary>
public sealed class OnOtpSendResponse
{
    /// <summary>The response's data envelope.</summary>
    [JsonPropertyName("data")]
    public required OnOtpSendResponseData Data { get; init; }

    /// <summary>Builds the response signaling Entra should continue after the custom send.</summary>
    public static OnOtpSendResponse ContinueWithDefaultBehavior() => new()
    {
        Data = new OnOtpSendResponseData
        {
            Actions = [new OnOtpSendResponseAction { ODataType = "microsoft.graph.OtpSend.continueWithDefaultBehavior" }],
        },
    };
}

/// <summary>The `data` envelope of an <see cref="OnOtpSendResponse"/>.</summary>
public sealed class OnOtpSendResponseData
{
    /// <summary>The OData type discriminator for this response shape.</summary>
    [JsonPropertyName("@odata.type")]
    public string ODataType { get; init; } = "microsoft.graph.OnOtpSendResponseData";

    /// <summary>The actions Entra should take next.</summary>
    [JsonPropertyName("actions")]
    public required List<OnOtpSendResponseAction> Actions { get; init; }
}

/// <summary>A single action in an <see cref="OnOtpSendResponseData"/>.</summary>
public sealed class OnOtpSendResponseAction
{
    /// <summary>The OData type discriminator for this action.</summary>
    [JsonPropertyName("@odata.type")]
    public required string ODataType { get; init; }
}
