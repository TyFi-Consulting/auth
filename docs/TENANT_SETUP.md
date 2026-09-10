# Per-tenant setup: Email OTP + custom Maileroo sender

Steps to wire a new Entra External ID tenant to the `TyFi.Auth.OtpMailer` Azure Function so OTP
emails are sent via Maileroo instead of Microsoft's default provider. Repeat once per tenant
(therapy-scheduling-manager, tab-cloud, stock-tracker, workout-app).

## Prerequisites

- The tenant exists (`scripts/setup-tenant.sh`, recorded in the [README](../README.md#tenants)).
- `apps/otp-mailer` is deployed to an Azure Function App reachable over HTTPS (one Function App can
  serve all tenants; see [Configuration](#configuration) for the per-tenant settings shape).

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

Add an entry per tenant to the Function App's settings (or `local.settings.json` locally). Two
independent sections — auth (who's allowed to call us) and mail (how we send for that tenant):

```jsonc
{
  "OtpMailerAuth:Tenants:0:TenantId": "<tenant-guid>",
  "OtpMailerAuth:Tenants:0:Issuer": "https://<tenant-name>.ciamlogin.com/<tenant-guid>/v2.0",
  "OtpMailerAuth:Tenants:0:Audience": "<custom-extension-app-client-id-from-step-2>",

  "Maileroo:Tenants:<tenant-guid>:ApiKey": "<maileroo-sending-key>",
  "Maileroo:Tenants:<tenant-guid>:FromAddress": "noreply@<project-domain>",
  "Maileroo:Tenants:<tenant-guid>:FromDisplayName": "<Project Display Name>",
  "Maileroo:Tenants:<tenant-guid>:Subject": "Your verification code"
}
```

The `Audience` claim check, `Issuer` claim check, and the fixed Microsoft caller-client-id check
(`azp`/`appid` == `99045fe1-7639-4a75-9d4a-577b6ca3810f`) are all enforced by
[`EntraCustomExtensionTokenValidator`](../apps/otp-mailer/src/TyFi.Auth.OtpMailer/Auth/EntraCustomExtensionTokenValidator.cs)
before any email is sent — an unrecognized tenant or wrong caller is rejected with 401 and no
Maileroo call is made.

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

## Per-tenant log

| Tenant | Custom extension app (Audience) client ID | Notes |
|---|---|---|
| therapy-scheduling-manager (`tyfischeduler`) | _pending — steps 1–2 not yet performed_ | |
