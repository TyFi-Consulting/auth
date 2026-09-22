# Adopt `TyFi.Auth.Functions.AspNetCore`'s attribute-based authorization in therapy-scheduling-manager — LLM Working Instructions

## Status (2026-09-22)

- **Stage 1: DONE, not shipped.** therapy-scheduling-manager branch `role-authorization-middleware-adoption`
  (commit `9b6d4c1` + a follow-up fix renaming the `admin/users` routes off Azure's reserved `admin/`
  prefix, a real bug the manual smoke test caught, unrelated to this change). Full `dotnet test` (539
  tests) and frontend `tsc`/`lint`/`vitest` green; a real local smoke test (Postgres + `func start`) was
  run across anonymous/Therapist/Admin tiers. Not pushed.
- **Stage 2: DONE, not released.** This repo's branch `role-authorization-stage2-extensions` (commit
  `ea4edf8`, off `main` at `v0.2.0`). Added `IAuthorizationRoleNameResolver` (role-name indirection) and
  `[AuthorizeAuthenticated]` (authenticated-only, no specific role) to `TyFi.Auth.Functions.Worker`,
  consumed by both host packages. +12 tests, docs updated. Validated by packing locally
  (`0.3.0-local1`) and pointing therapy-scheduling-manager at it: confirmed a zero-code-change upgrade
  passes all tests, then confirmed the consumer-side simplification (delete
  `RoleNameAuthorizationRequirementResolver`, add a ~15-line `IAuthorizationRoleNameResolver` impl,
  switch `CurrentUserFunction` to `[AuthorizeAuthenticated]`) also passes all tests — then **reverted**
  that validation in therapy-scheduling-manager since nothing is being released yet. Not pushed.
- **Stage 3: NOT started**, intentionally — the owner wants one combined PR/deploy (Stage 1 +
  the Stage 2 consumer update) rather than shipping in two passes. Resume here when ready to ship:
  cut the release (this stage), then bump therapy-scheduling-manager's package reference and reapply
  the consumer-side simplification described above.

You are closing a gap discovered during a PR review: `therapy-scheduling-manager` hand-rolls its own
per-handler role authorization (`PracticeAccessAuthorizer`, `AdminAccessAuthorizer`, manual
`AuthorizeAsync(...)` + outcome-mapping boilerplate in every handler) even though **this repo already
ships a middleware that does the same job via `[Authorize]`/`[AllowAnonymous]` attributes** —
[`TyFi.Auth.Functions.AspNetCore`](src/TyFi.Auth.Functions.AspNetCore/README.md), released in `v0.2.0`,
which `therapy-scheduling-manager` already depends on for `TyFi.Auth.Jwt`/`TyFi.Auth.Identity` but has
never adopted for authorization itself. The owner reviews, commits, and pushes all work — do not push or
open PRs without explicit confirmation.

## Mandatory reading before any stage

- [../llm-prompts/README.md](../llm-prompts/README.md) — the index of coding rules for all TyFi projects.
  Follow the relevant rule files it references (`azure-functions.md`, `terraform.md`, `github-actions.md`)
  for every line of code you write.
- [README.md](README.md), [docs/INTEGRATION.md](docs/INTEGRATION.md), and
  [src/TyFi.Auth.Functions.AspNetCore/README.md](src/TyFi.Auth.Functions.AspNetCore/README.md) — the
  package you are adopting/hardening. Read `AuthorizationAspNetCoreMiddleware.cs`,
  `AuthorizationRequirementResolver.cs`, `IProblemResultFactory`/`JsonProblemResultFactory`, and
  `HttpContextAuthenticationExtensions.cs` in full before changing anything.
- `../therapy-scheduling-manager/apps/api/src/TherapyScheduling.Application/PracticeAccess/` and
  `.../Administration/` — the bespoke pattern you are replacing (`PracticeAccessAuthorizer`,
  `AdminAccessAuthorizer`, `PracticeAccessResult`, every `*Handler` that calls `AuthorizeAsync` then maps
  the outcome to 401/403). `NEW_LLM_PROMPT.md` in that repo has the full history of why it was built this
  way (the TyFi.Auth.Identity cutover predates this discovery).
- [docs/PUBLISHING.md](docs/PUBLISHING.md) — the release process for step 3 below.

## Working protocol (applies to EVERY stage below)

At the start of each stage, run this process:

> Convene a team of {subject-matter} experts to design and implement this task. Simultaneously convene an
> adversarial review team of experts who challenge every significant decision, propose alternatives, and
> push back until consensus is reached. Record the final consensus and any accepted risks before writing
> code, and run a final adversarial review of the completed diff before handing it to the owner.

Substitute `{subject-matter}` per stage (given below). Present unresolved disagreements to the owner
instead of forcing consensus. Additionally, at the end of every non-trivial increment, run the adversarial
team against the actual diff — this is the established "internal review pods" convention used throughout
these repos; do not skip it because the change looks small.

