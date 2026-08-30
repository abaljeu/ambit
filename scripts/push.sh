#!/bin/bash
# Push entry for the git protocol (.cursor/skills/git-share/SKILL.md).
# Publishes ready or master; dev is never pushed.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

usage() {
    echo "Usage: $0 <ready|master>"
    exit 1
}

[[ $# -eq 1 ]] || usage

PLACE="$1"
case "$PLACE" in
    dev)
        echo "dev is not published; use ready or master." >&2
        exit 1
        ;;
    ready|master)
        ;;
    *)
        usage
        ;;
esac

git rev-parse --verify --quiet "$PLACE" >/dev/null \
    || { echo "No such branch: $PLACE" >&2; exit 1; }

git push origin "$PLACE"
