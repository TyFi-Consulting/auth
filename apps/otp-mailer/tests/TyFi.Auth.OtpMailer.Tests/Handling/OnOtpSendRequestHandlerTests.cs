using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TyFi.Auth.OtpMailer.Auth;
using TyFi.Auth.OtpMailer.Handling;
using TyFi.Auth.OtpMailer.Mail;

namespace TyFi.Auth.OtpMailer.Tests.Handling;

public sealed class OnOtpSendRequestHandlerTests
{
    private static Stream RequestBody(string identifier, string oneTimeCode) =>
        new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            data = new { otpContext = new { identifier, onetimecode = oneTimeCode } },
        })));

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenTokenInvalid()
    {
        var tokenValidator = new Mock<IBearerTokenValidator>();
        tokenValidator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenValidationOutcome.Failure("unknown_issuer"));
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = new OnOtpSendRequestHandler(tokenValidator.Object, emailSender.Object, NullLogger<OnOtpSendRequestHandler>.Instance);

        var result = await handler.HandleAsync("Bearer bad-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(401, result.StatusCode);
        Assert.Null(result.Response);
        emailSender.Verify(s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenOtpContextMissing()
    {
        var tokenValidator = new Mock<IBearerTokenValidator>();
        tokenValidator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenValidationOutcome.Success("tenant-1"));
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = new OnOtpSendRequestHandler(tokenValidator.Object, emailSender.Object, NullLogger<OnOtpSendRequestHandler>.Instance);

        using var emptyBody = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        var result = await handler.HandleAsync("Bearer good-token", emptyBody, CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadGateway_WhenEmailSendFails()
    {
        var tokenValidator = new Mock<IBearerTokenValidator>();
        tokenValidator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenValidationOutcome.Success("tenant-1"));
        var emailSender = new Mock<IOtpEmailSender>();
        emailSender.Setup(s => s.SendOtpEmailAsync("tenant-1", "user@example.com", "123456", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var handler = new OnOtpSendRequestHandler(tokenValidator.Object, emailSender.Object, NullLogger<OnOtpSendRequestHandler>.Instance);

        var result = await handler.HandleAsync("Bearer good-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(502, result.StatusCode);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenEmailSendSucceeds()
    {
        var tokenValidator = new Mock<IBearerTokenValidator>();
        tokenValidator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenValidationOutcome.Success("tenant-1"));
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = new OnOtpSendRequestHandler(tokenValidator.Object, emailSender.Object, NullLogger<OnOtpSendRequestHandler>.Instance);

        var result = await handler.HandleAsync("Bearer good-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Response);
        emailSender.Verify(s => s.SendOtpEmailAsync("tenant-1", "user@example.com", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }
}
