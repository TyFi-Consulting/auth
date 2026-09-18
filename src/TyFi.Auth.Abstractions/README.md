# TyFi.Auth.Abstractions

Provider-agnostic authentication types shared by every other `TyFi.Auth.*` package: `AuthenticatedUser`,
`IBearerTokenAuthenticator`, `BearerTokenAuthenticationResult`, and `AuthClaimMappingOptions` (configurable
claim-type mapping so `roles` vs `permissions` vs a custom URI claim is config, not code).

This package has no OIDC/JWT validation logic and no hosting-specific code. Reference a validator package
(`TyFi.Auth.Jwt`) and a host-adapter package (`TyFi.Auth.Functions.Worker` or `.AspNetCore`) alongside it.

```bash
dotnet add package TyFi.Auth.Abstractions
```

See the repository [README](../../README.md) for how the packages fit together and
[docs/INTEGRATION.md](../../docs/INTEGRATION.md) for wiring instructions.
