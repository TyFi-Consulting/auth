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

        // ValidateOnStart fails the host at startup with a clear message when a signing/hashing key is
        // missing or too short, instead of a collaborator throwing on the first real request.
        static bool isAtLeast32Bytes(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String(key).Length >= 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        services.AddOptions<AuthIdentityOptions>()
            .Bind(configuration.GetSection(AuthIdentityOptions.SectionName))
            .Validate(o => isAtLeast32Bytes(o.SigningKey), "Auth:Identity:SigningKey must be a base64 value decoding to at least 32 bytes.")
            .Validate(o => isAtLeast32Bytes(o.HashingKey), "Auth:Identity:HashingKey must be a base64 value decoding to at least 32 bytes.")
            .ValidateOnStart();

        // Shares TyFi.Auth.Jwt's own claim-mapping section rather than a separate one: the issuer
        // (here) and the validator (TyFi.Auth.Jwt) must agree on claim-type names, or issued tokens
        // can carry claims the validator doesn't recognize and fail authorization after validation.
        services.Configure<AuthClaimMappingOptions>(configuration.GetSection("Auth:Jwt:ClaimMapping"));
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

    private static bool IsAtLeast32Bytes(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(key).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
