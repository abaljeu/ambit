#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CONFIG="Debug"
ACTION=""
PROJECT="$ROOT/src/Desktop"
CLIENT_PROJECT="$ROOT/src/Client"
CLIENT_OUT_DIR="$ROOT/src/Server/wwwroot"

usage() {
    echo "Usage: $0 [action] [--debug|--release]"
    echo "  Actions: build, clean, run (default: build)"
    echo "  --debug  (default) Debug configuration"
    echo "  --release  Release configuration"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        build|clean|run)
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

[[ -z "$ACTION" ]] && ACTION="build"

build_client() {
    echo "==> Building client..."
    dotnet fable "$CLIENT_PROJECT" --outDir "$CLIENT_OUT_DIR" --sourceMaps
}

case "$ACTION" in
    build)
        build_client
        echo "==> Building desktop ($CONFIG)..."
        dotnet build "$PROJECT" -c "$CONFIG"
        ;;
    clean)
        dotnet clean "$PROJECT" -c "$CONFIG"
        ;;
    run)
        build_client
        dotnet run --project "$PROJECT" -c "$CONFIG"
        ;;
    *)
        usage
        ;;
esac
