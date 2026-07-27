#!/bin/bash
# AGENTS - don't use
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CONFIG="Debug"
ACTION=""
TARGET_URL=""
SKIP_CLIENT=""
PROJECT="$ROOT/src/Desktop"
CLIENT_PROJECT="$ROOT/src/Client"
CLIENT_OUT_DIR="$ROOT/src/Server/wwwroot"
SERVER_PROJECT="$ROOT/src/Server"
CLOUD_APP_URL="https://collaborative-systems.org/ambit"
LOCAL_APP_URL="http://localhost:5115/ambit"

usage() {
    echo "Usage: $0 [action] [--debug|--release] [--cloud|--local]"
    echo "  Actions: build, clean, run, run-local (default: build)"
    echo "  run        build client, run desktop; --cloud (default) or --local for proxy target"
    echo "  run-local  build client+server, start server, run desktop (server stops on exit)"
    echo "  --debug  (default) Debug configuration"
    echo "  --release  Release configuration"
    echo "  --cloud  proxy to production server (default for run)"
    echo "  --local  proxy to http://localhost:5115/ambit (run only)"
    echo "  --no-client  skip Fable build (run / run-local only)"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        build|clean|run|run-local)
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
        --cloud)
            TARGET_URL="$CLOUD_APP_URL"
            shift
            ;;
        --local)
            TARGET_URL="$LOCAL_APP_URL"
            shift
            ;;
        --no-client)
            SKIP_CLIENT=1
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
    npm run bundle
}

server_listening() {
    curl -sf -o /dev/null "$LOCAL_APP_URL" 2>/dev/null
}

wait_for_server() {
    echo "==> Waiting for server at $LOCAL_APP_URL ..."
    local i=0
    while [[ $i -lt 120 ]]; do
        if server_listening; then
            return 0
        fi
        sleep 1
        i=$((i + 1))
    done
    echo "Server did not become ready in time." >&2
    return 1
}

stop_local_server() {
    echo "==> Stopping Gambol.Server..."
    case "$(uname -s)" in
        MINGW* | MSYS* | CYGWIN*)
            taskkill //IM Gambol.Server.exe //F 2>/dev/null || true
            ;;
        *)
            pkill -f "Gambol.Server" 2>/dev/null || true
            ;;
    esac
}

run_local() {
    [[ -z "$SKIP_CLIENT" ]] && build_client

    echo "==> Building server..."
    dotnet build "$SERVER_PROJECT" -c "$CONFIG"

    echo "==> Starting local server..."
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5115 \
        dotnet run --project "$SERVER_PROJECT" -c "$CONFIG" --no-launch-profile &
    SERVER_PID=$!

    cleanup() {
        echo "==> Stopping local server (PID $SERVER_PID)..."
        kill "$SERVER_PID" 2>/dev/null || true
        wait "$SERVER_PID" 2>/dev/null || true
    }
    trap cleanup EXIT INT TERM

    wait_for_server

    echo "==> Starting desktop (local server)..."
    dotnet run --project "$PROJECT" -c "$CONFIG" -- --target "$LOCAL_APP_URL"
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
        [[ -z "$SKIP_CLIENT" ]] && build_client
        RUN_ARGS=()
        if [[ -n "$TARGET_URL" ]]; then
            RUN_ARGS+=(--target "$TARGET_URL")
        fi
        dotnet run --project "$PROJECT" -c "$CONFIG" -- "${RUN_ARGS[@]}"
        ;;
    run-local)
        run_local
        ;;
    *)
        usage
        ;;
esac
