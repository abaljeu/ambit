#!/bin/bash
# POST /hard-reset — unattended stand-in for hard_reset_fsi_session.
# Usage: scripts/sagefs-hard-reset.sh <sessionId> [true|false]
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_sagefs-post.sh"

if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: scripts/sagefs-hard-reset.sh <sessionId> [true|false]" >&2
    exit 2
fi

id="$(sagefs_session_id "$1")"
rebuild="${2:-true}"
[[ "$rebuild" == "true" || "$rebuild" == "false" ]] || { echo "rebuild is true or false; got '${rebuild}'." >&2; exit 2; }
sagefs_post "/hard-reset" "{\"sessionId\":\"${id}\",\"rebuild\":${rebuild}}"
