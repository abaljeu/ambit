#!/bin/bash
# Bring dev (dev) into ready (git protocol).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./_git-protocol.sh
source "$SCRIPT_DIR/_git-protocol.sh"

usage() {
    echo "Usage: $0 \"<message>\""
    exit 1
}

[[ $# -eq 1 ]] || usage
MESSAGE="$1"

require_places_clean
require_ready_current
if trees_match dev ready && branches_aligned dev ready
then
    echo "==> dev already on ready."
    finish_on_dev
    exit 0
fi
merge_no_ff ready dev "$MESSAGE"
finish_on_dev
