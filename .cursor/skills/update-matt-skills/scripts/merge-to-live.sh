#!/usr/bin/env bash
# From a live w/* tip, land vendor merge on update/mattpocock-skills.
# Pass --bootstrap for first merge (--allow-unrelated-histories).
# Does not merge onto the shared w/* branch itself.
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
    echo "Checkout a live w/* branch first (on $branch)"
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

git checkout -B update/mattpocock-skills

if [ "$bootstrap" -eq 1 ]; then
  git merge --no-ff --allow-unrelated-histories vendor/mattpocock-skills
else
  git merge --no-ff vendor/mattpocock-skills
fi

echo "Merged vendor/mattpocock-skills into update/mattpocock-skills"
