namespace TyFi.Auth.OtpMailer.Auth;

public sealed class OtpMailerAuthOptions
{
    public const string SectionName = "OtpMailerAuth";

    public List<TenantAuthOption> Tenants { get; init; } = [];
}
