namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class RegisterResultTests
{
    [Fact]
    public void Started_IsASingleSharedInstance()
    {
        Assert.Same(RegisterResult.Started, RegisterResult.Started);
    }
}
