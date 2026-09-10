using System.Security.Claims;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Functions.Worker.Tests;

public sealed class FunctionContextAuthenticationExtensionsTests
{
    [Fact]
    public void GetAuthenticatedUser_ReturnsUser_WhenPresent()
    {
        var context = new FakeFunctionContext();
        var user = AuthenticatedUser.Create(new ClaimsPrincipal(), "iss", "sub", null, []);
        context.Items[FunctionContextAuthenticationExtensions.UserContextItem] = user;

        var result = context.GetAuthenticatedUser();

        Assert.Same(user, result);
    }

    [Fact]
    public void GetAuthenticatedUser_Throws_WhenAbsent()
    {
        var context = new FakeFunctionContext();

        Assert.Throws<InvalidOperationException>(() => context.GetAuthenticatedUser());
    }
}
