#!/bin/bash
set -euo pipefail

# Per-boot Cloud Agent start. Processes do not survive the Build snapshot.
# Starts Postgres 17 on 127.0.0.1:5432 and exports compose-style connection strings.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=pg.sh
. "${SCRIPT_DIR}/pg.sh"

persist_cloud_env
hash -r

if [ ! -x "${PGBIN}/pg_ctl" ]; then
    echo "PostgreSQL 17 missing. The Cloud Agent Build did not run install. Stop and report." >&2
    exit 1
fi

init_cluster
start_postgres
ensure_databases
wait_for_tcp
verify_postgres_tcp

echo "Postgres ready on ${PGHOST_TCP}:${PGPORT}"
echo "TEST_DB_CONNECTION_STRING=${TEST_DB_CONNECTION_STRING}"
echo "DB_CONNECTION_STRING=${DB_CONNECTION_STRING}"
