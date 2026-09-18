# TyFi.Auth.Jwt

Provider-agnostic OIDC/JWT bearer-token **validation**: cached JWKS with refresh-on-key-miss, and
issuer/audience/lifetime/signature validation (RS256 only). Works against any standards-compliant OIDC
issuer -- Auth0, Microsoft Entra, or the self-issued tokens from `TyFi.Auth.Identity` -- purely through
configuration, with zero provider-specific code.

```bash
dotnet add package TyFi.Auth.Jwt
```

```jsonc
{
  "Auth:Jwt:Issuer": "https://your-issuer",
  "Auth:Jwt:Audience": "api://your-app-id",
  "Auth:Jwt:ClaimMapping:RolesClaimType": "roles"
}
```

Leaving `Issuer`/`Audience` empty disables authentication entirely (every request returns 401) -- a
deliberate fail-closed default for unconfigured environments.

See the repository [README](../../README.md) and [docs/INTEGRATION.md](../../docs/INTEGRATION.md) for
full wiring instructions alongside a `TyFi.Auth.Functions.*` host-adapter package.
