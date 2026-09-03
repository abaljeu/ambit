#!/bin/bash
# Shared helpers for gitready.sh, gitmaster.sh, and gitdev.sh. Not a public command.
if [[ "${BASH_SOURCE[0]}" == "$0" ]]
then
    echo "Internal helper; run gitready.sh, gitmaster.sh, or gitdev.sh." >&2
    exit 1
fi

_gp_script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$_gp_script_dir/.." && pwd)"
cd "$ROOT"

die() {
    echo "$1" >&2
    exit 1
}

require_place() {
    git rev-parse --verify --quiet "$1" >/dev/null \
        || die "No such place: $1"
}

require_clean() {
    git diff --quiet && git diff --cached --quiet \
        || die "Working tree is dirty. Commit on dev first."
}

# The published tip must already be in local ready, or two ready tips get mashed together.
require_ready_current() {
    if git rev-parse --verify --quiet origin/ready >/dev/null
    then
        git merge-base --is-ancestor origin/ready ready \
            || die "Local ready is behind origin/ready. Pull ready first."
    fi
}

merge_no_ff() {
    local into="$1" from="$2"
    local msg="${3-}"
    echo "==> $from into $into"
    git switch "$into"
    if [[ -n "$msg" ]]
    then
        git merge --no-ff -m "$msg" "$from"
    else
        git merge --no-ff "$from"
    fi
}

trees_match() {
    git diff --quiet "$1" "$2"
}

branches_aligned() {
    local base="$1"
    shift
    local branch
    for branch in "$@"
    do
        git merge-base --is-ancestor "$base" "$branch" || return 1
    done
}

finish_on_dev() {
    git switch dev
    echo "==> Done. Now on dev ($(git rev-parse --short HEAD))."
}

# forward_from master is gitdev.sh. ready-only is catch-up after ready moved.
forward_from() {
    local place="$1"
    case "$place" in
        master)
            merge_no_ff ready master "forward master into ready"
            merge_no_ff dev ready "forward ready into dev"
            ;;
        ready)
            merge_no_ff dev ready "forward ready into dev"
            ;;
        *)
            die "Propagate forward from master or ready, not $place"
            ;;
    esac
}

require_places_clean() {
    require_place dev
    require_place ready
    require_place master
    require_clean
}
