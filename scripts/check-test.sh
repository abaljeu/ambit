#!/bin/bash
set -euo pipefail

pid="${1:?}"
log=""
ptr="/tmp/ambit-full-suite.${pid}.path"
if [[ -f "$ptr" ]]; then
    log="$(<"$ptr")"
fi

if ps -p "$pid" >/dev/null 2>&1; then
    echo "STILL_RUNNING"
else
    echo "DONE"
fi

if [[ -n "$log" ]]; then
    echo "$log"
fi
