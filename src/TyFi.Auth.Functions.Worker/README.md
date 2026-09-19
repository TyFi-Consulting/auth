# TyFi.Auth.Functions.Worker

Azure Functions isolated-worker middleware for the **built-in** HTTP model (`HttpRequestData`/
`HttpResponseData`): authorizes incoming requests using whatever `IBearerTokenAuthenticator` is registered
(typically `TyFi.Auth.Jwt`), and fails closed (401/500) for any HTTP-triggered function missing an explicit
`[Authorize]`/`[AllowAnonymous]` attribute.

```bash
dotnet add package TyFi.Auth.Functions.Worker
```

```csharp
builder.Services.AddJwtBearerTokenAuthentication(builder.Configuration);
builder.Services.AddTyFiAuthFunctionsWorker();
builder.UseMiddleware<AuthorizationWorkerMiddleware>();
```

Read the authenticated caller via `context.GetAuthenticatedUser()`.

Use `TyFi.Auth.Functions.AspNetCore` instead if your Function app calls
`builder.ConfigureFunctionsWebApplication()`. See the repository
[README](../../README.md) and [docs/INTEGRATION.md](../../docs/INTEGRATION.md) for full details.
