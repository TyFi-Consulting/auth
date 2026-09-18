# TyFi.Auth.Functions.AspNetCore

Azure Functions isolated-worker middleware for the **ASP.NET Core integration** HTTP model
(`HttpRequest`/`IActionResult`): authorizes incoming requests using whatever `IBearerTokenAuthenticator` is
registered (typically `TyFi.Auth.Jwt`), and fails closed (401/500) for any HTTP-triggered function missing
an explicit `[Authorize]`/`[AllowAnonymous]` attribute.

```bash
dotnet add package TyFi.Auth.Functions.AspNetCore
```

```csharp
builder.Services.AddJwtBearerTokenAuthentication(builder.Configuration);
builder.Services.AddTyFiAuthFunctionsAspNetCore();
builder.UseMiddleware<AuthorizationAspNetCoreMiddleware>();
```

Read the authenticated caller via `httpContext.GetAuthenticatedUser()` (obtained from
`context.GetHttpContext()`).

Use `TyFi.Auth.Functions.Worker` instead if your Function app calls
`builder.ConfigureFunctionsWorkerDefaults()`. See the repository
[README](../../README.md) and [docs/INTEGRATION.md](../../docs/INTEGRATION.md) for full details.
