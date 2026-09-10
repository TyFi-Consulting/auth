using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace TyFi.Auth.Functions.Worker.Tests;

public sealed class JsonProblemResponseWriterTests
{
    // WriteAsJsonAsync resolves the worker's configured serializer from FunctionContext.InstanceServices.
    private static FakeFunctionContext CreateContext() => new()
    {
        InstanceServices = new ServiceCollection()
            .Configure<WorkerOptions>(o => o.Serializer = new JsonObjectSerializer())
            .BuildServiceProvider(),
    };

    [Fact]
    public async Task WriteAsync_WritesStatusCodeAndTitle()
    {
        var writer = new JsonProblemResponseWriter();
        var request = new FakeHttpRequestData(CreateContext());

        var response = await writer.WriteAsync(request, HttpStatusCode.Forbidden, "Insufficient permission", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        Assert.Equal("Insufficient permission", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(403, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenRequestIsNull()
    {
        var writer = new JsonProblemResponseWriter();

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteAsync(null!, HttpStatusCode.Unauthorized, "title", CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteAsync_Throws_WhenTitleIsMissing(string? title)
    {
        var writer = new JsonProblemResponseWriter();
        var request = new FakeHttpRequestData(CreateContext());

        await Assert.ThrowsAnyAsync<ArgumentException>(() => writer.WriteAsync(request, HttpStatusCode.Unauthorized, title!, CancellationToken.None));
    }
}
