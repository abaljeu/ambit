#!/bin/bash
# Shared POST for the SageFS unattended routes. Sourced by scripts/sagefs-*.sh.
# See .agents/skills/shell-scripts/SKILL.md.
set -euo pipefail

sagefs_port() {
    local port="${SAGEFS_MCP_PORT:-37749}"
    [[ "$port" =~ ^[0-9]+$ ]] || { echo "SAGEFS_MCP_PORT is a port number; got ${port}." >&2; exit 2; }
    printf '%s' "$port"
}

sagefs_session_id() {
    local id="${1:-}"
    [[ "$id" =~ ^[0-9a-f]{8}$ ]] || { echo "Session id is 8 lowercase hex characters; got '${id}'." >&2; exit 2; }
    printf '%s' "$id"
}

sagefs_post() {
    local path="$1"
    local body="$2"
    local port
    port="$(sagefs_port)"
    curl -sS -m 20 -X POST "http://127.0.0.1:${port}${path}" \
        -H "Content-Type: application/json" \
        -d "$body"
    printf '\n'
}
