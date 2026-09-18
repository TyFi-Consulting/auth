namespace TyFi.Auth.Identity.Abstractions;

/// <summary>Outcome of <see cref="IAuthenticationService.RegisterAsync"/>.</summary>
public enum RegisterOutcome
{
    /// <summary>A new account was created and a verification code was sent.</summary>
    Started,

    /// <summary>An account already exists for this email address.</summary>
    AlreadyRegistered,
}

/// <summary>Result of <see cref="IAuthenticationService.RegisterAsync"/>.</summary>
public sealed record RegisterResult(RegisterOutcome Outcome)
{
    /// <summary>Creates a <see cref="RegisterOutcome.Started"/> result.</summary>
    public static RegisterResult Started() => new(RegisterOutcome.Started);

    /// <summary>Creates a <see cref="RegisterOutcome.AlreadyRegistered"/> result.</summary>
    public static RegisterResult AlreadyRegistered() => new(RegisterOutcome.AlreadyRegistered);
}
