using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace TyFi.Auth.OtpMailer.Auth;

/// <summary>Caches per-issuer OIDC discovery documents/JWKS, refreshing on demand when a key is unrecognized.</summary>
public interface IOpenIdConnectConfigurationCache
{
    /// <summary>Gets the cached OIDC configuration for the given issuer, fetching and caching it on first use.</summary>
    /// <param name="issuer">The issuer whose `/.well-known/openid-configuration` document should be retrieved.</param>
    /// <param name="cancellationToken">Cancellation token for the retrieval operation.</param>
    Task<OpenIdConnectConfiguration> GetConfigurationAsync(string issuer, CancellationToken cancellationToken);

    /// <summary>Forces the next <see cref="GetConfigurationAsync"/> call for this issuer to re-fetch its JWKS.</summary>
    /// <param name="issuer">The issuer whose cached configuration should be invalidated.</param>
    void RequestRefresh(string issuer);
}
