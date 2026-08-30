#!/bin/bash
set -euo pipefail

ACTION="${1:-}"
RESOURCE_GROUP="${2:-Amble_group}"
SERVER_NAME="${3:-gambol-pg}"
WEBAPP_NAME="Amble"

restart_webapp() {
  echo "Restarting Azure App Service '$WEBAPP_NAME' in '$RESOURCE_GROUP'..."
  az webapp restart --resource-group "$RESOURCE_GROUP" --name "$WEBAPP_NAME"
}

case "$ACTION" in
  web)
    restart_webapp
    ;;
  start)
    echo "Starting Azure PostgreSQL Flexible Server '$SERVER_NAME' in '$RESOURCE_GROUP'..."
    az postgres flexible-server start \
      --resource-group "$RESOURCE_GROUP" \
      --name "$SERVER_NAME"
    restart_webapp
    ;;
  stop)
    echo "Stopping Azure PostgreSQL Flexible Server '$SERVER_NAME' in '$RESOURCE_GROUP'..."
    az postgres flexible-server stop \
      --resource-group "$RESOURCE_GROUP" \
      --name "$SERVER_NAME"
    restart_webapp
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
    echo "Usage: ./scripts/azure.sh {web|start|stop|status} [resource-group] [server-name]" >&2
    echo "  web: restart App Service $WEBAPP_NAME only. start|stop: Postgres then same restart." >&2
    exit 1
    ;;
esac
