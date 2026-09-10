using Microsoft.AspNetCore.Http;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Functions.AspNetCore.Tests;

public sealed class HttpContextAuthenticationExtensionsTests
{
    [Fact]
    public void GetAuthenticatedUser_ReturnsUser_WhenPresent()
    {
        var httpContext = new DefaultHttpContext();
        var user = AuthenticatedUser.Create(new System.Security.Claims.ClaimsPrincipal(), "iss", "sub", null, []);
        httpContext.Items[HttpContextAuthenticationExtensions.UserContextItem] = user;

        var result = httpContext.GetAuthenticatedUser();

        Assert.Same(user, result);
    }

    [Fact]
    public void GetAuthenticatedUser_Throws_WhenAbsent()
    {
        var httpContext = new DefaultHttpContext();

        Assert.Throws<InvalidOperationException>(() => httpContext.GetAuthenticatedUser());
    }

    [Fact]
    public void GetAuthenticatedUser_Throws_WhenHttpContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => HttpContextAuthenticationExtensions.GetAuthenticatedUser(null!));
    }
}
