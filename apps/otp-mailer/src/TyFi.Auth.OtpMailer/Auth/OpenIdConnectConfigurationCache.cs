using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace TyFi.Auth.OtpMailer.Auth;

public sealed class OpenIdConnectConfigurationCache : IOpenIdConnectConfigurationCache
{
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _managers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string issuer, CancellationToken cancellationToken)
    {
        var manager = _managers.GetOrAdd(issuer, static iss => new ConfigurationManager<OpenIdConnectConfiguration>(
            iss.TrimEnd('/') + "/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true }));
        return await manager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RequestRefresh(string issuer)
    {
        if (_managers.TryGetValue(issuer, out var manager))
        {
            manager.RequestRefresh();
        }
    }
}
