namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Maps <see cref="IAuthenticationService"/> result outcomes to HTTP status codes, shared by every
/// hosting-model adapter (<c>TyFi.Auth.Functions.Worker</c>, <c>TyFi.Auth.Functions.AspNetCore</c>) so the
/// mapping is defined once and stays identical regardless of which HTTP model a project uses.
/// </summary>
public static class AuthOutcomeStatusCodes
{
    /// <summary>Status code for a <see cref="RegisterResult"/> outcome.</summary>
    public static int ForRegister(RegisterOutcome outcome) => outcome switch
    {
        RegisterOutcome.Started => 202,
        RegisterOutcome.AlreadyRegistered => 409,
        _ => 500,
    };

    /// <summary>Status code for a <see cref="VerifyCodeResult"/> outcome.</summary>
    public static int ForVerifyCode(VerifyCodeOutcome outcome) => outcome switch
    {
        VerifyCodeOutcome.Succeeded => 200,
        VerifyCodeOutcome.InvalidCode => 400,
        VerifyCodeOutcome.Expired => 400,
        VerifyCodeOutcome.TooManyAttempts => 429,
        VerifyCodeOutcome.AccountLocked => 423,
        _ => 500,
    };

    /// <summary>Status code for a <see cref="RefreshResult"/> outcome.</summary>
    public static int ForRefresh(RefreshOutcome outcome) => outcome switch
    {
        RefreshOutcome.Succeeded => 200,
        RefreshOutcome.InvalidToken => 401,
        RefreshOutcome.Expired => 401,
        RefreshOutcome.Reused => 401,
        _ => 500,
    };
}
