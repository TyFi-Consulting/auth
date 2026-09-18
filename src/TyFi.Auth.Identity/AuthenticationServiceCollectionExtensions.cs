using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TyFi.Auth.Abstractions;
using TyFi.Auth.Identity.Abstractions;

namespace TyFi.Auth.Identity;

/// <summary>DI registration for <see cref="AuthenticationService"/> and its collaborators.</summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers self-hosted, passwordless (email one-time-code) authentication. <typeparamref name="TUserAccountStore"/>
    /// and <typeparamref name="TEmailSender"/> and <typeparamref name="TRefreshTokenStore"/> are supplied
    /// by the consuming project; all three are required because none of them are safely defaultable
    /// (a default in-memory store would silently break on Functions cold starts / scale-out).
    /// </summary>
    public static TyFiAuthenticationBuilder AddAuthentication<TUserAccountStore, TEmailSender, TRefreshTokenStore>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TUserAccountStore : class, IUserAccountStore
        where TEmailSender : class, IEmailSender
        where TRefreshTokenStore : class, IRefreshTokenStore
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AuthIdentityOptions>(configuration.GetSection(AuthIdentityOptions.SectionName));
        services.Configure<AuthClaimMappingOptions>(configuration.GetSection(AuthClaimMappingOptions.SectionName));
        services.TryAddSingleton<IClaimsEnricher, NoOpClaimsEnricher>();

        services.AddScoped<IUserAccountStore, TUserAccountStore>();
        services.AddScoped<IEmailSender, TEmailSender>();
        services.AddScoped<IRefreshTokenStore, TRefreshTokenStore>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IOtpGenerator, OtpGenerator>();
        services.TryAddSingleton<ISecretHasher, HmacSecretHasher>();
        services.TryAddSingleton<IEmailNormalizer, EmailNormalizer>();
        services.TryAddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.TryAddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.TryAddScoped<ITokenIssuanceCoordinator, TokenIssuanceCoordinator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return new TyFiAuthenticationBuilder(services);
    }
}
