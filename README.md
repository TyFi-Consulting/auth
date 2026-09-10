# auth

TyFi Consulting's shared authentication infrastructure: Microsoft Entra External ID tenant
provisioning ([scripts/setup-tenant.sh](scripts/setup-tenant.sh)) and the `TyFi.Auth` NuGet
package family.

## Packages

| Package | Purpose |
|---|---|
| [`TyFi.Auth.Abstractions`](src/TyFi.Auth.Abstractions) | `AuthenticatedUser`, `IBearerTokenAuthenticator`, claim-mapping options. No OIDC or hosting code. |
| [`TyFi.Auth.Jwt`](src/TyFi.Auth.Jwt) | Provider-agnostic OIDC/JWT validation (cached JWKS, RS256-only) — Auth0 vs. Entra is config-only. |
| [`TyFi.Auth.Functions.Worker`](src/TyFi.Auth.Functions.Worker) | Isolated-worker middleware for the built-in HTTP model. |
| [`TyFi.Auth.Functions.AspNetCore`](src/TyFi.Auth.Functions.AspNetCore) | Isolated-worker middleware for the ASP.NET Core integration HTTP model. |
| [`TyFi.Auth.EntraExternalId`](src/TyFi.Auth.EntraExternalId) | The `OnOtpSend` custom authentication extension endpoint + Maileroo sender, hosted by each project's own Function app. |

See [docs/INTEGRATION.md](docs/INTEGRATION.md) for wiring instructions and
[docs/PUBLISHING.md](docs/PUBLISHING.md) for the NuGet release process.

```bash
dotnet test TyFi.Auth.sln   # build + run all unit tests
```

## Tenants

Tenant IDs, subscriptions, and resource groups are tracked privately (not in this public repo).
See [docs/TENANT_SETUP.md](docs/TENANT_SETUP.md) for the reusable per-tenant `OnOtpSend` wiring
steps.