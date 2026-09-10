using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TyFi.Auth.Jwt;

namespace TyFi.Auth.EntraExternalId;

/// <summary>DI registration for the OnOtpSend endpoint building block.</summary>
public static class EntraExternalIdServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OnOtpSend callback validator, Maileroo email sender, and request handler.
    /// Register your own <see cref="IOtpEmailBodyRenderer"/> beforehand to supply your project's
    /// email copy/branding; otherwise <see cref="DefaultOtpEmailBodyRenderer"/> is used.
    /// </summary>
    public static IServiceCollection AddEntraExternalIdOtpMailer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<EntraOtpSendOptions>(configuration.GetSection(EntraOtpSendOptions.SectionName));
        services.Configure<MailerooOptions>(configuration.GetSection(MailerooOptions.SectionName));

        // TryAdd: shares the JWKS cache with TyFi.Auth.Jwt's own registration when an app also
        // validates its own API tokens, regardless of call order.
        services.TryAddSingleton<IOpenIdConnectConfigurationCache, OpenIdConnectConfigurationCache>();
        services.AddSingleton<IOnOtpSendCallbackValidator, EntraOtpSendCallbackValidator>();
        services.TryAddSingleton<IOtpEmailBodyRenderer, DefaultOtpEmailBodyRenderer>();
        services.AddHttpClient<IOtpEmailSender, MailerooOtpEmailSender>();
        services.AddSingleton<IOnOtpSendRequestHandler, OnOtpSendRequestHandler>();
        return services;
    }
}
