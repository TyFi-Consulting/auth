# TyFi.Auth.EntraExternalId

Building blocks for Microsoft Entra External ID's `OnOtpSend` custom authentication extension: fail-closed
callback-token validation, request/response models, and a Maileroo-backed email sender. Hosted by each
consuming project's own Function app (this package ships the orchestration, not the deployed endpoint).

Entra-specific by design -- this is the one package in the family that is *not* provider-agnostic, because
it implements one specific provider's custom-extension contract. It sits independently of every other
package here (including `TyFi.Auth.Identity`) and only matters to a project that is actually using Entra
External ID as its identity provider.

```bash
dotnet add package TyFi.Auth.EntraExternalId
```

```csharp
builder.Services.AddEntraExternalIdOtpMailer(builder.Configuration);
```

See the repository [README](../../README.md), [docs/INTEGRATION.md](../../docs/INTEGRATION.md), and
[docs/TENANT_SETUP.md](../../docs/TENANT_SETUP.md) for full wiring and tenant-setup instructions.
