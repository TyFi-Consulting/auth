#!/usr/bin/env bash
# Consents an existing per-project GitHub Actions OIDC app registration into an Entra External
# ID (CIAM) tenant, and grants it the Microsoft Graph Application.ReadWrite.All application
# permission there, so Terraform's azuread provider can manage app registrations in that tenant
# using the SAME identity already used for azurerm deploys — option (b) in each project's
# docs/architecture/entra-terraform-bootstrap.md (one identity, no new secret, vs. option (a)'s
# separate tenant-scoped app registration).
#
# Prerequisites: `az login` as an admin of BOTH the app's home tenant (to change its
# signInAudience) and the target external tenant (to create the service principal + grant the
# app role there). Idempotent — safe to re-run.
#
# Usage:
#   ./bootstrap-terraform-identity.sh --app-id <github-actions-app-client-id> \
#       --external-tenant-id <external-tenant-guid>
set -euo pipefail

GRAPH_APP_ID="00000003-0000-0000-c000-000000000000" # Microsoft Graph, well-known appId
APP_ID=""
EXTERNAL_TENANT_ID=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --app-id) APP_ID="$2"; shift 2 ;;
        --external-tenant-id) EXTERNAL_TENANT_ID="$2"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [[ -z "$APP_ID" || -z "$EXTERNAL_TENANT_ID" ]]; then
    echo "Required: --app-id <client-id>, --external-tenant-id <tenant-guid>" >&2
    exit 1
fi

echo "==> Ensuring '${APP_ID}' allows sign-in from other tenants (required for cross-tenant consent)..."
CURRENT_AUDIENCE="$(az ad app show --id "$APP_ID" --query signInAudience -o tsv)"
if [[ "$CURRENT_AUDIENCE" == "AzureADMyOrg" ]]; then
    az ad app update --id "$APP_ID" --set signInAudience=AzureADMultipleOrgs
    echo "    Updated signInAudience: AzureADMyOrg -> AzureADMultipleOrgs"
else
    echo "    Already ${CURRENT_AUDIENCE}; skipping."
fi

echo "==> Acquiring a Microsoft Graph token for the external tenant (${EXTERNAL_TENANT_ID})..."
EXTERNAL_TOKEN="$(az account get-access-token --tenant "$EXTERNAL_TENANT_ID" --resource-type ms-graph --query accessToken -o tsv)"

graph_get() {
    # $1 = path (e.g. "servicePrincipals"), optional $2 = OData filter expression (unencoded).
    if [[ -n "${2:-}" ]]; then
        curl -sS -G -H "Authorization: Bearer ${EXTERNAL_TOKEN}" --data-urlencode "\$filter=$2" \
            "https://graph.microsoft.com/v1.0/$1"
    else
        curl -sS -H "Authorization: Bearer ${EXTERNAL_TOKEN}" "https://graph.microsoft.com/v1.0/$1"
    fi
}

graph_post() {
    curl -sS -X POST -H "Authorization: Bearer ${EXTERNAL_TOKEN}" -H "Content-Type: application/json" \
        -d "$2" "https://graph.microsoft.com/v1.0/$1"
}

echo "==> Checking for an existing service principal for '${APP_ID}' in the external tenant..."
SP_ID="$(graph_get "servicePrincipals" "appId eq '${APP_ID}'" | python3 -c "
import json, sys
values = json.load(sys.stdin)['value']
print(values[0]['id'] if values else '')
")"

if [[ -z "$SP_ID" ]]; then
    SP_ID="$(graph_post "servicePrincipals" "{\"appId\": \"${APP_ID}\"}" | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")"
    echo "    Created service principal ${SP_ID}."
else
    echo "    Already exists (${SP_ID}); skipping."
fi

echo "==> Resolving Microsoft Graph's service principal + the Application.ReadWrite.All app role..."
GRAPH_SP="$(graph_get "servicePrincipals" "appId eq '${GRAPH_APP_ID}'")"
GRAPH_SP_ID="$(echo "$GRAPH_SP" | python3 -c "import json,sys; print(json.load(sys.stdin)['value'][0]['id'])")"
APP_ROLE_ID="$(echo "$GRAPH_SP" | python3 -c "
import json, sys
role = next(r for r in json.load(sys.stdin)['value'][0]['appRoles'] if r['value'] == 'Application.ReadWrite.All')
print(role['id'])
")"

echo "==> Checking for an existing Application.ReadWrite.All grant..."
ALREADY_GRANTED="$(graph_get "servicePrincipals/${SP_ID}/appRoleAssignments" | python3 -c "
import json, sys
assignments = json.load(sys.stdin)['value']
print('yes' if any(a['appRoleId'] == '${APP_ROLE_ID}' and a['resourceId'] == '${GRAPH_SP_ID}' for a in assignments) else 'no')
")"

if [[ "$ALREADY_GRANTED" == "yes" ]]; then
    echo "    Already granted; skipping."
else
    graph_post "servicePrincipals/${SP_ID}/appRoleAssignments" \
        "{\"principalId\": \"${SP_ID}\", \"resourceId\": \"${GRAPH_SP_ID}\", \"appRoleId\": \"${APP_ROLE_ID}\"}" >/dev/null
    echo "    Granted Application.ReadWrite.All (admin-consented as part of this run)."
fi

echo "==> Done. Terraform's azuread provider can now manage app registrations in ${EXTERNAL_TENANT_ID} using this same GitHub Actions identity."
