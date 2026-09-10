# auth

TyFi Consulting's shared authentication infrastructure: Microsoft Entra External ID tenant
provisioning ([scripts/setup-tenant.sh](scripts/setup-tenant.sh)), the Maileroo OTP-sender
Azure Function, and the `TyFi.Auth` NuGet package family. See
[NEW_LLM_PROMPT.md](NEW_LLM_PROMPT.md) for the full migration plan.

## Tenants

| Project | Tenant name | Tenant ID | Resource group | Subscription |
|---|---|---|---|---|
| therapy-scheduling-manager | `tyfischeduler` | `e748122c-ff6e-4759-a0e3-26b5ad5adc07` | `rg-therapy-scheduling` | Pay-As-You-Go (`91e2998a-d362-4ac4-bc91-4f52bfc7483b`) |

- Default domain: `<tenant name>.onmicrosoft.com`
- Login domain (used as the OIDC authority): `<tenant name>.ciamlogin.com`
- Admin center: `https://entra.microsoft.com/<tenant ID>`

> New tenants are created into the **existing resource group of the project they belong
> to** (not a new `rg-tyfi-auth-*` group), so they show up alongside that project's other
> Azure resources.

## apps/otp-mailer

A .NET 10 isolated-worker Azure Function implementing the `OnOtpSend` custom authentication
extension callback: validates the Entra-issued bearer token per tenant, then sends the one-time
passcode through [Maileroo](https://maileroo.com) instead of Microsoft's default email provider.
One Function App serves all four tenants (config keyed by tenant ID). See
[docs/TENANT_SETUP.md](docs/TENANT_SETUP.md) for per-tenant wiring steps and the configuration
shape, and [apps/otp-mailer](apps/otp-mailer) for the source.

```bash
cd apps/otp-mailer
dotnet test   # build + run all unit tests
```