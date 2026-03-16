#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="Debug"
TARGETS=""

usage() {
    echo "Usage: $0 [--debug|--release] [server|shared|all]"
    echo "  Targets: server, shared, all (default: all)"
    echo "  --debug  (default) Debug configuration"
    echo "  --release  Release configuration"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        server|shared|all)
            TARGETS="$1"
            shift
            ;;
        --debug)
            CONFIG="Debug"
            shift
            ;;
        --release)
            CONFIG="Release"
            shift
            ;;
        *)
            usage
            ;;
    esac
done

[[ -z "$TARGETS" ]] && TARGETS="all"

run_tests() {
    local project="$1"
    echo "==> Running $project tests ($CONFIG)..."
    dotnet test "tests/$project.Tests" -c "$CONFIG" --no-build
}

case "$TARGETS" in
    server)
        dotnet build tests/Server.Tests -c "$CONFIG"
        run_tests "Server"
        ;;
    shared)
        dotnet build tests/Shared.Tests -c "$CONFIG"
        run_tests "Shared"
        ;;
    all)
        dotnet build tests/Server.Tests -c "$CONFIG"
        dotnet build tests/Shared.Tests -c "$CONFIG"
        run_tests "Server"
        run_tests "Shared"
        ;;
    *)
        usage
        ;;
esac

echo "==> All tests passed."
