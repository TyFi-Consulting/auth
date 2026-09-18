namespace TyFi.Auth.Identity.Tests;

/// <summary>A controllable <see cref="TimeProvider"/> for deterministic expiry/lockout assertions.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow += delta;
}
