using Microsoft.Extensions.DependencyInjection;

namespace TyFi.Auth.OtpMailer.Handling;

public static class HandlingServiceCollectionExtensions
{
    public static IServiceCollection AddOnOtpSendHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IOnOtpSendRequestHandler, OnOtpSendRequestHandler>();
        return services;
    }
}
