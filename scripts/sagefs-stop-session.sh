#!/bin/bash
# POST /api/sessions/stop — unattended stand-in for stop_session.
# Usage: scripts/sagefs-stop-session.sh <sessionId>
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_sagefs-post.sh"

if [[ $# -ne 1 ]]; then
    echo "Usage: scripts/sagefs-stop-session.sh <sessionId>" >&2
    exit 2
fi

id="$(sagefs_session_id "$1")"
sagefs_post "/api/sessions/stop" "{\"sessionId\":\"${id}\"}"
