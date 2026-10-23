#!/bin/bash
set -euo pipefail

if ps -p "${1:?}" >/dev/null 2>&1; then
    echo "STILL_RUNNING"
else
    echo "DONE"
fi
