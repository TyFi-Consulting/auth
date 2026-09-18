# TyFi.Auth.Identity

Self-hosted, passwordless (email one-time-code) authentication: registration, login, refresh-token
rotation with reuse detection, account lockout, and configurable role/permission-to-claims expansion.
No external identity provider required -- your project supplies where users and refresh tokens are
stored and how emails are sent; this package supplies the orchestration and token issuance.

Access tokens are signed JWTs validated by the existing provider-agnostic `TyFi.Auth.Jwt` package with
**no changes** -- the claim types you configure here (via `Auth:ClaimMapping` / `AuthClaimMappingOptions`,
shared with `TyFi.Auth.Abstractions`) are exactly what `TyFi.Auth.Jwt` already expects.

## Install

```bash
dotnet add package TyFi.Auth.Identity
```

## Usage

Implement the three storage/delivery seams against your own data store and email sender:

```csharp
public sealed class SqlUserAccountStore : IUserAccountStore { /* ... */ }
public sealed class MailerooEmailSender : IEmailSender { /* ... */ }
public sealed class SqlRefreshTokenStore : IRefreshTokenStore { /* ... */ }
```

Then register everything in `Program.cs`:

```csharp
builder.Services
    .AddAuthentication<SqlUserAccountStore, MailerooEmailSender, SqlRefreshTokenStore>(builder.Configuration)
    .AddRoles("Admin", "User")
    .AddPermissions(
        permissions: ["WidgetRead", "WidgetWrite"],
        assignments: new Dictionary<string, string[]>
        {
            ["Admin"] = ["WidgetRead", "WidgetWrite"],
            ["User"] = ["WidgetRead"],
        });
```

`appsettings.json` (signing/hashing keys should come from Key Vault or user-secrets in production, never
source control):

```jsonc
{
  "Auth:Identity:Issuer": "https://your-app",
  "Auth:Identity:Audience": "api://your-app-id",
  "Auth:Identity:SigningKey": "<base64, 32+ bytes, HMAC-SHA256>",
  "Auth:Identity:HashingKey": "<base64, 32+ bytes, HMAC-SHA256, different key than SigningKey>"
}
```

Call `IAuthenticationService` from your Function app's login/register/logout endpoints. The library never
touches your request/response shape -- your endpoint owns deserialization and the response body entirely;
`AuthOutcomeStatusCodes` just saves you from re-deriving the same outcome-to-HTTP-status mapping in every
project:

```csharp
public sealed record RegisterRequest(string Email); // your own request shape, however you want it

[Function("Register")]
public async Task<HttpResponseData> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, CancellationToken ct)
{
    var body = await req.ReadFromJsonAsync<RegisterRequest>(ct);
    var result = await _auth.RegisterAsync(body!.Email, ct);

    var response = req.CreateResponse((HttpStatusCode)AuthOutcomeStatusCodes.ForRegister(result.Outcome));
    await response.WriteAsJsonAsync(new { outcome = result.Outcome.ToString() }, response.StatusCode, ct);
    return response;
}
```

Every failure mode (wrong code, expired token, locked-out account, reused refresh token, ...) is a value
on the returned result, never an exception -- your endpoint decides what status code and body to send.

## Design notes

- **No opinion on wire format.** The library takes plain parameters and returns plain result types; it
  never defines a request/response DTO or touches `HttpRequestData`/`HttpRequest`. Your project's endpoint
  fully owns deserialization and the response payload shape -- the only shared piece is
  `AuthOutcomeStatusCodes`, a pure outcome→status-code mapping with zero coupling to any HTTP type, and
  even that is just a convenience you're free to ignore.
- **Passwordless by design.** No password hashing, no password-reset flow, no credential-stuffing surface.
  A login is always "email a one-time code, verify it."
- **Refresh tokens are opaque and rotated.** Each use exchanges the token for a new one; presenting an
  already-rotated or revoked token is treated as theft and revokes the whole token family.
- **Roles are a catalog, permissions are derived.** `AddRoles`/`AddPermissions` declare the catalog once;
  a user's roles are expanded through the role→permission assignments at token-issuance time, so the
  access token always carries both.
- **No admin dashboard.** You get direct control of your own user table instead of a hosted vendor UI --
  see the repository README for the trade-offs this implies.

See the repository [README](../../README.md) for how this package relates to the others, and
[TyFi.Auth.Identity.Abstractions's README](../TyFi.Auth.Identity.Abstractions/README.md) for the full
interface/type reference.
