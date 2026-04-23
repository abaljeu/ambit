#!/bin/bash
set -euo pipefail

ACTION="${1:-}"
RESOURCE_GROUP="${2:-Amble_group}"
SERVER_NAME="${3:-gambol-pg}"

case "$ACTION" in
  start)
    echo "Starting Azure PostgreSQL Flexible Server '$SERVER_NAME' in '$RESOURCE_GROUP'..."
    az postgres flexible-server start \
      --resource-group "$RESOURCE_GROUP" \
      --name "$SERVER_NAME"
    ;;
  stop)
    echo "Stopping Azure PostgreSQL Flexible Server '$SERVER_NAME' in '$RESOURCE_GROUP'..."
    az postgres flexible-server stop \
      --resource-group "$RESOURCE_GROUP" \
      --name "$SERVER_NAME"
    ;;
  status)
    echo "Fetching status for Azure PostgreSQL Flexible Server '$SERVER_NAME' in '$RESOURCE_GROUP'..."
    az postgres flexible-server show \
      --resource-group "$RESOURCE_GROUP" \
      --name "$SERVER_NAME" \
      --query "{name:name,state:state,location:location,version:version}" \
      -o json
    ;;
  *)
    echo "Usage: ./scripts/azure-postgres-server.sh {start|stop|status} [resource-group] [server-name]" >&2
    exit 1
    ;;
esac
