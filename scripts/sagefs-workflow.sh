#!/bin/bash
# POST /api/sessions/<id>/workflow — unattended stand-in for switch_workflow.
# Usage: scripts/sagefs-workflow.sh <sessionId> <workflow>
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_sagefs-post.sh"

if [[ $# -ne 2 ]]; then
    echo "Usage: scripts/sagefs-workflow.sh <sessionId> <interactive|hotreload|livetesting>" >&2
    exit 2
fi

id="$(sagefs_session_id "$1")"
workflow="$2"
[[ "$workflow" =~ ^(interactive|repl|normal|hotreload|weblive|live|web|livetesting|live-testing|testing|test)$ ]] \
    || { echo "workflow is interactive, hotreload, or livetesting (aliases accepted); got '${workflow}'." >&2; exit 2; }
sagefs_post "/api/sessions/${id}/workflow" "{\"workflow\":\"${workflow}\"}"
