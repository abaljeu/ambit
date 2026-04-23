#!/bin/bash
set -euo pipefail

./scripts/azure-postgres-server.sh stop "${1:-Amble_group}" "${2:-gambol-pg}"
