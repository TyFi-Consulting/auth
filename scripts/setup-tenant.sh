#!/usr/bin/env bash
# Creates a Microsoft Entra External ID (external/CIAM) tenant via ARM.
# The tenant itself is a directory; this ARM resource anchors it to a
# subscription/resource group for MAU billing.
#
# Usage:
#   ./setup-tenant.sh --name <tenantname> --display-name "<Display Name>" \
#       --resource-group <rg> [--subscription <sub-id>] \
#       [--location "United States"] [--country-code CA] [--rg-location canadacentral]
#
# Notes:
#   --name: 1-26 alphanumeric chars; becomes <name>.onmicrosoft.com and <name>.ciamlogin.com. Immutable.
#   --location: data residency geo, one of: 'United States', 'Europe', 'Asia Pacific', 'Australia'.
#     Canada is not a residency geo; CA country code maps to 'United States'.
set -euo pipefail

API_VERSION="2023-05-17-preview"
LOCATION="United States"
COUNTRY_CODE="CA"
RG_LOCATION="canadacentral"
SUBSCRIPTION=""
NAME=""
DISPLAY_NAME=""
RESOURCE_GROUP=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --name) NAME="$2"; shift 2 ;;
        --display-name) DISPLAY_NAME="$2"; shift 2 ;;
        --resource-group) RESOURCE_GROUP="$2"; shift 2 ;;
        --subscription) SUBSCRIPTION="$2"; shift 2 ;;
        --location) LOCATION="$2"; shift 2 ;;
        --country-code) COUNTRY_CODE="$2"; shift 2 ;;
        --rg-location) RG_LOCATION="$2"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [[ -z "$NAME" || -z "$DISPLAY_NAME" || -z "$RESOURCE_GROUP" ]]; then
    echo "Required: --name, --display-name, --resource-group" >&2
    exit 1
fi
if [[ ! "$NAME" =~ ^[a-zA-Z0-9]{1,26}$ ]]; then
    echo "--name must be 1-26 alphanumeric characters (becomes ${NAME}.onmicrosoft.com)" >&2
    exit 1
fi

if [[ -z "$SUBSCRIPTION" ]]; then
    SUBSCRIPTION="$(az account show --query id -o tsv)"
fi

echo "Registering resource provider Microsoft.AzureActiveDirectory (no-op if already registered)..."
az provider register --namespace Microsoft.AzureActiveDirectory --subscription "$SUBSCRIPTION" --wait

if [[ "$(az group exists --name "$RESOURCE_GROUP" --subscription "$SUBSCRIPTION")" != "true" ]]; then
    echo "Creating resource group ${RESOURCE_GROUP} in ${RG_LOCATION}..."
    az group create --name "$RESOURCE_GROUP" --location "$RG_LOCATION" --subscription "$SUBSCRIPTION" -o none
fi

RESOURCE_URL="https://management.azure.com/subscriptions/${SUBSCRIPTION}/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.AzureActiveDirectory/ciamDirectories/${NAME}?api-version=${API_VERSION}"

if az rest --method get --url "$RESOURCE_URL" -o none 2>/dev/null; then
    echo "Tenant resource '${NAME}' already exists; skipping creation."
else
    echo "Creating external tenant '${NAME}' (${DISPLAY_NAME})... this can take several minutes."
    az rest --method put --url "$RESOURCE_URL" --body "$(cat <<JSON
{
  "location": "${LOCATION}",
  "sku": { "name": "Standard", "tier": "A0" },
  "properties": {
    "createTenantProperties": {
      "displayName": "${DISPLAY_NAME}",
      "countryCode": "${COUNTRY_CODE}"
    }
  }
}
JSON
)" -o none
fi

echo "Waiting for provisioning to complete..."
for _ in $(seq 1 60); do
    STATE="$(az rest --method get --url "$RESOURCE_URL" --query properties.provisioningState -o tsv 2>/dev/null || echo "Pending")"
    if [[ "$STATE" == "Succeeded" ]]; then
        break
    fi
    if [[ "$STATE" == "Failed" ]]; then
        echo "Provisioning failed. Inspect the resource in the portal." >&2
        exit 1
    fi
    sleep 10
done

TENANT_ID="$(az rest --method get --url "$RESOURCE_URL" --query properties.tenantId -o tsv)"
echo ""
echo "Done."
echo "  Tenant ID:      ${TENANT_ID}"
echo "  Default domain: ${NAME}.onmicrosoft.com"
echo "  Login domain:   ${NAME}.ciamlogin.com"
echo "  Admin center:   https://entra.microsoft.com/${TENANT_ID}"
echo ""
echo "Next (per tenant, in the Entra admin center or Graph/Terraform):"
echo "  1. Create a sign-up/sign-in user flow with 'Email one-time passcode'."
echo "  2. Register the custom email provider (OnOtpSend) pointing at the Maileroo sender Function."
echo "  3. Register apps (SPA + API) and app roles — via Terraform (azuread provider) per project."
