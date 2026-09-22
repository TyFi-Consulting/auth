# Integration notes

## Package map

| Package | Purpose |
|---|---|
| `TyFi.Auth.Abstractions` | `AuthenticatedUser`, `IBearerTokenAuthenticator`, claim-mapping options. No OIDC or hosting code. |
| `TyFi.Auth.Jwt` | Provider-agnostic OIDC/JWT validation (cached JWKS, RS256 by default; static-key HS256 mode for self-issued tokens). Implements `IBearerTokenAuthenticator`. |
| `TyFi.Auth.Functions.Worker` | Isolated-worker middleware for the **built-in** HTTP model (`HttpRequestData`/`HttpResponseData`). |
| `TyFi.Auth.Functions.AspNetCore` | Isolated-worker middleware for the **ASP.NET Core integration** HTTP model (`HttpRequest`/`IActionResult`, `ConfigureFunctionsWebApplication()`). |
| `TyFi.Auth.Identity.Abstractions` | Data contracts and extension points (`IUserAccountStore`, `IEmailSender`, `IRefreshTokenStore`, `IAuthenticationService`, ...) for the self-hosted identity engine. |
| `TyFi.Auth.Identity` | Self-hosted, passwordless (email one-time-code) authentication: registration, login, refresh-token rotation, lockout. No third-party identity provider required. |
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

For an endpoint that needs a signed-in caller but no specific role (e.g. a "who am I" check that
must work even for a caller with no role assigned yet), use `[AuthorizeAuthenticated]` instead of
`[Authorize(Policy = "...")]`. If a function carries both, the explicit policy wins and
`[AuthorizeAuthenticated]` is ignored.

`[Authorize(Policy = "...")]`'s argument must be a compile-time constant, but many consumers'
actual role names are runtime configuration (Terraform, App Configuration, etc.), not literals.
Register your own `IAuthorizationRoleNameResolver` (after `AddTyFiAuthFunctionsAspNetCore()` /
`AddTyFiAuthFunctionsWorker()`, so it overrides the pass-through default) to translate a policy
name to the actual configured role string before the middleware compares it against the caller's
roles:

```csharp
public sealed class ConfiguredRoleNameResolver(IOptions<MyRoleNamesOptions> roleNames) : IAuthorizationRoleNameResolver
{
    public string ResolveRoleName(string policy) => policy switch
    {
        "Admin" => roleNames.Value.Admin,
        "Therapist" => roleNames.Value.Therapist,
        _ => policy,
    };
}
```

## Self-hosted identity (no third-party provider)

```bash
dotnet add package TyFi.Auth.Identity
```

`Program.cs` -- implement `IUserAccountStore`, `IEmailSender`, `IRefreshTokenStore` against your own
storage/mail sender, then:

```csharp
builder.Services
    .AddAuthentication<YourUserAccountStore, YourEmailSender, YourRefreshTokenStore>(builder.Configuration)
    .AddRoles("Admin", "User")
    .AddPermissions(
        permissions: ["WidgetRead", "WidgetWrite"],
        assignments: new Dictionary<string, string[]> { ["Admin"] = ["WidgetRead", "WidgetWrite"], ["User"] = ["WidgetRead"] });
```

Config -- the issuer (`TyFi.Auth.Identity`) and validator (`TyFi.Auth.Jwt`) must agree on the signing
key and claim-mapping section, since a self-issued token has no discovery document/JWKS endpoint:

```jsonc
{
  "Auth:Identity:Issuer": "https://your-app",
  "Auth:Identity:Audience": "api://your-app-id",
  "Auth:Identity:SigningKey": "<base64, 32+ bytes, HMAC-SHA256>",
  "Auth:Identity:HashingKey": "<base64, 32+ bytes, HMAC-SHA256, different key than SigningKey>",

  "Auth:Jwt:Issuer": "https://your-app",
  "Auth:Jwt:Audience": "api://your-app-id",
  "Auth:Jwt:SigningKey": "<the same value as Auth:Identity:SigningKey above>",
  "Auth:Jwt:ValidAlgorithms:0": "HS256"
}
```

Call `IAuthenticationService` from your own login/register/refresh/logout endpoints -- the library
never touches your request/response shape, so your endpoints own deserialization and the response
body entirely. See [`TyFi.Auth.Identity`'s README](../src/TyFi.Auth.Identity/README.md) for a full
endpoint example and design notes (passwordless, enumeration-safe, atomic refresh rotation).

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
