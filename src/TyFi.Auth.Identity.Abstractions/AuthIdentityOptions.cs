namespace TyFi.Auth.Identity.Abstractions;

/// <summary>Options controlling one-time code, lockout, and token lifetimes/signing.</summary>
public sealed class AuthIdentityOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Auth:Identity";

    /// <summary>Number of digits in a login/registration one-time code. Default 6.</summary>
    public int OtpCodeLength { get; set; } = 6;

    /// <summary>How long a one-time code remains valid after being sent. Default 10 minutes.</summary>
    public TimeSpan OtpCodeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Maximum incorrect attempts allowed against one outstanding code. Default 5.</summary>
    public int OtpMaxAttempts { get; set; } = 5;

    /// <summary>
    /// Minimum time between sending a new code to the same account, whether via registration or a
    /// login-code request. Rejected resend attempts within this window still return the same
    /// enumeration-safe result -- they just don't trigger another email. Default 60 seconds.
    /// </summary>
    public TimeSpan MinimumCodeResendInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Consecutive failed logins before the account is temporarily locked out. Default 5.</summary>
    public int MaxFailedLoginAttemptsBeforeLockout { get; set; } = 5;

    /// <summary>How long an account stays locked out once triggered. Default 15 minutes.</summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>The <c>iss</c> claim value for issued access tokens.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The <c>aud</c> claim value for issued access tokens.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded symmetric key (HMAC-SHA256, at least 32 bytes) used to sign access tokens.
    /// Should come from Key Vault/app settings in production, never source control.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded symmetric key (HMAC-SHA256, at least 32 bytes) used to hash one-time codes and
    /// refresh tokens for storage. Deliberately separate from <see cref="SigningKey"/> so the two crypto
    /// uses never share key material.
    /// </summary>
    public string HashingKey { get; set; } = string.Empty;

    /// <summary>How long an issued access token is valid. Default 15 minutes.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long an issued refresh token is valid if never used. Default 30 days.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}
