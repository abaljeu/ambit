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

case "$ACTION" in
    build)
        echo "==> Building client (Fable)..."
        dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
        echo "==> Building server ($CONFIG)..."
        dotnet build src/Server -c "$CONFIG"
        ;;
    clean)
        dotnet clean src/Server -c "$CONFIG"
        ;;
    run)
        echo "==> Building client (Fable)..."
        dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
        dotnet run --project src/Server -c "$CONFIG"
        ;;
    watch)
        echo "==> Building client (Fable)..."
        dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
        dotnet watch run --project src/Server -c "$CONFIG"
        ;;
    *)
        usage
        ;;
esac
