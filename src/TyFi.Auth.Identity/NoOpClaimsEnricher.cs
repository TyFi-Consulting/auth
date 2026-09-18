using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>Default <see cref="IClaimsEnricher"/> that adds no additional claims.</summary>
public sealed class NoOpClaimsEnricher : IClaimsEnricher
{
    /// <inheritdoc />
    public Task EnrichAsync(IList<System.Security.Claims.Claim> claims, AuthUserRecord user, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
