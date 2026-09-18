#!/bin/bash
set -euo pipefail

# Idempotent Cloud Agent install. Safe to re-run on an existing Build.
# Disk only: .NET 10 SDK, Fable local tool, npm lockfile deps, Postgres 17 packages
# and cluster. Does not leave Postgres running (start.sh does that per boot).

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=pg.sh
. "${SCRIPT_DIR}/pg.sh"

persist_cloud_env

mkdir -p "$DOTNET_ROOT"

INSTALLER="${DOTNET_ROOT}/dotnet-install.sh"
curl --retry 3 --retry-delay 2 -fsSL https://dot.net/v1/dotnet-install.sh -o "$INSTALLER"
chmod +x "$INSTALLER"
"$INSTALLER" --channel 10.0 --install-dir "$DOTNET_ROOT"

sudo ln -sfn "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet
hash -r
persist_cloud_env

if ! command -v dotnet >/dev/null; then
    echo "dotnet missing after SDK install" >&2
    exit 1
fi

if ! dotnet --list-sdks | grep -E '^10\.'; then
    echo ".NET 10 SDK missing after install" >&2
    dotnet --list-sdks >&2
    exit 1
fi

if [ -f .config/dotnet-tools.json ]; then
    dotnet tool restore
fi

if [ -f package-lock.json ] && command -v npm >/dev/null; then
    npm ci
fi

install_postgres_packages
init_cluster
start_postgres
ensure_databases
wait_for_tcp
verify_postgres_tcp
stop_postgres

dotnet --info
dotnet fable --help
echo "Cloud Agent install ok: $(command -v dotnet) $(dotnet --version); Postgres 17 cluster at ${PGDATA} (stopped; start.sh runs it)"
