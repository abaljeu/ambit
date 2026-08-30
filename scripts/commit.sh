#!/bin/bash
# Commit entry for the git protocol (.cursor/skills/git-protocol/SKILL.md).
# Ordinary commits on dev; the Desktop agent uses this or the human types git commit.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

usage() {
    echo "Usage: $0 \"<message>\""
    exit 1
}

[[ $# -gt 0 ]] || usage

head="$(git rev-parse --abbrev-ref HEAD)"
[[ "$head" == "dev" ]] \
    || { echo "Commits belong on dev; HEAD is $head. Switch to dev first." >&2; exit 1; }

git add .
git commit -m "$1"
