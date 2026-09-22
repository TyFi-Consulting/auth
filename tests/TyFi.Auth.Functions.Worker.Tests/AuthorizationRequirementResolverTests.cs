using Microsoft.Azure.Functions.Worker;
using Moq;

namespace TyFi.Auth.Functions.Worker.Tests;

public sealed class AuthorizationRequirementResolverTests
{
    private static FunctionContext CreateContext(string methodName)
    {
        var entryPoint = $"{typeof(AuthorizationRequirementResolverFixtures).FullName}.{methodName}";
        var definition = new Mock<FunctionDefinition>();
        definition.SetupGet(d => d.EntryPoint).Returns(entryPoint);
        definition.SetupGet(d => d.PathToAssembly).Returns(typeof(AuthorizationRequirementResolverFixtures).Assembly.Location);

        var context = new Mock<FunctionContext>();
        context.SetupGet(c => c.FunctionDefinition).Returns(definition.Object);
        return context.Object;
    }

    [Fact]
    public void Resolve_AllowsAnonymous_WhenAttributePresent()
    {
        var resolver = new AuthorizationRequirementResolver();

        var requirement = resolver.Resolve(CreateContext(nameof(AuthorizationRequirementResolverFixtures.AnonymousFunction)));

        Assert.True(requirement.AllowAnonymous);
    }

    [Fact]
    public void Resolve_ReturnsPolicy_WhenAuthorizeAttributePresent()
    {
        var resolver = new AuthorizationRequirementResolver();

        var requirement = resolver.Resolve(CreateContext(nameof(AuthorizationRequirementResolverFixtures.FunctionWithPolicy)));

        Assert.False(requirement.AllowAnonymous);
        Assert.Equal("sessions:read", requirement.RequiredPolicy);
    }

    [Fact]
    public void Resolve_ReturnsNoPolicy_WhenNoAttributesPresent()
    {
        var resolver = new AuthorizationRequirementResolver();

        var requirement = resolver.Resolve(CreateContext(nameof(AuthorizationRequirementResolverFixtures.FunctionWithNoAttributes)));

        Assert.False(requirement.AllowAnonymous);
        Assert.Null(requirement.RequiredPolicy);
        Assert.False(requirement.RequireAuthenticatedOnly);
    }

    [Fact]
    public void Resolve_RequiresAuthenticatedOnly_WhenAttributePresentWithNoPolicy()
    {
        var resolver = new AuthorizationRequirementResolver();

        var requirement = resolver.Resolve(CreateContext(nameof(AuthorizationRequirementResolverFixtures.FunctionRequiringAuthenticatedOnly)));

        Assert.False(requirement.AllowAnonymous);
        Assert.Null(requirement.RequiredPolicy);
        Assert.True(requirement.RequireAuthenticatedOnly);
    }

    [Fact]
    public void Resolve_PolicyTakesPrecedence_WhenBothPolicyAndAuthenticatedOnlyPresent()
    {
        var resolver = new AuthorizationRequirementResolver();

        var requirement = resolver.Resolve(CreateContext(nameof(AuthorizationRequirementResolverFixtures.FunctionWithBothPolicyAndAuthenticatedOnly)));

        Assert.Equal("sessions:read", requirement.RequiredPolicy);
        Assert.False(requirement.RequireAuthenticatedOnly);
    }

    [Fact]
    public void Resolve_Throws_WhenContextIsNull()
    {
        var resolver = new AuthorizationRequirementResolver();

        Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null!));
    }
}
