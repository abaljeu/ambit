#!/usr/bin/env bash
# Commit skills/ + .agents/skills on vendor. Plain message, no SHAs.
set -euo pipefail

branch=$(git rev-parse --abbrev-ref HEAD)
if [ "$branch" != "vendor/mattpocock-skills" ]; then
  echo "Must be on vendor/mattpocock-skills (on $branch)"
  exit 1
fi

git add skills .agents/skills

if git diff --cached --quiet; then
  echo "Nothing to commit"
  exit 0
fi

git commit -m "Update projected skills"

echo "Committed on vendor/mattpocock-skills"
