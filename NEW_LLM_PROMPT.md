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

## Current state (2026-09-10)

- The therapy tenant exists: `tyfischeduler` (IDs in [README.md](README.md#tenants)). Remaining three
  tenants not yet created.
- **Stage 1 is DONE.** `apps/otp-mailer/` has been harvested and deleted. `TyFi.Auth.Abstractions`,
  `TyFi.Auth.Jwt`, `TyFi.Auth.Functions.Worker`, `TyFi.Auth.Functions.AspNetCore`, and
  `TyFi.Auth.EntraExternalId` all exist under `src/`/`tests/` (see `TyFi.Auth.sln`), each with unit
  tests (62 total, all green; `dotnet test TyFi.Auth.sln`). `TyFi.Auth.EntraExternalId` is
  single-tenant options (one Function app = one tenant, per the reversed centralized→per-project
  decision below), reuses `TyFi.Auth.Jwt`'s JWKS cache, and enforces its own response-time budget
  (`Auth:EntraOtpSend:ResponseTimeout`, default 1.5s) so a slow Maileroo call fails fast into the
  tenant's `fallbackToMicrosoftProviderOnError` instead of risking Entra's ~2s callback timeout.
  CI (`ci.yml`) and tag-triggered NuGet Trusted Publishing release (`release.yml` +
  `docs/PUBLISHING.md`) are wired up but the packages have not yet been published or had their
  `NUGET_USER` variable/Trusted Publishing policy configured — that's a manual owner step.
  `docs/INTEGRATION.md` documents consumer wiring, including the REQUIRED tiny `[Function("OnOtpSend")]`
  stub class each consuming app must add (isolated-worker function indexing only scans the
  consuming app's own assembly, not referenced library assemblies — the package cannot define the
  trigger itself, only everything behind it).
- Next: Stage 2 (therapy-scheduling-manager pilot migration — Terraform, adopt `TyFi.Auth.*`, host the
  OnOtpSend endpoint, MSAL frontend swap, user migration, E2E).

## Fixed decisions (already made with the owner — do not relitigate)

- Target platform: **Microsoft Entra External ID** (external/CIAM tenants), one tenant per project. Free
  tier: 50K MAU combined; app roles → standard `roles` claim; standard OIDC/PKCE flows.
- **Auth0 alternative reassessed and REJECTED (2026-09-10)** — do not reopen. Auth0's custom email
  provider Action (confirmed available on the owner's plan) plus its more complete Terraform provider
  would have solved the per-project email-domain pain within the existing single free tenant. The owner
  still chose Entra for: per-project tenant isolation (separate user pools — matters most for therapy's
  privacy-sensitive clients), linear published growth pricing ($0 to 50K MAU, then $0.03/MAU, vs Auth0's
  paid-plan cliff past its 25K free tier), free-tier durability (identity is a loss-leader for Microsoft
  that drives Azure spend; it is Okta's entire revenue), and all-C#/one-bill ecosystem fit. Trade-offs
  accepted eyes-open: weaker Terraform coverage (user flows/branding/email extension = portal or Graph),
  7-day sign-in/audit log retention on external tenants (export via Azure Monitor if longer is needed),
  SMS OTP is MFA-only and a paid add-on (email OTP is the primary method), self-coded web login pages
  would require Entra's newer native-auth API + a CORS proxy (hosted branded pages are the plan), and
  Microsoft product-churn risk (they retired Azure AD B2C with ~5-year runway) — the TyFi.Auth
  abstraction is the hedge against that.
- Email delivery: **Maileroo** (owner's existing provider), sent **from each project's own Function app**,
  NOT from a centralized mailer service. Each project's existing Function app hosts one `OnOtpSend`
  endpoint; its tenant's custom authentication extension targets that app. All security- and
  protocol-critical logic (callback token validation, payload parsing, response schema, Maileroo client)
  ships in a shared NuGet package (`TyFi.Auth.EntraExternalId`) so the per-project surface is a DI call,
  an options block, and the project-owned email template/copy. Rationale: email text/branding/config
  deploy with the product that uses them; per-app blast radius; per-app Maileroo keys. Do NOT use ACS.
- **OnOtpSend timing contract**: Entra enforces a hard ~2-second response timeout (error 1003005
  `CustomExtensionTimedOut`) with retries — this bounds the HTTP RESPONSE, not email delivery. The
  endpoint must validate, hand off the send, and return `continueWithDefaultBehavior` fast; never await
  full delivery confirmation synchronously. Additionally enable `fallbackToMicrosoftProviderOnError` on
  each tenant's listener (Graph PATCH, documented in TENANT_SETUP.md) so a cold start or Maileroo outage
  degrades to a Microsoft-branded email instead of a failed sign-in.
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
- **Email OTP is native Entra functionality** — Microsoft generates and verifies the codes, rate-limits
  attempts, and renders the hosted sign-in UI; our code only customizes DELIVERY via OnOtpSend. Never
  implement OTP generation or verification.
- **The migration is reversible per app.** Each stage stands alone; the owner may stop after the therapy
  pilot and leave the remaining apps on Auth0 indefinitely (no cost pressure there). Never batch
  cross-app changes into one increment.

---

## Stage 0 — Tenant + email foundation

> Convene a team of Azure identity platform (Entra External ID/CIAM) experts... (per protocol above)

1. Run [scripts/setup-tenant.sh](scripts/setup-tenant.sh) once per project (owner supplies names,
   subscription, resource group). The therapy tenant `tyfischeduler` ALREADY EXISTS — create the other
   three when their stages begin. Record each tenant ID + domains in this repo's README.
2. Per tenant, configure the sign-up/sign-in user flow with Email one-time passcode and branding, and keep
   [docs/TENANT_SETUP.md](docs/TENANT_SETUP.md) accurate as the wiring evolves (it currently describes the
   old centralized-mailer deployment — update it for per-project hosting as part of Stage 1/2).
3. The end-to-end OTP email proof (extension → project Function → Maileroo → sign-in completes) happens in
   Stage 2, because the endpoint now lives in the therapy project's own Function app.

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
- `TyFi.Auth.EntraExternalId` (REQUIRED, not optional) — the OnOtpSend endpoint as a near-complete
  building block: callback-token validation (issuer/audience/Microsoft-caller `azp` check, FAIL CLOSED
  when unconfigured), payload models, response schema, Maileroo sender, and a registration extension so a
  consuming Function app exposes the endpoint with one DI call + one options block + its own
  `IOtpEmailBodyRenderer` (the email template/copy stays project-owned). HARVEST `apps/otp-mailer/`'s
  `Auth/`, `Handling/`, `Mail/`, `Models/` layers + their tests — they were built for this shape; the main
  changes are single-tenant options (each app serves ONE tenant, so replace the tenant LIST with one
  tenant's settings), splitting the template out as the consumer seam, and honoring the 2-second response
  contract (hand off the send; don't block the response on delivery confirmation — decide fire-and-forget
  vs queue with the adversarial team). Then DELETE `apps/otp-mailer`.

Requirements: net8.0, xUnit + Moq tests mirroring source structure, zero warnings, README with consumer
integration docs (like idempotency-handler's docs/INTEGRATION.md), GitHub Actions build/test/publish
workflow gated on version tags. Packages must be provider-agnostic in BOTH directions — they validate
Auth0 tokens while Auth0 is still live, AND a future move off Entra (back to Auth0 or to another OIDC
provider) must be config-only: no provider-specific claim names, issuer formats, or endpoints hardcoded
anywhere except as options defaults. Prove with tests against both Auth0-shaped and Entra-shaped tokens.
Exception: `TyFi.Auth.EntraExternalId` is intentionally Entra-specific (the OnOtpSend contract is
proprietary) — confine ALL Entra-isms to that one package. Security review checklist for the
adversarial team: algorithm confusion (RS256 only), audience confusion across the 4 apps, clock skew,
JWKS cache poisoning/refresh behavior, no PII in logs.

## Stage 2 — therapy-scheduling-manager migration (pilot)

Moved out of this repo. Full stage instructions (working protocol, decisions, current-state audit,
tasks) now live in `../therapy-scheduling-manager/ENTRA_MIGRATION_PROMPT.md`, so this public repo
stays free of project-specific detail. Run that prompt once `TyFi.Auth.*` is published (see
Stage 1 above and `docs/PUBLISHING.md`).

## Stage 3 — tab-cloud (SessionHarbor)

Moved out of this repo. Full stage instructions now live in
`../tab-cloud/ENTRA_MIGRATION_PROMPT.md`.

## Stage 4 — stock-tracker

Moved out of this repo. Full stage instructions now live in
`../stock-tracker/llm-instructions/entra-migration-prompt.md`.

## Stage 5 — workout-app

Moved out of this repo. Full stage instructions now live in
`../workout-app/ENTRA_MIGRATION_PROMPT.md`.

## Decommission (after all stages verified in production)

Remove Auth0 tenant config, delete `AUTH0_*` secrets/vars from all four repos' GitHub settings, cancel the
Auth0 tenant. Only with explicit owner confirmation. (Each project's own migration prompt also carries a
decommission note scoped to that repo.)

