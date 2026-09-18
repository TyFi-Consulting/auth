namespace TyFi.Auth.Identity.Abstractions.Tests;

public sealed class SingletonResultTests
{
    [Fact]
    public void RequestLoginCodeResult_Sent_IsASingleSharedInstance()
    {
        Assert.Same(RequestLoginCodeResult.Sent, RequestLoginCodeResult.Sent);
    }

    [Fact]
    public void LogoutResult_Succeeded_IsASingleSharedInstance()
    {
        Assert.Same(LogoutResult.Succeeded, LogoutResult.Succeeded);
    }
}

public sealed class AuthUserRecordTests
{
    [Fact]
    public void Roles_DefaultsToEmpty()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        Assert.Empty(user.Roles);
    }
}
