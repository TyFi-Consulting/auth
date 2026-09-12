# Per-tenant setup: Email OTP + custom Maileroo sender

Steps to wire a new Entra External ID tenant to a project-hosted `OnOtpSend` endpoint so OTP
emails are sent via Maileroo instead of Microsoft's default provider. Repeat once per tenant
(therapy-scheduling-manager, tab-cloud, stock-tracker, workout-app).

> **Architecture (2026-09-10):** each project's own Function app hosts its tenant's OTP endpoint
> via the `TyFi.Auth.EntraExternalId` package. The Target URL in step 2 is therefore that project's
> Function app, and the configuration below applies to that app's settings — one tenant per app,
> so there is no tenant list/dictionary to index into.

## Prerequisites

- The tenant exists (`scripts/setup-tenant.sh`, recorded in the [README](../README.md#tenants)).
- The project's Function app hosts the `OnOtpSend` endpoint (via `TyFi.Auth.EntraExternalId`) and is
  reachable over HTTPS.

## 1. Enable the Email One-Time Passcode sign-up/sign-in user flow

In the [Entra admin center](https://entra.microsoft.com) for the tenant: **External Identities** →
**User flows** → create/edit a sign-up and sign-in flow → under **Identity providers**, enable
**Email one-time passcode**.

## 2. Register the custom authentication extension (Azure portal)

1. **Microsoft Entra ID** → **Enterprise applications** → **Custom authentication extensions** →
   **Create a custom extension**.
2. Event type: **EmailOtpSend**.
3. Endpoint configuration: **Target URL** = the Function's `OnOtpSend` URL (from the Function App's
   **Get Function Url**, using the Function-level key — the code path is anonymous at the host
   level because authentication is enforced in-code via bearer-token validation, not a Function key).
4. API Authentication: **Create new app registration** (e.g. "Azure Functions authentication events
   API"). Record its **Application (client) ID** — this is the `Audience` value below.
5. Applications tab: associate with the app(s) that use Email OTP sign-in for this tenant (typically
   the tenant's single SPA app registration), or apply tenant-wide.
6. After creation, open the generated app registration → **API permissions** → **Grant admin
   consent**. This lets Entra call the Function using `client_credentials`.

## 3. Configuration

Add these settings to the project's Function App settings (or `local.settings.json` locally). Two
independent sections — auth (who's allowed to call us) and mail (how we send):

```jsonc
{
  "Auth:EntraOtpSend:Issuer": "https://<tenant-name>.ciamlogin.com/<tenant-guid>/v2.0",
  "Auth:EntraOtpSend:Audience": "<custom-extension-app-client-id-from-step-2>",

  "Maileroo:ApiKey": "<maileroo-sending-key>",
  "Maileroo:FromAddress": "noreply@<project-domain>",
  "Maileroo:FromDisplayName": "<Project Display Name>",
  "Maileroo:Subject": "Your verification code"
}
```

The `Audience` claim check, `Issuer` claim check, and the fixed Microsoft caller-client-id check
(`azp`/`appid` == `99045fe1-7639-4a75-9d4a-577b6ca3810f`) are all enforced by
[`EntraOtpSendCallbackValidator`](../src/TyFi.Auth.EntraExternalId/EntraOtpSendCallbackValidator.cs)
before any email is sent — an invalid or wrong caller is rejected with 401 and no Maileroo call is
made. See [INTEGRATION.md](INTEGRATION.md) for the DI registration and the required trigger stub.

## 4. Fallback behavior on error (optional)

By default, if the Function errors, Entra does **not** send an OTP at all (fails closed). To fall
back to Microsoft's own provider on error instead, patch the listener via Graph:

```http
PATCH https://graph.microsoft.com/beta/identity/authenticationEventListeners/{listenerId}
Content-type: application/json

{
  "@odata.type": "#microsoft.graph.onEmailOtpSendListener",
  "handler": {
    "@odata.type": "#microsoft.graph.onOtpSendCustomExtensionHandler",
    "configuration": { "behaviorOnError": { "@odata.type": "#microsoft.graph.fallbackToMicrosoftProviderOnError" } }
  }
}
```

## 5. Test

Open a private browser window and sign in through the tenant's authorize endpoint using an Email
OTP account; confirm the code arrives from the project's own Maileroo-verified domain rather than a
Microsoft default sender address.