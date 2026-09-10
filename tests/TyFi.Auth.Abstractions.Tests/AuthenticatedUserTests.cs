using System.Security.Claims;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Abstractions.Tests;

public sealed class AuthenticatedUserTests
{
    [Fact]
    public void Create_ReturnsUser_WithGivenValues()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "abc")]));

        var user = AuthenticatedUser.Create(principal, "https://issuer.example/", "abc", "Jane Doe", ["Admin"]);

        Assert.Same(principal, user.Principal);
        Assert.Equal("https://issuer.example/", user.Issuer);
        Assert.Equal("abc", user.Subject);
        Assert.Equal("Jane Doe", user.Name);
        Assert.Equal(["Admin"], user.Roles);
    }

    [Fact]
    public void Create_Throws_WhenPrincipalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticatedUser.Create(null!, "iss", "sub", null, []));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenIssuerIsMissing(string? issuer)
    {
        var principal = new ClaimsPrincipal();
        Assert.ThrowsAny<ArgumentException>(() => AuthenticatedUser.Create(principal, issuer!, "sub", null, []));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenSubjectIsMissing(string? subject)
    {
        var principal = new ClaimsPrincipal();
        Assert.ThrowsAny<ArgumentException>(() => AuthenticatedUser.Create(principal, "iss", subject!, null, []));
    }

    [Fact]
    public void Create_Throws_WhenRolesIsNull()
    {
        var principal = new ClaimsPrincipal();
        Assert.Throws<ArgumentNullException>(() => AuthenticatedUser.Create(principal, "iss", "sub", null, null!));
    }
}
