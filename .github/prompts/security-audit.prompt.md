---
description: Bi-annual security audit of the TyFi.Auth.Identity self-hosted authentication packages
mode: agent
---

You are auditing `TyFi.Auth.Identity` and `TyFi.Auth.Identity.Abstractions` -- this repo's self-hosted,
passwordless (email one-time-code) authentication engine -- against current attack trends. This audit is
run roughly every six months; treat it as a real security review, not a formality.

## Steps

1. Research current (since the last audit, or the last ~6-12 months if unknown) publicly disclosed attack
   techniques, CVEs, and OWASP guidance relevant to this package's actual surface area: OAuth2/OIDC/JWT
   validation and issuance, refresh-token rotation, one-time-code/OTP delivery and verification, account
   lockout/brute-force protection, and claims-based authorization. Prioritize official sources: OWASP
   Cheat Sheet Series, OWASP ASVS updates, CVE databases, and vendor security advisories (Microsoft
   IdentityModel, IETF OAuth working group drafts).
2. Read through `src/TyFi.Auth.Identity/` and `src/TyFi.Auth.Identity.Abstractions/` in full.
3. Cross-reference: for each attack technique/finding from step 1, determine whether this codebase is
   exposed, already mitigated, or not applicable (explain why for each).
4. Pay particular attention to areas that have historically been sources of real vulnerabilities:
   - JWT algorithm confusion / `alg: none` acceptance
   - Issuer/audience/expiry validation gaps
   - Refresh token reuse detection and family revocation correctness
   - OTP code entropy, expiry, and attempt-limiting
   - Timing/enumeration side channels (do responses differ for "account doesn't exist" vs "wrong code"?)
   - Lockout policy correctness and reset-on-success behavior
   - Any new dependency (Microsoft.IdentityModel.*, etc.) advisories since the pinned versions in
     `src/TyFi.Auth.Identity/TyFi.Auth.Identity.csproj` and `src/TyFi.Auth.Jwt/TyFi.Auth.Jwt.csproj`
5. Produce a findings report: for each finding, severity, affected file(s), and a concrete recommended
   fix. If nothing is found, say so explicitly -- do not manufacture findings.
6. For any finding you and the user agree should be fixed now, implement the fix with accompanying xUnit
   tests (mirroring the existing test project structure), and run `dotnet test TyFi.Auth.sln` before
   considering it done.
7. Summarize what changed (or didn't) for the record, so the next audit has a clear starting point.

Do not treat this as a rubber stamp -- if the research in step 1 turns up nothing new, say so plainly
rather than inventing minor style nitpicks to justify the audit.
