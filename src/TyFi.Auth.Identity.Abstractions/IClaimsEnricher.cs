using System.Security.Claims;

namespace TyFi.Auth.Identity.Abstractions;

/// <summary>
/// Optional extension point for adding project-specific claims to an access token beyond the built-in
/// subject/email/roles/permissions claims. Register a custom implementation via
/// <c>TyFiAuthenticationBuilder.WithClaimsEnricher&lt;T&gt;()</c>; the default is a no-op.
/// </summary>
public interface IClaimsEnricher
{
    /// <summary>Adds any additional claims for the given user to the mutable <paramref name="claims"/> list.</summary>
    Task EnrichAsync(IList<Claim> claims, AuthUserRecord user, CancellationToken cancellationToken);
}
