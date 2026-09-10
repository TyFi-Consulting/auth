using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace TyFi.Auth.EntraExternalId.Tests;

public sealed class MailerooOtpEmailSenderTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = """{"success":true,"message":"ok","data":{"reference_id":"abc123"}}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(ResponseStatusCode) { Content = new StringContent(ResponseContent) };
        }
    }

    private static MailerooOptions ValidOptions() => new()
    {
        ApiKey = "test-sending-key",
        FromAddress = "noreply@example.com",
        FromDisplayName = "Example",
        Subject = "Your code",
    };

    private static MailerooOtpEmailSender CreateSender(FakeHttpMessageHandler handler, MailerooOptions? options = null) => new(
        new HttpClient(handler, disposeHandler: false),
        Options.Create(options ?? ValidOptions()),
        new DefaultOtpEmailBodyRenderer(),
        NullLogger<MailerooOtpEmailSender>.Instance);

    [Fact]
    public async Task SendOtpEmailAsync_PostsExpectedRequest()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        var sender = CreateSender(fakeHandler);

        await sender.SendOtpEmailAsync("user@example.com", "123456", CancellationToken.None);

        Assert.NotNull(fakeHandler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, fakeHandler.CapturedRequest!.Method);
        Assert.Equal("https://smtp.maileroo.com/api/v2/emails", fakeHandler.CapturedRequest.RequestUri!.ToString());
        Assert.Equal("test-sending-key", fakeHandler.CapturedRequest.Headers.Authorization!.Parameter);

        using var body = JsonDocument.Parse(fakeHandler.CapturedBody!);
        Assert.Equal("user@example.com", body.RootElement.GetProperty("to")[0].GetProperty("address").GetString());
        Assert.Contains("123456", body.RootElement.GetProperty("html").GetString());
    }

    [Fact]
    public async Task SendOtpEmailAsync_Throws_WhenNotConfigured()
    {
        var sender = CreateSender(new FakeHttpMessageHandler(), new MailerooOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendOtpEmailAsync("user@example.com", "123456", CancellationToken.None));
    }

    [Fact]
    public async Task SendOtpEmailAsync_Throws_WhenMailerooReturnsError()
    {
        var fakeHandler = new FakeHttpMessageHandler
        {
            ResponseStatusCode = HttpStatusCode.Unauthorized,
            ResponseContent = """{"success":false,"message":"Invalid API key"}""",
        };
        var sender = CreateSender(fakeHandler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendOtpEmailAsync("user@example.com", "123456", CancellationToken.None));
    }
}
