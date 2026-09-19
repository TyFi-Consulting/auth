namespace TyFi.Auth.Identity.Abstractions;

/// <summary>Outcome of <see cref="IAuthenticationService.RefreshAsync"/>.</summary>
public enum RefreshOutcome
{
    /// <summary>The refresh token was valid and rotated; <see cref="RefreshResult.Tokens"/> is populated.</summary>
    Succeeded,

    /// <summary>The token doesn't exist, doesn't belong to a resolvable user, or is malformed.</summary>
    InvalidToken,

    /// <summary>The token exists but has expired.</summary>
    Expired,

    /// <summary>
    /// The token had already been rotated or revoked and was presented again -- a signal the token was
    /// stolen. The entire token family has been revoked as a result; the caller must sign in again.
    /// </summary>
    Reused,
}

/// <summary>Result of <see cref="IAuthenticationService.RefreshAsync"/>.</summary>
public sealed record RefreshResult(RefreshOutcome Outcome, AuthTokens? Tokens)
{
    /// <summary>Creates a successful result carrying the newly issued tokens.</summary>
    public static RefreshResult Succeeded(AuthTokens tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        return new RefreshResult(RefreshOutcome.Succeeded, tokens);
    }

    /// <summary>Creates a failed result with the given outcome.</summary>
    public static RefreshResult Failed(RefreshOutcome outcome)
    {
        if (outcome == RefreshOutcome.Succeeded)
        {
            throw new ArgumentException("Use Succeeded(tokens) for a successful result.", nameof(outcome));
        }

        return new RefreshResult(outcome, null);
    }
}
