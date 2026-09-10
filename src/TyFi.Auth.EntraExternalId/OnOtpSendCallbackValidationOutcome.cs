namespace TyFi.Auth.EntraExternalId;

/// <summary>The outcome of validating an <c>OnOtpSend</c> callback's bearer token.</summary>
public sealed record OnOtpSendCallbackValidationOutcome(bool IsValid, string? FailureReason)
{
    /// <summary>Creates a successful outcome.</summary>
    public static OnOtpSendCallbackValidationOutcome Success() => new(true, null);

    /// <summary>Creates a failed outcome with the given reason.</summary>
    public static OnOtpSendCallbackValidationOutcome Failure(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new OnOtpSendCallbackValidationOutcome(false, reason);
    }
}
