# Auth0 → Microsoft Entra External ID Migration — LLM Working Instructions

You are implementing TyFi Consulting's migration off Auth0 onto Microsoft Entra External ID, including a
shared `TyFi.Auth` NuGet package family that lives in THIS repository. Work one stage at a time, in order.
The owner reviews, commits, and pushes all work — do not push or open PRs without explicit confirmation.

## Mandatory reading before any stage

- [../llm-prompts/README.md](../llm-prompts/README.md) — the index of coding rules for all TyFi projects.
  Follow the relevant rule files it references (`azure-functions.md`, `react.md`, `terraform.md`,
  `github-actions.md`) for every line of code you write.
- The reference package for structure/publishing conventions: `../idempotency-handler`
  (TyFi.Idempotency.* — layered packages, nuget.org publishing under `tyfi-consulting`).

## Working protocol (applies to EVERY task below)

At the start of each task, run this process:

> Convene a team of {subject-matter} experts to design and implement this task. Simultaneously convene an
> adversarial review team of experts who challenge every significant decision, propose alternatives, and
> push back until consensus is reached. Record the final consensus and any accepted risks before writing
> code, and run a final adversarial review of the completed diff before handing it to the owner.

Substitute `{subject-matter}` per task (given in each stage). Present unresolved disagreements to the owner
instead of forcing consensus. Additionally, at the end of every non-trivial increment, run the adversarial
team against the actual diff (this matches the established "internal review pods" convention used in
therapy-scheduling-manager).

## Fixed decisions (already made with the owner — do not relitigate)

- Target platform: **Microsoft Entra External ID** (external/CIAM tenants), one tenant per project. Free
  tier: 50K MAU combined; app roles → standard `roles` claim; standard OIDC/PKCE flows.
- Email delivery: **Maileroo** (owner's existing provider). OTP emails are sent by a custom authentication
  extension on the `OnOtpSend` event → an Azure Function owned by us → Maileroo API. Do NOT use ACS.
- `TyFi.Auth` is **public**: public GitHub repo (this one), published to public nuget.org under
  `tyfi-consulting`. No secrets or tenant/client identifiers baked into the packages — everything via
  `IOptions` configuration, exactly like TyFi.Idempotency.
- Migration order: **therapy-scheduling-manager (pilot) → tab-cloud → stock-tracker → workout-app**.
- Each app persists the user's email in its own datastore at first authenticated request (from the token
  claim), so no admin-API lookups are ever needed.
