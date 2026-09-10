using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TyFi.Auth.Abstractions;

namespace TyFi.Auth.Jwt;

/// <summary>DI registration for <see cref="JwtBearerTokenAuthenticator"/>.</summary>
public static class JwtServiceCollectionExtensions
{
    /// <summary>Registers a provider-agnostic <see cref="IBearerTokenAuthenticator"/> backed by OIDC/JWT validation.</summary>
    public static IServiceCollection AddJwtBearerTokenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtBearerTokenAuthenticatorOptions>(configuration.GetSection(JwtBearerTokenAuthenticatorOptions.SectionName));
        // TryAdd: the JWKS cache is also shared by TyFi.Auth.EntraExternalId's OnOtpSend validator
        // when both are registered in the same app, regardless of call order.
        services.TryAddSingleton<IAuthorizationHeaderParser, AuthorizationHeaderParser>();
        services.TryAddSingleton<IOpenIdConnectConfigurationCache, OpenIdConnectConfigurationCache>();
        services.AddSingleton<IBearerTokenAuthenticator, JwtBearerTokenAuthenticator>();
        return services;
    }
}
