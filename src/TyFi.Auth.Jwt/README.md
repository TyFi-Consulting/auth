# TyFi.Auth.Jwt

Provider-agnostic OIDC/JWT bearer-token **validation**: cached JWKS with refresh-on-key-miss, and
issuer/audience/lifetime/signature validation (RS256 only by default). Works against any
standards-compliant OIDC issuer -- Auth0 or Microsoft Entra -- purely through configuration, with zero
provider-specific code.

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

## Validating self-issued tokens (e.g. `TyFi.Auth.Identity`)

A self-issued token has no discovery document or JWKS endpoint to fetch a public key from, so the
default OIDC-discovery path can't validate it. Set `SigningKey` to switch to static-key validation
instead -- this skips discovery entirely and validates directly against that key. It must be paired
with `ValidAlgorithms`, which is not widened automatically (a misconfiguration fails closed rather than
silently accepting an unintended algorithm):

```jsonc
{
  "Auth:Jwt:Issuer": "https://your-app",
  "Auth:Jwt:Audience": "api://your-app-id",
  "Auth:Jwt:SigningKey": "<the same base64 key as Auth:Identity:SigningKey>",
  "Auth:Jwt:ValidAlgorithms:0": "HS256"
}
```

See the repository [README](../../README.md) and [docs/INTEGRATION.md](../../docs/INTEGRATION.md) for
full wiring instructions alongside a `TyFi.Auth.Functions.*` host-adapter package.
