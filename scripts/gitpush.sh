#!/bin/bash
# Push ready or master (git protocol). Does not switch HEAD.
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

if [[ "$PLACE" == master ]]
then
    tip="$(git rev-parse master)"
    tip_tags=()
    while IFS= read -r name
    do
        [[ -n "$name" ]] && tip_tags+=("refs/tags/$name")
    done < <(git tag --points-at "$tip")
    if ((${#tip_tags[@]} > 0))
    then
        git push --force origin "${tip_tags[@]}"
    fi
fi