## Fixed decisions (already made — do not relitigate)

- **Test the adoption in `therapy-scheduling-manager` first; only port changes back to this repo
  (`TyFi.Auth.Functions.AspNetCore`/`TyFi.Auth.Functions.Worker`/`TyFi.Auth.Abstractions`) once they've
  proven themselves against that real app, then cut a release.** Do not speculatively redesign the
  package in the abstract before it has been exercised end-to-end against every existing endpoint.
- The middleware already supports the roles this app needs today (`Admin`, `Therapist`, one required role
  per function via `[Authorize(Policy = "...")]`) — confirm this is still true before assuming a multi-role
  ("any of") extension is actually necessary; don't build it speculatively.
- `therapy-scheduling-manager`'s public/anonymous endpoints (booking, contact verification, health/ready)
  must keep working unauthenticated — every one of them needs an explicit `[AllowAnonymous]`, since the
  middleware fails closed on any HTTP-triggered function missing both attributes.
- Version bumps and releases follow the existing tag-triggered workflow in `docs/PUBLISHING.md` — no new
  release infrastructure needed.

## Stage 1 — Adopt the middleware in therapy-scheduling-manager (in that repo)

> Convene a team of Azure Functions isolated-worker and ASP.NET Core authorization experts... (per
> protocol above)

1. Add the `TyFi.Auth.Functions.AspNetCore` package reference (already at `v0.2.0`, no new release needed
   to start this stage) to `TherapyScheduling.Functions`, register it
   (`AddTyFiAuthFunctionsAspNetCore()`, `builder.UseMiddleware<AuthorizationAspNetCoreMiddleware>()`) in
   `Program.cs`.
2. Decorate every existing HTTP-triggered function with `[Authorize(Policy = "Admin")]`,
   `[Authorize(Policy = "Therapist")]`, or `[AllowAnonymous]` as appropriate — inventory every function
   first (Auth, Catalog, Availability, Agreements, Payments, Scheduling, PracticeReadiness, PublicBooking,
   BookingSession, Operations, Administration) so none is missed and none silently starts 500ing.
3. Delete `PracticeAccessAuthorizer`/`AdminAccessAuthorizer`/`PracticeAccessResult` and simplify every
   handler that used them down to reading `httpContext.GetAuthenticatedUser()` (or whatever the equivalent
   ends up being for handlers below the Functions layer — this may need a small adapter, since handlers
   currently take a bearer-token string, not an `HttpContext`).
4. Compare the middleware's 401/403 response shape (`IProblemResultFactory`/`JsonProblemResultFactory`,
   likely RFC 7807 `ProblemDetails`) against what the frontend currently expects (`{ message: "..." }`,
   read via each feature's own `fetchErrorStatus`/`errorMessage` helpers). Fix whichever side is wrong —
   do not let the frontend silently show a blank/generic error because the shape changed.
5. Full test pass (backend `dotnet test`, frontend `vitest`/`tsc -b`/`eslint`) plus a manual smoke test of
   at least one endpoint per role tier (anonymous, Therapist, Admin) against a real deployment before
   calling this stage done.
6. Write down every gap, bug, or missing feature you hit while doing this (e.g., response-shape mismatch,
   any real need for multi-role support, anything about `AuthorizationRequirementResolver`'s reflection
   that felt fragile) — this list is the input to Stage 2.

## Stage 2 — Port proven fixes back into this repo

> Convene a team of .NET authentication library and OIDC/JWT security experts... (per protocol above)

1. For each gap recorded in Stage 1, fix it here (`TyFi.Auth.Functions.AspNetCore`/
   `TyFi.Auth.Functions.Worker`/`TyFi.Auth.Abstractions` as appropriate), with unit tests, following this
   repo's existing test conventions (`dotnet test TyFi.Auth.sln`).
2. Re-point `therapy-scheduling-manager` at a locally-built/pack-referenced copy of the fixed package (not
   yet the released NuGet version) and re-run its full test suite to confirm the fix actually resolves
   what Stage 1 found, before publishing anything.
3. Update the affected package READMEs and `docs/INTEGRATION.md` if the fix changes any documented
   behavior or wiring step.

## Stage 3 — Release

> Convene a team of release-engineering/package-versioning experts... (per protocol above)

1. Decide the version bump (semver: patch for bug fixes, minor if you added any new public surface like a
   multi-role policy option) with the owner.
2. Follow the tagged-release process in [docs/PUBLISHING.md](docs/PUBLISHING.md) and this repo's
   [README.md](README.md) "Release" section.
3. Bump `therapy-scheduling-manager`'s package reference to the newly published version, re-run its full
   test suite one more time, and hand off to the owner for review before any push/PR.
