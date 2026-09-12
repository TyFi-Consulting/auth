namespace TyFi.Auth.EntraExternalId;

/// <summary>
/// Options for validating the Entra <c>OnOtpSend</c> custom authentication extension callback for a
/// single tenant. Leaving <see cref="Issuer"/> or <see cref="Audience"/> empty disables the endpoint
/// (fails closed — every callback is rejected) rather than silently accepting unauthenticated calls.
/// </summary>
public sealed class EntraOtpSendOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "Auth:EntraOtpSend";

    /// <summary>The expected `iss` claim: "https://{tenantId}.ciamlogin.com/{tenantId}/v2.0".</summary>
    public string? Issuer { get; set; }

    /// <summary>The expected `aud` claim: this tenant's "Azure Functions authentication events API" app registration's client ID.</summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Upper bound on how long the handler will wait for the email provider before responding.
    /// Entra enforces a hard ~2-second timeout on the callback response (error 1003005
    /// CustomExtensionTimedOut); this must stay comfortably under that so a slow provider fails
    /// fast into a 502, letting the tenant's `fallbackToMicrosoftProviderOnError` setting degrade
    /// gracefully instead of the whole sign-in failing.
    /// </summary>
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>True once both <see cref="Issuer"/> and <see cref="Audience"/> are set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience);
}
