#!/bin/bash
set -euo pipefail

[[ $# -eq 1 ]] || { echo "Usage: $0 <name>" >&2; exit 1; }

b="$(git branch --show-current)"
git switch "$1"
git merge "$b"
