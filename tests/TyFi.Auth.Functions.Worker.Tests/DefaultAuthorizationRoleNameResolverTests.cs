namespace TyFi.Auth.Functions.Worker.Tests;

public sealed class DefaultAuthorizationRoleNameResolverTests
{
    [Fact]
    public void ResolveRoleName_ReturnsThePolicyUnchanged()
    {
        var resolver = new DefaultAuthorizationRoleNameResolver();

        var roleName = resolver.ResolveRoleName("Admin");

        Assert.Equal("Admin", roleName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveRoleName_RejectsAnEmptyPolicy(string? policy)
    {
        var resolver = new DefaultAuthorizationRoleNameResolver();

        Assert.ThrowsAny<ArgumentException>(() => resolver.ResolveRoleName(policy!));
    }
}
