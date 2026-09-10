using System.Security.Claims;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Abstractions.Tests;

public sealed class BearerTokenAuthenticationResultTests
{
    [Fact]
    public void Authenticated_ReturnsSuccessResult_WithUser()
    {
        var user = AuthenticatedUser.Create(new ClaimsPrincipal(), "iss", "sub", null, []);

        var result = BearerTokenAuthenticationResult.Authenticated(user);

        Assert.True(result.IsAuthenticated);
        Assert.Same(user, result.User);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void Authenticated_Throws_WhenUserIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => BearerTokenAuthenticationResult.Authenticated(null!));
    }

    [Fact]
    public void Failed_ReturnsFailureResult_WithReason()
    {
        var result = BearerTokenAuthenticationResult.Failed("missing bearer token");

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.User);
        Assert.Equal("missing bearer token", result.FailureReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failed_Throws_WhenReasonIsMissing(string? reason)
    {
        Assert.ThrowsAny<ArgumentException>(() => BearerTokenAuthenticationResult.Failed(reason!));
    }
}
