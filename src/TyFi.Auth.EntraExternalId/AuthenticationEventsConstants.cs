namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Fixed client ID Microsoft Entra always uses as the caller (`azp`/`appid` claim) when invoking
/// a custom authentication extension. This is a Microsoft-published, tenant-agnostic constant —
/// identical for every Entra tenant in the world, not specific to this project or any customer's
/// tenant — so it is safe to hardcode in this public repository. See
/// https://learn.microsoft.com/entra/identity-platform/custom-extension-overview#protect-your-rest-api.
/// </summary>
public static class AuthenticationEventsConstants
{
    /// <summary>The fixed Microsoft Authentication Events client ID.</summary>
    public const string MicrosoftAuthenticationEventsClientId = "99045fe1-7639-4a75-9d4a-577b6ca3810f";
}
