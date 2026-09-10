using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TyFi.Auth.OtpMailer.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddOtpMailerTokenValidation(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OtpMailerAuthOptions>(configuration.GetSection(OtpMailerAuthOptions.SectionName));
        services.AddSingleton<IOpenIdConnectConfigurationCache, OpenIdConnectConfigurationCache>();
        services.AddSingleton<IBearerTokenValidator, EntraCustomExtensionTokenValidator>();
        return services;
    }
}
