using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TyFi.Auth.Functions.AspNetCore.Tests;

public sealed class JsonProblemResultFactoryTests
{
    [Fact]
    public void Create_ReturnsObjectResult_WithStatusCodeAndTitle()
    {
        var factory = new JsonProblemResultFactory();

        var result = Assert.IsType<ObjectResult>(factory.Create(HttpStatusCode.Forbidden, "Insufficient permission"));

        Assert.Equal(403, result.StatusCode);
        var valueType = result.Value!.GetType();
        Assert.Equal("Insufficient permission", valueType.GetProperty("title")!.GetValue(result.Value));
        Assert.Equal(403, valueType.GetProperty("status")!.GetValue(result.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenTitleIsMissing(string? title)
    {
        var factory = new JsonProblemResultFactory();

        Assert.ThrowsAny<ArgumentException>(() => factory.Create(HttpStatusCode.Unauthorized, title!));
    }
}
