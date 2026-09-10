namespace TyFi.Auth.Abstractions;

/// <summary>Extracts the raw token from an `Authorization: Bearer {token}` header value.</summary>
public interface IAuthorizationHeaderParser
{
    /// <summary>Returns the token, or null if the header is missing, empty, or not a Bearer-scheme value.</summary>
    string? ParseBearerToken(string? authorizationHeader);
}
