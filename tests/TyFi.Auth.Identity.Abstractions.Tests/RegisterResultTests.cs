namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class RegisterResultTests
{
    [Fact]
    public void Started_HasStartedOutcome()
    {
        var result = RegisterResult.Started();
        Assert.Equal(RegisterOutcome.Started, result.Outcome);
    }

    [Fact]
    public void AlreadyRegistered_HasAlreadyRegisteredOutcome()
    {
        var result = RegisterResult.AlreadyRegistered();
        Assert.Equal(RegisterOutcome.AlreadyRegistered, result.Outcome);
    }
}
