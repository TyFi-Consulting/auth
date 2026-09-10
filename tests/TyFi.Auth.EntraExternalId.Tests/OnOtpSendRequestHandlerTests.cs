using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace TyFi.Auth.EntraExternalId.Tests;

public sealed class OnOtpSendRequestHandlerTests
{
    private static Stream RequestBody(string identifier, string oneTimeCode) =>
        new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            data = new { otpContext = new { identifier, onetimecode = oneTimeCode } },
        })));

    private static OnOtpSendRequestHandler CreateHandler(
        IOnOtpSendCallbackValidator validator,
        IOtpEmailSender emailSender,
        TimeSpan? responseTimeout = null) => new(
            validator,
            emailSender,
            Options.Create(new EntraOtpSendOptions { ResponseTimeout = responseTimeout ?? TimeSpan.FromSeconds(5) }),
            NullLogger<OnOtpSendRequestHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenCallbackInvalid()
    {
        var validator = new Mock<IOnOtpSendCallbackValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnOtpSendCallbackValidationOutcome.Failure("unexpected_caller"));
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = CreateHandler(validator.Object, emailSender.Object);

        var result = await handler.HandleAsync("Bearer bad-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(401, result.StatusCode);
        Assert.Null(result.Response);
        emailSender.Verify(s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenOtpContextMissing()
    {
        var validator = new Mock<IOnOtpSendCallbackValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnOtpSendCallbackValidationOutcome.Success());
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = CreateHandler(validator.Object, emailSender.Object);

        using var emptyBody = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        var result = await handler.HandleAsync("Bearer good-token", emptyBody, CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadGateway_WhenEmailSendFails()
    {
        var validator = new Mock<IOnOtpSendCallbackValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnOtpSendCallbackValidationOutcome.Success());
        var emailSender = new Mock<IOtpEmailSender>();
        emailSender.Setup(s => s.SendOtpEmailAsync("user@example.com", "123456", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var handler = CreateHandler(validator.Object, emailSender.Object);

        var result = await handler.HandleAsync("Bearer good-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadGateway_WhenEmailSendExceedsResponseTimeout()
    {
        var validator = new Mock<IOnOtpSendCallbackValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnOtpSendCallbackValidationOutcome.Success());
        var emailSender = new Mock<IOtpEmailSender>();
        emailSender.Setup(s => s.SendOtpEmailAsync("user@example.com", "123456", It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken ct) => await Task.Delay(Timeout.Infinite, ct));
        var handler = CreateHandler(validator.Object, emailSender.Object, TimeSpan.FromMilliseconds(50));

        var result = await handler.HandleAsync("Bearer good-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(502, result.StatusCode);
        Assert.Equal("email_send_failed", result.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenEmailSendSucceeds()
    {
        var validator = new Mock<IOnOtpSendCallbackValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnOtpSendCallbackValidationOutcome.Success());
        var emailSender = new Mock<IOtpEmailSender>();
        var handler = CreateHandler(validator.Object, emailSender.Object);

        var result = await handler.HandleAsync("Bearer good-token", RequestBody("user@example.com", "123456"), CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Response);
        emailSender.Verify(s => s.SendOtpEmailAsync("user@example.com", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }
}
