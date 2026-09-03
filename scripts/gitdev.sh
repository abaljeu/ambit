#!/bin/bash
# Forward-merge master toward dev (dev).
# No dev or desc argument.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./_git-protocol.sh
source "$SCRIPT_DIR/_git-protocol.sh"

usage() {
    echo "Usage: $0"
    exit 1
}

[[ $# -eq 0 ]] || usage

require_places_clean
forward_from master
finish_on_dev
