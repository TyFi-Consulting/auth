namespace TyFi.Auth.Abstractions;

/// <inheritdoc cref="IAuthorizationHeaderParser" />
public sealed class AuthorizationHeaderParser : IAuthorizationHeaderParser
{
    private const string BearerPrefix = "Bearer ";

    /// <inheritdoc />
    public string? ParseBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader[BearerPrefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}
