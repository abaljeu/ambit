#!/usr/bin/env bash
# Merge vendor/mattpocock-skills into current live w/* branch.
# Pass --bootstrap for first merge (--allow-unrelated-histories).
set -euo pipefail

bootstrap=0
if [ "${1:-}" = "--bootstrap" ]; then
  bootstrap=1
fi

branch=$(git rev-parse --abbrev-ref HEAD)
case "$branch" in
  w/*)
    ;;
  *)
    echo "Must be on a live w/* branch (on $branch)"
    exit 1
    ;;
esac

if ! git rev-parse --verify vendor/mattpocock-skills >/dev/null 2>&1; then
  echo "Missing branch vendor/mattpocock-skills"
  exit 1
fi

if [ -n "$(git status --porcelain)" ]; then
  echo "Working tree not clean"
  exit 1
fi

if [ "$bootstrap" -eq 1 ]; then
  git merge --no-ff --allow-unrelated-histories vendor/mattpocock-skills
else
  git merge --no-ff vendor/mattpocock-skills
fi

echo "Merged vendor/mattpocock-skills into $branch"
