#!/bin/bash
# Squash ready onto master, then forward (git protocol).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./_git-protocol.sh
source "$SCRIPT_DIR/_git-protocol.sh"

usage() {
    echo "Usage: $0 \"<message>\""
    exit 1
}

if [[ $# -eq 0 ]]; then
    require_place master
    require_place ready
    if trees_match master ready
    then
        echo "==> ready and master already match; nothing to squash."
    else
        master_commit="$(git rev-parse master)"
        boundary=""
        while read -r commit first_parent other_parents
        do
            for parent in $other_parents
            do
                if [[ "$parent" == "$master_commit" ]]
                then
                    boundary="$commit"
                    break 2
                fi
            done
        done < <(git rev-list --first-parent --merges --parents ready)

        if [[ -n "$boundary" ]]
        then
            preview_count="$(
                git log --format=%H ready |
                    awk -v boundary="$boundary" '$1 == boundary && !found { print NR; found = 1 }'
            )"
            git log --oneline --max-count="$preview_count" ready
        else
            git log --oneline master..ready
        fi
    fi
    exit 0
fi

[[ $# -eq 1 ]] || usage
MESSAGE="$1"

require_places_clean
require_ready_current
if trees_match master ready; then
    echo "==> ready and master already match; nothing to squash."
    if branches_aligned master ready dev; then
        finish_on_dev
        exit 0
    fi
    echo "==> catch up branch ancestry"
    forward_from master
    finish_on_dev
    exit 0
fi
echo "==> squash ready onto master"
git switch master
git merge --squash ready
if git diff --cached --quiet; then
    echo "==> nothing to squash onto master." >&2
    finish_on_dev
    exit 1
fi
git commit -m "$MESSAGE"
forward_from master
finish_on_dev
