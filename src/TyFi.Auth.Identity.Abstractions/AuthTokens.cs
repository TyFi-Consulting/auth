namespace TyFi.Auth.Identity.Abstractions;

/// <summary>An issued access/refresh token pair returned on a successful login or refresh.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresUtc);
