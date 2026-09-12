# Integration notes

## Package map

| Package | Purpose |
|---|---|
| `TyFi.Auth.Abstractions` | `AuthenticatedUser`, `IBearerTokenAuthenticator`, claim-mapping options. No OIDC or hosting code. |
| `TyFi.Auth.Jwt` | Provider-agnostic OIDC/JWT validation (cached JWKS, RS256-only). Implements `IBearerTokenAuthenticator`. |
| `TyFi.Auth.Functions.Worker` | Isolated-worker middleware for the **built-in** HTTP model (`HttpRequestData`/`HttpResponseData`). |
| `TyFi.Auth.Functions.AspNetCore` | Isolated-worker middleware for the **ASP.NET Core integration** HTTP model (`HttpRequest`/`IActionResult`, `ConfigureFunctionsWebApplication()`). |
| `TyFi.Auth.EntraExternalId` | The `OnOtpSend` custom authentication extension endpoint: callback validation + Maileroo sender. Entra-specific by design. |

Pick **one** of the two host packages, matching how your Function app calls
`builder.ConfigureFunctionsWebApplication()` (→ `Functions.AspNetCore`) or
`builder.ConfigureFunctionsWorkerDefaults()` (→ `Functions.Worker`).

## Securing your API (both apps that need it)

```bash
dotnet add package TyFi.Auth.Jwt
dotnet add package TyFi.Auth.Functions.AspNetCore   # or TyFi.Auth.Functions.Worker
```

`Program.cs`:

```csharp
builder.Services.AddJwtBearerTokenAuthentication(builder.Configuration);
builder.Services.AddTyFiAuthFunctionsAspNetCore();   // or AddTyFiAuthFunctionsWorker()
builder.UseMiddleware<AuthorizationAspNetCoreMiddleware>();   // or AuthorizationWorkerMiddleware
```

`appsettings.json` / app settings (config-only provider swap — Auth0 today, Entra tomorrow):

```jsonc
{
  "Auth:Jwt:Issuer": "https://{tenant}.ciamlogin.com/{tenantId}/v2.0",
  "Auth:Jwt:Audience": "api://your-app-id",
  "Auth:Jwt:ClaimMapping:RolesClaimType": "roles"   // "permissions" for Auth0, or a custom URI claim
}
```

Leaving `Issuer`/`Audience` empty disables authentication entirely (every request returns
401) — a deliberate fail-closed default for unconfigured environments, not a bypass.

Mark every HTTP-triggered function with `[Authorize("policy-name")]` or `[AllowAnonymous]`
(from `Microsoft.AspNetCore.Authorization`). A function with **neither** attribute fails
closed (500 "Authorization policy missing") rather than silently allowing anonymous access.
Read the caller from `context.GetAuthenticatedUser()` (Functions.Worker) or
`httpContext.GetAuthenticatedUser()` (Functions.AspNetCore, via
`context.GetHttpContext()`).

## Hosting the OnOtpSend endpoint (one app, its own tenant)

```bash
dotnet add package TyFi.Auth.EntraExternalId
```

`Program.cs`:

```csharp
builder.Services.AddEntraExternalIdOtpMailer(builder.Configuration);
```

Config:

```jsonc
{
  "Auth:EntraOtpSend:Issuer": "https://{tenantId}.ciamlogin.com/{tenantId}/v2.0",
  "Auth:EntraOtpSend:Audience": "<custom-extension-app-registration-client-id>",

  "Maileroo:ApiKey": "<sending-key>",
  "Maileroo:FromAddress": "noreply@your-domain.com",
  "Maileroo:FromDisplayName": "Your App",
  "Maileroo:Subject": "Your verification code"
}
```

Register your own email copy/branding (optional — a generic default is used otherwise),
**before** calling `AddEntraExternalIdOtpMailer` so it wins over the default registration:

```csharp
builder.Services.AddSingleton<IOtpEmailBodyRenderer, YourAppOtpEmailBodyRenderer>();
builder.Services.AddEntraExternalIdOtpMailer(builder.Configuration);
```

**Add the trigger stub.** Azure Functions isolated-worker function discovery only scans the
*consuming app's own* compiled assembly, not referenced library assemblies — so the
`[Function]`-attributed method must live in your app, even though it does no real work:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using TyFi.Auth.EntraExternalId;

public sealed class OnOtpSend(IOnOtpSendRequestHandler handler)
{
    [Function("OnOtpSend")]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        CancellationToken cancellationToken)
        => handler.HandleHttpAsync(req, cancellationToken);
}
```

`AuthorizationLevel.Anonymous` is correct here: the callback is authenticated in code
(issuer/audience/caller checks), not via a Function key, because Entra sends a bearer JWT.

See [TENANT_SETUP.md](TENANT_SETUP.md) for registering the custom authentication extension
against this endpoint's URL, and enable `fallbackToMicrosoftProviderOnError` on the tenant's
listener — the handler enforces its own response-time budget
(`Auth:EntraOtpSend:ResponseTimeout`, default 1.5s, comfortably under Entra's ~2s callback
timeout) so a slow/unavailable Maileroo degrades to Microsoft's default email instead of
failing the sign-in.
