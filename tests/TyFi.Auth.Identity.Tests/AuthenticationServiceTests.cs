using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity.Tests;

public sealed class AuthenticationServiceTests
{
    private readonly Mock<IUserAccountStore> _userStore = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokenStore = new();
    private readonly Mock<IOtpGenerator> _otpGenerator = new();
    private readonly Mock<ISecretHasher> _secretHasher = new();
    private readonly Mock<ITokenIssuanceCoordinator> _tokenIssuanceCoordinator = new();
    private readonly ManualTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly AuthIdentityOptions _options = new();

    private AuthenticationService CreateService() => new(
        _userStore.Object,
        _emailSender.Object,
        _refreshTokenStore.Object,
        _otpGenerator.Object,
        _secretHasher.Object,
        _tokenIssuanceCoordinator.Object,
        new EmailNormalizer(),
        Options.Create(_options),
        NullLogger<AuthenticationService>.Instance,
        _timeProvider);

    [Fact]
    public async Task RegisterAsync_CreatesAccountAndSendsCode_WhenEmailNotRegistered()
    {
        AuthUserRecord? storedUser = null;
        _userStore.Setup(s => s.FindByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(storedUser));
        _userStore.Setup(s => s.CreateAsync(It.IsAny<AuthUserRecord>(), It.IsAny<CancellationToken>()))
            .Returns<AuthUserRecord, CancellationToken>((u, _) =>
            {
                storedUser = u;
                return Task.FromResult(u);
            });
        _userStore.Setup(s => s.UpdateAsync(It.IsAny<AuthUserRecord>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _otpGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("123456");
        _secretHasher.Setup(h => h.Hash("123456")).Returns("hashed");

        var service = CreateService();
        var result = await service.RegisterAsync("NEW@example.com", CancellationToken.None);

        Assert.Same(RegisterResult.Started, result);
        _userStore.Verify(
            s => s.CreateAsync(It.Is<AuthUserRecord>(u => u.Email == "new@example.com"), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSender.Verify(s => s.SendLoginCodeAsync("new@example.com", "123456", _options.OtpCodeLifetime, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_SendsLoginCode_WhenEmailAlreadyRegistered()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com", CreatedUtc = _timeProvider.GetUtcNow() };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _otpGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("999999");
        _secretHasher.Setup(h => h.Hash("999999")).Returns("hashed3");

        var service = CreateService();
        var result = await service.RegisterAsync("a@b.com", CancellationToken.None);

        Assert.Same(RegisterResult.Started, result);
        _userStore.Verify(s => s.CreateAsync(It.IsAny<AuthUserRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(s => s.SendLoginCodeAsync("a@b.com", "999999", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_AlwaysReturnsSent_RegardlessOfAccountExistence()
    {
        _userStore.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthUserRecord?)null);

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("unknown@example.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_SendsCode_WhenAccountExistsAndNotLockedOut()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _otpGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("654321");
        _secretHasher.Setup(h => h.Hash("654321")).Returns("hashed2");

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("a@b.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync("a@b.com", "654321", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_DoesNotSendCode_WhenAccountLockedOut()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com", LockoutEndUtc = _timeProvider.GetUtcNow().AddMinutes(5) };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("a@b.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_DoesNotSendCode_WithinResendCooldown()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com", LastCodeSentUtc = _timeProvider.GetUtcNow() };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("a@b.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_SendsCode_AfterCooldownElapses()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            LastCodeSentUtc = _timeProvider.GetUtcNow().Subtract(_options.MinimumCodeResendInterval).AddSeconds(-1),
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _otpGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("654321");
        _secretHasher.Setup(h => h.Hash("654321")).Returns("hashed2");

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("a@b.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync("a@b.com", "654321", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_DoesNotSendCode_WhenUpdateLosesRace()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _otpGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns("111111");
        _secretHasher.Setup(h => h.Hash("111111")).Returns("hashed4");

        var service = CreateService();
        var result = await service.RequestLoginCodeAsync("a@b.com", CancellationToken.None);

        Assert.Same(RequestLoginCodeResult.Sent, result);
        _emailSender.Verify(s => s.SendLoginCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsInvalidCode_WhenAccountDoesNotExist()
    {
        _userStore.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthUserRecord?)null);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("nope@example.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.InvalidCode, result.Outcome);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsAccountLocked_WhenLockoutActive()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com", LockoutEndUtc = _timeProvider.GetUtcNow().AddMinutes(5) };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.AccountLocked, result.Outcome);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsInvalidCode_WhenNoPendingCode()
    {
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        // No pending code must be indistinguishable from an unknown account (both InvalidCode),
        // otherwise this endpoint becomes an account-existence oracle.
        Assert.Equal(VerifyCodeOutcome.InvalidCode, result.Outcome);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsExpired_WhenPendingCodeExpired()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            PendingCodeHash = "hashed",
            PendingCodeExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(-1),
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsTooManyAttempts_WhenAttemptsExhausted()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            PendingCodeHash = "hashed",
            PendingCodeExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(5),
            PendingCodeAttempts = 5,
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.TooManyAttempts, result.Outcome);
    }

    [Fact]
    public async Task VerifyCodeAsync_IncrementsFailedAttempts_AndLocksOutAfterThreshold()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            PendingCodeHash = "hashed",
            PendingCodeExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(5),
            FailedLoginAttempts = 4,
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretHasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "wrong", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.InvalidCode, result.Outcome);
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.NotNull(user.LockoutEndUtc);
        _userStore.Verify(s => s.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_Succeeds_AndIssuesTokens_WhenCodeCorrect()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            PendingCodeHash = "hashed",
            PendingCodeExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(5),
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretHasher.Setup(h => h.Verify("123456", "hashed")).Returns(true);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var tokens = new AuthTokens("access", _timeProvider.GetUtcNow().AddMinutes(15), "refresh", _timeProvider.GetUtcNow().AddDays(30));
        _tokenIssuanceCoordinator.Setup(c => c.IssueAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(tokens);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.Succeeded, result.Outcome);
        Assert.Same(tokens, result.Tokens);
        Assert.Null(user.PendingCodeHash);
        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsInvalidCode_WhenConsumeLosesRace()
    {
        var user = new AuthUserRecord
        {
            Id = "1",
            Email = "a@b.com",
            PendingCodeHash = "hashed",
            PendingCodeExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(5),
        };
        _userStore.Setup(s => s.FindByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _secretHasher.Setup(h => h.Verify("123456", "hashed")).Returns(true);
        _userStore.Setup(s => s.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();
        var result = await service.VerifyCodeAsync("a@b.com", "123456", CancellationToken.None);

        Assert.Equal(VerifyCodeOutcome.InvalidCode, result.Outcome);
        Assert.Null(result.Tokens);
        _tokenIssuanceCoordinator.Verify(c => c.IssueAsync(It.IsAny<AuthUserRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsInvalidToken_WhenTokenNotFound()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync((RefreshTokenRecord?)null);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
    }

    [Fact]
    public async Task RefreshAsync_RevokesFamily_AndReturnsReused_WhenTokenAlreadyRotated()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord
        {
            TokenHash = "hash",
            FamilyId = "family1",
            UserId = "1",
            ExpiresUtc = _timeProvider.GetUtcNow().AddDays(1),
            RotatedUtc = _timeProvider.GetUtcNow(),
        };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.Reused, result.Outcome);
        _refreshTokenStore.Verify(s => s.RevokeFamilyAsync("family1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsExpired_WhenTokenExpired()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord { TokenHash = "hash", FamilyId = "family1", UserId = "1", ExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(-1) };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task RefreshAsync_Succeeds_AndRotatesToken_WhenValid()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord { TokenHash = "hash", FamilyId = "family1", UserId = "1", ExpiresUtc = _timeProvider.GetUtcNow().AddDays(1) };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        _userStore.Setup(s => s.FindByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokens = new AuthTokens("access2", _timeProvider.GetUtcNow().AddMinutes(15), "refresh2", _timeProvider.GetUtcNow().AddDays(30));
        _tokenIssuanceCoordinator.Setup(c => c.RotateAsync(user, "family1", "hash", It.IsAny<CancellationToken>())).ReturnsAsync(tokens);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.Succeeded, result.Outcome);
        Assert.Same(tokens, result.Tokens);
    }

    [Fact]
    public async Task RefreshAsync_RevokesFamily_AndReturnsReused_WhenRotationLosesRace()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord { TokenHash = "hash", FamilyId = "family1", UserId = "1", ExpiresUtc = _timeProvider.GetUtcNow().AddDays(1) };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);
        var user = new AuthUserRecord { Id = "1", Email = "a@b.com" };
        _userStore.Setup(s => s.FindByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenIssuanceCoordinator.Setup(c => c.RotateAsync(user, "family1", "hash", It.IsAny<CancellationToken>())).ReturnsAsync((AuthTokens?)null);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.Reused, result.Outcome);
        _refreshTokenStore.Verify(s => s.RevokeFamilyAsync("family1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsInvalidToken_WhenUserNoLongerExists()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord { TokenHash = "hash", FamilyId = "family1", UserId = "1", ExpiresUtc = _timeProvider.GetUtcNow().AddDays(1) };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _userStore.Setup(s => s.FindByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUserRecord?)null);

        var service = CreateService();
        var result = await service.RefreshAsync("token", CancellationToken.None);

        Assert.Equal(RefreshOutcome.InvalidToken, result.Outcome);
    }

    [Fact]
    public async Task LogoutAsync_RevokesFamily_WhenTokenExists()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var record = new RefreshTokenRecord { TokenHash = "hash", FamilyId = "family1", UserId = "1", ExpiresUtc = _timeProvider.GetUtcNow().AddDays(1) };
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var service = CreateService();
        var result = await service.LogoutAsync("token", CancellationToken.None);

        Assert.Same(LogoutResult.Succeeded, result);
        _refreshTokenStore.Verify(s => s.RevokeFamilyAsync("family1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_Succeeds_EvenWhenTokenDoesNotExist()
    {
        _secretHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenStore.Setup(s => s.FindByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync((RefreshTokenRecord?)null);

        var service = CreateService();
        var result = await service.LogoutAsync("token", CancellationToken.None);

        Assert.Same(LogoutResult.Succeeded, result);
        _refreshTokenStore.Verify(s => s.RevokeFamilyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
