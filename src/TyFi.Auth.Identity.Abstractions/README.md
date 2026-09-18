# TyFi.Auth.Identity.Abstractions

Data contracts and extension-point interfaces for `TyFi.Auth.Identity`'s self-hosted, passwordless
(email one-time-code) authentication: `IUserAccountStore`, `IEmailSender`, `IRefreshTokenStore`,
`IClaimsEnricher`, `AuthUserRecord`, `AuthTokens`, the `IAuthenticationService` entry point, its result
types (`RegisterResult`, `VerifyCodeResult`, `RefreshResult`, ...), and the options types
(`AuthIdentityOptions`, `RoleCatalogOptions`, `PermissionCatalogOptions`).

No storage, email, or token-signing code lives here -- reference `TyFi.Auth.Identity` for the
implementation, and implement `IUserAccountStore`/`IEmailSender`/`IRefreshTokenStore` in your own project.

```bash
dotnet add package TyFi.Auth.Identity.Abstractions
```

See [TyFi.Auth.Identity's README](../TyFi.Auth.Identity/README.md) for the full usage example, and the
repository [README](../../README.md) for how every package fits together.
