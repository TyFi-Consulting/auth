namespace TyFi.Auth.OtpMailer.Auth;

/// <summary>Validates the bearer token Entra sends when invoking the OnOtpSend callback.</summary>
public interface IBearerTokenValidator
{
    /// <summary>Validates the `Authorization` header value and, if valid, resolves which tenant issued it.</summary>
    /// <param name="authorizationHeaderValue">The raw `Authorization` header value, e.g. "Bearer {token}".</param>
    /// <param name="cancellationToken">Cancellation token for the validation operation.</param>
    Task<TokenValidationOutcome> ValidateAsync(string? authorizationHeaderValue, CancellationToken cancellationToken);
}
