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
        if (existing is null)
        {
            var user = new AuthUserRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = normalizedEmail,
                CreatedUtc = _timeProvider.GetUtcNow(),
            };
            await _userStore.CreateAsync(user, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Registration created a new account.");
        }

        // Deliberately delegates to the same enumeration-safe code-issuing path as login: whether the
        // account was just created or already existed, the caller sees an identical result, so this
        // endpoint can never be used to distinguish a new registration from an existing account.
        await RequestLoginCodeAsync(email, cancellationToken).ConfigureAwait(false);
        return RegisterResult.Started;
    }

    /// <inheritdoc />
    public async Task<RequestLoginCodeResult> RequestLoginCodeAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = _emailNormalizer.Normalize(email);
        var user = await _userStore.FindByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var options = _options.Value;

        var lockedOut = user?.LockoutEndUtc is not null && user.LockoutEndUtc > now;
        var withinResendCooldown = user?.LastCodeSentUtc is not null
            && now < user.LastCodeSentUtc.Value.Add(options.MinimumCodeResendInterval);

        if (user is not null && !lockedOut && !withinResendCooldown)
        {
            var code = _otpGenerator.Generate(options.OtpCodeLength);
            user.PendingCodeHash = _secretHasher.Hash(code);
            user.PendingCodeExpiresUtc = now.Add(options.OtpCodeLifetime);
            user.PendingCodeAttempts = 0;
            user.LastCodeSentUtc = now;

            // A lost race here means a concurrent request already sent a code moments ago; skip
            // sending a second one rather than double-emailing the account.
            var updated = await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            if (updated)
            {
                await _emailSender.SendLoginCodeAsync(normalizedEmail, code, options.OtpCodeLifetime, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Login code sent.");
            }
        }

        // Always the same result -- an unregistered email, a locked-out account, and a
        // cooldown-throttled resend are all indistinguishable to the caller, so this endpoint can
        // never be used to enumerate accounts or to spam a real account's inbox.
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

        // No pending code at all is indistinguishable from an unknown account -- both are InvalidCode.
        // Only a code that once existed and has since timed out is Expired.
        if (user.PendingCodeHash is null)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.InvalidCode);
        }

        if (user.PendingCodeExpiresUtc is null || user.PendingCodeExpiresUtc <= now)
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

            // Best-effort: a lost race here means a concurrent request updated the counters first.
            // The guess was still wrong either way, so the outcome is the same regardless.
            await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            return VerifyCodeResult.Failed(VerifyCodeOutcome.InvalidCode);
        }

        user.PendingCodeHash = null;
        user.PendingCodeExpiresUtc = null;
        user.PendingCodeAttempts = 0;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginUtc = now;

        // Atomic consume: if this loses the race (a concurrent request already consumed the same
        // one-time code first), do not also issue a second session for a code that's no longer valid.
        var consumed = await _userStore.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        if (!consumed)
        {
            return VerifyCodeResult.Failed(VerifyCodeOutcome.InvalidCode);
        }

        var tokens = await _tokenIssuanceCoordinator.IssueAsync(user, cancellationToken).ConfigureAwait(false);
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

        var tokens = await _tokenIssuanceCoordinator.RotateAsync(user, record.FamilyId, tokenHash, cancellationToken).ConfigureAwait(false);
        if (tokens is null)
        {
            // Lost the race to rotate this token -- another concurrent request already rotated (or
            // revoked) it first. Two live presentations of the same refresh token is itself a theft
            // signal, so burn the whole family rather than silently ignoring it.
            await _refreshTokenStore.RevokeFamilyAsync(record.FamilyId, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Refresh token rotation race detected; token family revoked.");
            return RefreshResult.Failed(RefreshOutcome.Reused);
        }

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
