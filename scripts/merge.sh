#!/bin/bash
# Merge entry for the git protocol (.cursor/skills/git-protocol/SKILL.md).
# The Desktop agent does not merge on its own; this runs by manual approval or by hand.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

MESSAGE=""

usage() {
    echo "Usage: $0 <command> [options]"
    echo "  ready              Merge dev into ready (--no-ff)."
    echo "  master -m <msg>    Squash ready onto master, then propagate forward."
    echo "  forward [place]    Propagate master (default) or ready toward dev."
    echo
    echo "A hotfix born on master or ready reaches dev with: $0 forward <place>"
    exit 1
}

require_place() {
    git rev-parse --verify --quiet "$1" >/dev/null \
        || { echo "No such place: $1" >&2; exit 1; }
}

require_clean() {
    git diff --quiet && git diff --cached --quiet \
        || { echo "Working tree is dirty. Commit on dev first." >&2; exit 1; }
}

# The published tip must already be in local ready, or two ready tips get mashed together.
require_ready_current() {
    if git rev-parse --verify --quiet origin/ready >/dev/null; then
        git merge-base --is-ancestor origin/ready ready \
            || { echo "Local ready is behind origin/ready. Pull ready first." >&2; exit 1; }
    fi
}

merge_no_ff() {
    local into="$1" from="$2"
    echo "==> $from into $into"
    git switch "$into"
    git merge --no-ff "$from"
}

forward_from() {
    local place="$1"
    case "$place" in
        master) merge_no_ff ready master; merge_no_ff dev ready ;;
        ready)  merge_no_ff dev ready ;;
        *)      echo "Propagate forward from master or ready, not $place" >&2; exit 1 ;;
    esac
}

[[ $# -gt 0 ]] || usage
COMMAND="$1"
shift

while [[ $# -gt 0 ]]; do
    case "$1" in
        -m) MESSAGE="${2:-}"; shift 2 ;;
        master|ready) PLACE="$1"; shift ;;
        *) usage ;;
    esac
done

require_place dev
require_place ready
require_place master
require_clean

case "$COMMAND" in
    ready)
        require_ready_current
        merge_no_ff ready dev
        git switch dev
        ;;
    master)
        [[ -n "$MESSAGE" ]] || { echo "A squash needs -m <msg>" >&2; exit 1; }
        require_ready_current
        echo "==> squash ready onto master"
        git switch master
        git merge --squash ready
        git commit -m "$MESSAGE"
        forward_from master
        ;;
    forward)
        forward_from "${PLACE:-master}"
        ;;
    *)
        usage
        ;;
esac

echo "==> Done. Now on $(git rev-parse --abbrev-ref HEAD)."
