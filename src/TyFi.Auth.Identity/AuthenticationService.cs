using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <inheritdoc cref="IAuthenticationService" />
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserAccountStore _userStore;
    private readonly IEmailSender _emailSender;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IOtpGenerator _otpGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly ITokenIssuanceCoordinator _tokenIssuanceCoordinator;
    private readonly IEmailNormalizer _emailNormalizer;
    private readonly IOptions<AuthIdentityOptions> _options;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service.</summary>
    public AuthenticationService(
        IUserAccountStore userStore,
        IEmailSender emailSender,
        IRefreshTokenStore refreshTokenStore,
        IOtpGenerator otpGenerator,
        ISecretHasher secretHasher,
        ITokenIssuanceCoordinator tokenIssuanceCoordinator,
        IEmailNormalizer emailNormalizer,
        IOptions<AuthIdentityOptions> options,
        ILogger<AuthenticationService> logger,
        TimeProvider timeProvider)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _refreshTokenStore = refreshTokenStore ?? throw new ArgumentNullException(nameof(refreshTokenStore));
        _otpGenerator = otpGenerator ?? throw new ArgumentNullException(nameof(otpGenerator));
        _secretHasher = secretHasher ?? throw new ArgumentNullException(nameof(secretHasher));
        _tokenIssuanceCoordinator = tokenIssuanceCoordinator ?? throw new ArgumentNullException(nameof(tokenIssuanceCoordinator));
        _emailNormalizer = emailNormalizer ?? throw new ArgumentNullException(nameof(emailNormalizer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<RegisterResult> RegisterAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = _emailNormalizer.Normalize(email);
        var existing = await _userStore.FindByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return RegisterResult.AlreadyRegistered();
        }

        var now = _timeProvider.GetUtcNow();
        var options = _options.Value;
        var code = _otpGenerator.Generate(options.OtpCodeLength);

        var user = new AuthUserRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = normalizedEmail,
            PendingCodeHash = _secretHasher.Hash(code),
            PendingCodeExpiresUtc = now.Add(options.OtpCodeLifetime),
            PendingCodeAttempts = 0,
            CreatedUtc = now,
        };

        await _userStore.CreateAsync(user, cancellationToken).ConfigureAwait(false);
        await _emailSender.SendLoginCodeAsync(normalizedEmail, code, options.OtpCodeLifetime, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Registration started for a new account.");
        return RegisterResult.Started();
    }

    /// <inheritdoc />
    public async Task<RequestLoginCodeResult> RequestLoginCodeAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = _emailNormalizer.Normalize(email);
        var user = await _userStore.FindByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        if (user is not null && (user.LockoutEndUtc is null || user.LockoutEndUtc <= now))
        {
            var options = _options.Value;
            var code = _otpGenerator.Generate(options.OtpCodeLength);
            user.PendingCodeHash = _secretHasher.Hash(code);
            user.PendingCodeExpiresUtc = now.Add(options.OtpCodeLifetime);
            user.PendingCodeAttempts = 0;

            await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await _emailSender.SendLoginCodeAsync(normalizedEmail, code, options.OtpCodeLifetime, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Login code sent.");
        }

        // Always the same result -- an unregistered email and a locked-out account are indistinguishable
        // to the caller, so this endpoint can never be used to enumerate accounts.
        return RequestLoginCodeResult.Sent;
    }

    /// <inheritdoc />
    public async Task<VerifyCodeResult> VerifyCodeAsync(string email, string code, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedEmail = _emailNormalizer.Normalize(email);
        var user = await _userStore.FindByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        if (user is null)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.InvalidCode);
        }

        if (user.LockoutEndUtc is not null && user.LockoutEndUtc > now)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.AccountLocked);
        }

        if (user.PendingCodeHash is null || user.PendingCodeExpiresUtc is null || user.PendingCodeExpiresUtc <= now)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.Expired);
        }

        var options = _options.Value;
        if (user.PendingCodeAttempts >= options.OtpMaxAttempts)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.TooManyAttempts);
        }

        if (!_secretHasher.Verify(code, user.PendingCodeHash))
        {
            user.PendingCodeAttempts++;
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= options.MaxFailedLoginAttemptsBeforeLockout)
            {
                user.LockoutEndUtc = now.Add(options.LockoutDuration);
                _logger.LogWarning("Account locked out after repeated failed login attempts.");
            }

            await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            return VerifyCodeResult.Failed(VerifyCodeOutcome.InvalidCode);
        }

        user.PendingCodeHash = null;
        user.PendingCodeExpiresUtc = null;
        user.PendingCodeAttempts = 0;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginUtc = now;
        await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        var tokens = await _tokenIssuanceCoordinator.IssueAsync(user, existingFamilyId: null, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Login succeeded.");
        return VerifyCodeResult.Succeeded(tokens);
    }

    /// <inheritdoc />
    public async Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenHash = _secretHasher.Hash(refreshToken);
        var record = await _refreshTokenStore.FindByTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return RefreshResult.Failed(RefreshOutcome.InvalidToken);
        }

        if (record.RevokedUtc is not null || record.RotatedUtc is not null)
        {
            // A revoked or already-rotated token was presented again -- treat as theft and burn the
            // whole family so a stolen refresh token can't keep producing new sessions.
            await _refreshTokenStore.RevokeFamilyAsync(record.FamilyId, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Refresh token reuse detected; token family revoked.");
            return RefreshResult.Failed(RefreshOutcome.Reused);
        }

        var now = _timeProvider.GetUtcNow();
        if (record.ExpiresUtc <= now)
        {
            return RefreshResult.Failed(RefreshOutcome.Expired);
        }

        var user = await _userStore.FindByIdAsync(record.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return RefreshResult.Failed(RefreshOutcome.InvalidToken);
        }

        await _refreshTokenStore.MarkRotatedAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        var tokens = await _tokenIssuanceCoordinator.IssueAsync(user, record.FamilyId, cancellationToken).ConfigureAwait(false);
        return RefreshResult.Succeeded(tokens);
    }

    /// <inheritdoc />
    public async Task<LogoutResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenHash = _secretHasher.Hash(refreshToken);
        var record = await _refreshTokenStore.FindByTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);
        if (record is not null)
        {
            await _refreshTokenStore.RevokeFamilyAsync(record.FamilyId, cancellationToken).ConfigureAwait(false);
        }

        // Always succeeds -- logout never reveals whether the supplied token was valid.
        return LogoutResult.Succeeded;
    }
}