- Tenant creation is scripted via [scripts/setup-tenant.sh](scripts/setup-tenant.sh); app registrations,
  app roles, and role assignments are managed by **Terraform** (`azuread` provider, one provider alias per
  tenant) inside each project's existing `infrastructure/`/`terraform/` folder. User flows, branding, and
  the OnOtpSend extension registration are configured manually in the Entra admin center for now (or via
  the `msgraph` provider if it proves reliable — adversarial team's call).

---

## Stage 0 — Tenant + email foundation

> Convene a team of Azure identity platform (Entra External ID/CIAM) experts... (per protocol above)

1. Run [scripts/setup-tenant.sh](scripts/setup-tenant.sh) once per project (owner supplies names,
   subscription, resource group). Record each tenant ID + domains in this repo's README.
2. Build the **Maileroo OTP sender Azure Function** (C#, .NET 8 isolated worker, in this repo under
   `apps/otp-mailer/` or similar):
   - Handles the `OnOtpSend` custom authentication extension callback: validate the caller (Entra signs
     extension callbacks with a bearer token — validate issuer/audience properly; this is a security
     boundary), read recipient + OTP, send via Maileroo API using per-tenant sender config (from address,
     domain, template) resolved from configuration.
   - One Function app serving all tenants (per-tenant config keyed by tenant ID) unless the adversarial
     team finds a strong reason for per-tenant deployments.
   - Maileroo API keys live in app settings/Key Vault, never in code.
3. In ONE tenant (the therapy one), configure end to end and prove the loop: sign-up/sign-in user flow with
   Email one-time passcode, branding, custom email provider (OnOtpSend) → Function → Maileroo → code
   arrives from the project's domain → sign-in completes.
4. Document the exact per-tenant setup steps you performed (portal clicks or Graph calls) in
   `docs/TENANT_SETUP.md` so the remaining three tenants are reproducible.

## Stage 1 — TyFi.Auth shared packages (this repo)

> Convene a team of .NET authentication library and OIDC/JWT security experts... (per protocol above)

Purpose: collapse the three near-identical JWT validation middlewares into one provider-agnostic package
family, so each app's provider swap becomes configuration-only.

Harvest sources (read all three before designing):
- `../tab-cloud/backend/SessionHarbor.Api/Auth/` — the cleanest/most generic (validator, middleware,
  bearer reader, permission resolver, `AuthenticatedUser`).
- `../stock-tracker/AzureFunctions/PortfolioAnalyzer/Auth/` — `[Authorize("permission")]` attribute pattern.
- `../therapy-scheduling-manager/apps/api` — `IBearerTokenAuthenticator` seam + options-gated
  "not configured" behavior (empty config = auth disabled, useful for local dev).

Suggested package layout (mirror TyFi.Idempotency's layering; adversarial team refines):
- `TyFi.Auth.Abstractions` — `AuthenticatedUser`, `IBearerTokenAuthenticator`, claim-mapping options
  (configurable claim names for roles/permissions/subject/email so Auth0-style `permissions` and
  Entra-style `roles` are both just config).
- `TyFi.Auth.Jwt` — Microsoft.IdentityModel-based validation: OIDC discovery, **cached** JWKS with
  refresh-on-key-miss, issuer/audience/lifetime/signature validation. No provider-specific code.
- `TyFi.Auth.Functions.Worker` and/or `TyFi.Auth.Functions.AspNetCore` — isolated-worker middleware for
  both HTTP models + `[Authorize("role-or-permission")]` attribute (tab-cloud uses the built-in model;
  therapy uses ASP.NET Core integration — support both, same lesson as TyFi.Idempotency).
- Optional `TyFi.Auth.EntraExternalId` — OnOtpSend callback payload models + callback-token validation
  helpers for the Stage 0 Function.

Requirements: net8.0, xUnit + Moq tests mirroring source structure, zero warnings, README with consumer
integration docs (like idempotency-handler's docs/INTEGRATION.md), GitHub Actions build/test/publish
workflow gated on version tags. While Auth0 is still live, packages must validate Auth0 tokens too (they
are generic OIDC — prove with tests against both token shapes). Security review checklist for the
adversarial team: algorithm confusion (RS256 only), audience confusion across the 4 apps, clock skew,
JWKS cache poisoning/refresh behavior, no PII in logs.

## Stage 2 — therapy-scheduling-manager migration (pilot)

> Convene a team of OIDC migration and React/MSAL + .NET Functions experts... (per protocol above)

Current state (from audit): `@auth0/auth0-react` SPA; backend validates via Microsoft.IdentityModel with
custom roles claim `https://therapy-scheduling/roles` populated by an Auth0 Action; role names
`"Therapy Scheduling App - Admin"` / `"Therapy Scheduling App - Therapist"` centralized in
`Application/Authorization/Roles.cs`; native `users` table keyed by `auth_subject` (Auth0 deliberately not
the source of truth); token bridge `src/api/accessToken.ts` + `AccessTokenRegistrar`; Terraform in
`infrastructure/`, deploy via `.github/workflows/deploy.yml`; work happens on branches with owner-reviewed
PRs — follow that convention.

Tasks:
1. **Terraform**: add the `azuread` provider (aliased to the new external tenant). Manage: SPA app
   registration (auth code + PKCE, SWA redirect URIs), API app registration (expose audience), **app
   roles**, and role assignments.
2. **Role names defined exactly ONCE — in Terraform.** (Owner requirement.) Declare the role names as
   Terraform variables/locals; the `azuread_application` app_role blocks consume them; Terraform then
   feeds the same values into the Function App app settings (e.g. `Auth__Roles__Admin`,
   `Auth__Roles__Therapist`) and into the web build env via the deploy workflow if the frontend needs
   them. Code reads role names from configuration/options — `Roles.cs` becomes an options-backed lookup
   (or is deleted in favor of options), with NO hardcoded role-name strings anywhere in C# or TS. Result:
   renaming a role = one Terraform edit.
3. **Backend**: adopt `TyFi.Auth.*` (replacing the in-repo `IBearerTokenAuthenticator` implementation);
   point issuer at `https://{tenantId}.ciamlogin.com/{tenantId}/v2.0`, audience at the API registration;
   claim mapping `roles` instead of `https://therapy-scheduling/roles`.
4. **Frontend**: replace `@auth0/auth0-react` with `@azure/msal-react` behind the existing token-bridge
   seam (`accessToken.ts` provider + `AccessTokenRegistrar` + the `SignedIn*` gates + `AuthMenu` +
   session-expiry `sessionSlice`) so component churn is minimal. Update the ~test mocks.
5. **User migration**: remap `users.auth_subject` from Auth0 subs to Entra object IDs (few users; a
   SQL migration script + email-based reconciliation, or owner does it manually — owner's call).
6. **Email persistence**: store the email claim on the `users` row at first authenticated request.
7. Config sweep: GitHub vars (`AUTH0_*` → Entra equivalents), Terraform vars, `deploy.yml`, README.
8. E2E: full login (email OTP from Maileroo) → role-gated pages → API calls → session expiry behavior.
   Keep Auth0 config working on a branch until the owner confirms cutover.

## Stage 3 — tab-cloud (SessionHarbor)

> Convene a team of browser-extension OAuth (PKCE) and data-migration experts... (per protocol above)

Current state (from audit): extension hand-rolls PKCE via `browser.identity.launchWebAuthFlow` — no Auth0
SDK (`extension/src/firefox/auth.ts`, constants in `extension/src/shared/config.ts`); backend generic OIDC
validation with Auth0 `permissions` claim (`sessions:read`/`sessions:write`); CI E2E uses a
client-credentials grant. **CRITICAL**: Azure Table partition keys are `SHA256(issuer + "\n" + subject)` —
changing issuer orphans ALL user data.

Tasks:
1. Write and test the **partition re-keying script** FIRST (map old Auth0 issuer+sub → new Entra
   issuer+sub per user, copy/rewrite rows, verify counts, keep a rollback path). This is the stage's main
   risk; the adversarial team should focus on it.
2. Entra: tenant (Stage 0 script), Terraform app registrations — public client with the
   `https://<app-id>.chromiumapp.org/` (and Firefox) redirect URIs, API registration with app roles
   equivalent to `sessions:read`/`sessions:write`, plus a client-credentials app for CI E2E.
3. Extension: update the 4 constants in `config.ts`, manifest host permissions
   (`tyficonsulting.ca.auth0.com` → `{tenant}.ciamlogin.com`), UI strings. The PKCE code should work
   against Entra's `/oauth2/v2.0/*` endpoints — verify token/response shapes in tests.
4. Backend: adopt `TyFi.Auth.*`; claim mapping `permissions` → `roles`.
5. CI: swap the M2M token acquisition + secrets in `deploy-api.yml` / extension workflows.

## Stage 4 — stock-tracker

> Convene a team of OIDC migration and Azure Table Storage experts... (per protocol above)

Current state (from audit): `@auth0/auth0-react` + email-OTP passwordless; ~9 `permissions` scopes enforced
by middleware + `[Authorize]`; `UserTable` maps internal GUID → `Auth0UserId` (architected for migration);
**Management API M2M calls** (`Auth0UserServiceImp`) fetch user emails for alert notification timers —
Auth0 is currently the sole email store.

Tasks:
1. **Kill the Management API dependency first (works even before cutover)**: persist email on `UserEntity`
   at authenticated request time (from the token claim), backfill existing users, rewrite the 3 timer-
   function call sites + `GetAllUsers` to read the local field, delete `Auth0UserServiceImp`.
2. Entra: tenant, Terraform SPA + API registrations, app roles replacing the 9 permission scopes,
   assignments for existing users.
3. Backend: adopt `TyFi.Auth.*` (replaces `JwtValidationMiddleware` + `Auth0ServiceExtensions`).
4. Frontend: swap to `@azure/msal-react` (same seams as therapy: `AuthGuard`, `useGetAccessToken`,
   `useHasPermission` — update `useHasPermission` to read `roles`); update ~14 test-file mocks.
5. Backfill new Entra subs alongside `Auth0UserId` on `UserTable` (email-based reconciliation), then cut
   over. Config sweep: `deploy.yml` hardcoded domain/client ID, Terraform vars, MCP server docs.

## Stage 5 — workout-app

> Convene a team of Blazor WASM and OAuth security-hardening experts... (per protocol above)

Current state (from audit): Blazor WASM + Fluxor; auth-code flow **without PKCE**; server-side code
exchange Function using the client secret; token audience borrows Auth0's Management API audience; JWT
middleware has `ValidateLifetime = false` (expired tokens accepted!), `ShowPII = true`, uncached JWKS;
token persisted in localStorage; ~1 real user (`email|...` subs in Table Storage).

Tasks — treat as a rebuild of the auth layer, not a port:
1. Entra: tenant, Terraform registrations. Define a REAL API audience (its own app registration).
2. Replace the `Auth0.AuthenticationApi` package + hand-built URLs with a proper PKCE flow (public client;
   the server-side exchange Function can be deleted if PKCE-only suffices — adversarial team decides).
3. Adopt `TyFi.Auth.*` — which inherently fixes lifetime validation, PII logging, and JWKS caching.
4. Reconsider token storage (in-memory + silent renewal vs localStorage) — document the consensus.
5. Migrate the single user's `Auth0Id` values by hand; config sweep (Terraform, workflow, appsettings).

## Decommission (after all stages verified in production)

Remove Auth0 tenant config, delete `AUTH0_*` secrets/vars from all four repos' GitHub settings, cancel the
Auth0 tenant. Only with explicit owner confirmation.
