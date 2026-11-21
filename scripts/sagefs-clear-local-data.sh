#!/bin/bash
# POST /api/local-data/clear — unattended stand-in for manage_local_data.
# Usage: scripts/sagefs-clear-local-data.sh <friction|cohort|all>
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_sagefs-post.sh"

if [[ $# -ne 1 ]]; then
    echo "Usage: scripts/sagefs-clear-local-data.sh <friction|cohort|all>" >&2
    exit 2
fi

store="$1"
[[ "$store" =~ ^(friction|cohort|cohort-ledger|all)$ ]] \
    || { echo "store is friction, cohort, or all; got '${store}'." >&2; exit 2; }
sagefs_post "/api/local-data/clear?store=${store}" "{}"
