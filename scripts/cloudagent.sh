#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="Debug"
ACTION=""
PROJECT="src/CloudAgents.Console"

usage() {
    echo "Usage: $0 [action] [--debug|--release]"
    echo "  Actions: build, clean, run, watch (default: build)"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        build|clean|run|watch) ACTION="$1"; shift ;;
        --debug) CONFIG="Debug"; shift ;;
        --release) CONFIG="Release"; shift ;;
        *) usage ;;
    esac
done

[[ -z "$ACTION" ]] && ACTION="build"

case "$ACTION" in
    build)  echo "==> Building CloudAgents.Console ($CONFIG)..."; dotnet build "$PROJECT" -c "$CONFIG" ;;
    clean)  dotnet clean "$PROJECT" -c "$CONFIG" ;;
    run)    dotnet run --project "$PROJECT" -c "$CONFIG" --no-build ;;
    watch)  dotnet watch --project "$PROJECT" -c "$CONFIG" ;;
    *) usage ;;
esac