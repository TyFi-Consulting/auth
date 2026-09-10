namespace TyFi.Auth.OtpMailer.Auth;

/// <summary>Per-tenant issuer/audience pair used to validate the OnOtpSend callback token.</summary>
public sealed class TenantAuthOption
{
    public required string TenantId { get; init; }

    /// <summary>Expected `iss` claim, e.g. https://{domain}.ciamlogin.com/{tenantId}/v2.0.</summary>
    public required string Issuer { get; init; }

    /// <summary>Expected `aud` claim: the client ID of this tenant's "Azure Functions authentication events API" app registration.</summary>
    public required string Audience { get; init; }
}
