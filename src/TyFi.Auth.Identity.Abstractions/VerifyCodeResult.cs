namespace TyFi.Auth.Identity.Abstractions;

/// <summary>Outcome of <see cref="IAuthenticationService.VerifyCodeAsync"/>.</summary>
public enum VerifyCodeOutcome
{
    /// <summary>The code was correct and <see cref="VerifyCodeResult.Tokens"/> is populated.</summary>
    Succeeded,

    /// <summary>No account, no pending code, or an incorrect code was supplied.</summary>
    InvalidCode,

    /// <summary>A pending code existed but has expired.</summary>
    Expired,

    /// <summary>Too many incorrect attempts were made against the current pending code.</summary>
    TooManyAttempts,

    /// <summary>The account is temporarily locked out due to prior failed attempts.</summary>
    AccountLocked,
}

/// <summary>Result of <see cref="IAuthenticationService.VerifyCodeAsync"/>.</summary>
public sealed record VerifyCodeResult(VerifyCodeOutcome Outcome, AuthTokens? Tokens)
{
    /// <summary>Creates a successful result carrying the issued tokens.</summary>
    public static VerifyCodeResult Succeeded(AuthTokens tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        return new VerifyCodeResult(VerifyCodeOutcome.Succeeded, tokens);
    }

    /// <summary>Creates a failed result with the given outcome.</summary>
    public static VerifyCodeResult Failed(VerifyCodeOutcome outcome)
    {
        if (outcome == VerifyCodeOutcome.Succeeded)
        {
            throw new ArgumentException("Use Succeeded(tokens) for a successful result.", nameof(outcome));
        }

        return new VerifyCodeResult(outcome, null);
    }
}
