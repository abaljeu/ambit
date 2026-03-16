#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="Debug"
ACTION=""

usage() {
    echo "Usage: $0 [action] [--debug|--release]"
    echo "  Actions: build, clean, run, watch (default: watch)"
    echo "  --debug  (default) Debug configuration"
    echo "  --release  Release configuration"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        build|clean|run|watch)
            ACTION="$1"
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

[[ -z "$ACTION" ]] && ACTION="watch"

FABLE_OUT="src/Server/wwwroot"

case "$ACTION" in
    build)
        echo "==> Building client (Fable)..."
        dotnet fable src/Client --outDir "$FABLE_OUT" --sourceMaps
        ;;
    clean)
        echo "==> Cleaning Fable output..."
        rm -rf "$FABLE_OUT"
        ;;
    run)
        echo "==> Building client (Fable)..."
        dotnet fable src/Client --outDir "$FABLE_OUT" --sourceMaps
        echo "Client built. Serve via the server (e.g. ./scripts/server.sh run)."
        ;;
    watch)
        echo "==> Watching client (Fable)..."
        dotnet fable watch src/Client --outDir "$FABLE_OUT" --sourceMaps
        ;;
    *)
        usage
        ;;
esac
