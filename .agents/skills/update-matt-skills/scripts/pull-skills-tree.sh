#!/usr/bin/env bash
# Replace skills/ from skills-source/main. Must run on vendor/mattpocock-skills.
set -euo pipefail

branch=$(git rev-parse --abbrev-ref HEAD)
if [ "$branch" != "vendor/mattpocock-skills" ]; then
  echo "Must be on vendor/mattpocock-skills (on $branch)"
  exit 1
fi

if ! git remote get-url skills-source >/dev/null 2>&1; then
  echo "Missing remote skills-source"
  exit 1
fi

git fetch skills-source

if [ -d skills ]; then
  rm -rf skills
fi

git checkout skills-source/main -- skills/

echo "Pulled skills/ from skills-source/main"
