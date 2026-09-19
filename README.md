# auth

TyFi Consulting's shared authentication infrastructure: the `TyFi.Auth` NuGet package family, covering
both provider-agnostic bearer-token validation (Auth0, Entra External ID, or self-issued) and a fully
self-hosted, passwordless authentication engine for projects moving off a third-party identity provider.

## Packages

Every package links to its own README below for a detailed usage example. Start with
[docs/INTEGRATION.md](docs/INTEGRATION.md) for how they combine in a real Function app.

| Package | Purpose |
|---|---|
| [`TyFi.Auth.Abstractions`](src/TyFi.Auth.Abstractions/README.md) | `AuthenticatedUser`, `IBearerTokenAuthenticator`, claim-mapping options. No OIDC or hosting code. |
| [`TyFi.Auth.Jwt`](src/TyFi.Auth.Jwt/README.md) | Provider-agnostic OIDC/JWT **validation** (cached JWKS, RS256 by default; static-key HS256 mode for self-issued tokens) — Auth0, Entra, or self-issued tokens are all config-only. |
| [`TyFi.Auth.Functions.Worker`](src/TyFi.Auth.Functions.Worker/README.md) | Isolated-worker middleware for the built-in HTTP model. |
| [`TyFi.Auth.Functions.AspNetCore`](src/TyFi.Auth.Functions.AspNetCore/README.md) | Isolated-worker middleware for the ASP.NET Core integration HTTP model. |
| [`TyFi.Auth.Identity.Abstractions`](src/TyFi.Auth.Identity.Abstractions/README.md) | Data contracts and extension points (`IUserAccountStore`, `IEmailSender`, `IRefreshTokenStore`, ...) for the self-hosted identity engine. |
| [`TyFi.Auth.Identity`](src/TyFi.Auth.Identity/README.md) | Self-hosted, passwordless (email one-time-code) authentication: registration, login, refresh-token rotation with reuse detection, lockout, and role/permission-to-claims expansion. No third-party identity provider required. |
| [`TyFi.Auth.EntraExternalId`](src/TyFi.Auth.EntraExternalId/README.md) | The `OnOtpSend` custom authentication extension endpoint + Maileroo sender for projects still using Microsoft Entra External ID, hosted by each project's own Function app. Entra-specific by design; independent of every other package here. |

See [docs/INTEGRATION.md](docs/INTEGRATION.md) for wiring instructions and
[docs/PUBLISHING.md](docs/PUBLISHING.md) for the NuGet release process.

```bash
dotnet test TyFi.Auth.sln   # build + run all unit tests
```

## Release

After the release changes are merged to `main`, tag that commit and push the tag. The tag triggers
the GitHub Actions release workflow, which publishes every `TyFi.Auth.*` package at that version.

```bash
git switch main
git pull --ff-only origin main
git tag -a v0.1.1 -m "Release v0.1.1"
git push origin v0.1.1
gh run watch
```

Replace `0.1.1` with the intended semantic version. See
[docs/PUBLISHING.md](docs/PUBLISHING.md) for the one-time NuGet trusted-publishing setup and
troubleshooting.

## Tenants

Tenant IDs, subscriptions, and resource groups are tracked privately (not in this public repo).
See [docs/TENANT_SETUP.md](docs/TENANT_SETUP.md) for the reusable per-tenant `OnOtpSend` wiring
steps.