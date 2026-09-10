namespace TyFi.Auth.Abstractions;

/// <summary>Authenticates a raw `Authorization` header value against whatever token provider is configured.</summary>
public interface IBearerTokenAuthenticator
{
    /// <summary>Authenticates the given `Authorization` header value.</summary>
    /// <param name="authorizationHeader">The raw header value, e.g. "Bearer {token}", or null/empty if absent.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task<BearerTokenAuthenticationResult> AuthenticateAsync(string? authorizationHeader, CancellationToken cancellationToken);
}
