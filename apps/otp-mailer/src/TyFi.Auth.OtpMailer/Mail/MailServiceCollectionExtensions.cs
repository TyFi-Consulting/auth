using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TyFi.Auth.OtpMailer.Mail;

public static class MailServiceCollectionExtensions
{
    public static IServiceCollection AddMailerooEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MailerooOptions>(configuration.GetSection(MailerooOptions.SectionName));
        services.AddSingleton<IOtpEmailBodyRenderer, OtpEmailBodyRenderer>();
        services.AddHttpClient(MailerooOtpEmailSender.HttpClientName);
        services.AddSingleton<IOtpEmailSender, MailerooOtpEmailSender>();
        return services;
    }
}
