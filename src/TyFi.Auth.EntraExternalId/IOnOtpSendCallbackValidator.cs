namespace TyFi.Auth.EntraExternalId;

/// <summary>Validates the bearer token Entra sends when invoking the OnOtpSend callback.</summary>
public interface IOnOtpSendCallbackValidator
{
    /// <summary>Validates the `Authorization` header value against this tenant's expected issuer/audience/caller.</summary>
    /// <param name="authorizationHeaderValue">The raw `Authorization` header value, e.g. "Bearer {token}".</param>
    /// <param name="cancellationToken">Cancellation token for the validation operation.</param>
    Task<OnOtpSendCallbackValidationOutcome> ValidateAsync(string? authorizationHeaderValue, CancellationToken cancellationToken);
}
