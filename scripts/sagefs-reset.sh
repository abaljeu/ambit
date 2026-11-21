#!/bin/bash
# POST /reset — unattended stand-in for reset_fsi_session.
# Usage: scripts/sagefs-reset.sh <sessionId>
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_sagefs-post.sh"

if [[ $# -ne 1 ]]; then
    echo "Usage: scripts/sagefs-reset.sh <sessionId>" >&2
    exit 2
fi

id="$(sagefs_session_id "$1")"
sagefs_post "/reset" "{\"sessionId\":\"${id}\"}"
