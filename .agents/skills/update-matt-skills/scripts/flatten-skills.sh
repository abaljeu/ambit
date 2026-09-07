#!/usr/bin/env bash
# Wipe .agents/skills and flatten skills/** into it. Must run on vendor.
set -euo pipefail

branch=$(git rev-parse --abbrev-ref HEAD)
if [ "$branch" != "vendor/mattpocock-skills" ]; then
  echo "Must be on vendor/mattpocock-skills (on $branch)"
  exit 1
fi

if [ ! -d skills ]; then
  echo "Missing skills/ — run pull-skills-tree.sh first"
  exit 1
fi

list=plan/update-matt-skills/flatten-list.txt
mkdir -p plan/update-matt-skills
: > "$list"

# List non-deprecated skill dirs as: name<TAB>path
for skill_md in skills/*/SKILL.md skills/*/*/SKILL.md skills/*/*/*/SKILL.md; do
  if [ ! -f "$skill_md" ]; then
    continue
  fi

  case "$skill_md" in
    skills/deprecated/*)
      continue
      ;;
  esac

  skill_dir=$(dirname "$skill_md")
  name=$(basename "$skill_dir")
  printf '%s\t%s\n' "$name" "$skill_dir" >> "$list"
done

sort -o "$list" "$list"

# Reject duplicate basenames before any copy.
prev=
# Walk sorted names; stop on first duplicate.
while IFS=$(printf '\t') read -r name skill_dir; do
  if [ "$name" = "$prev" ]; then
    echo "Duplicate skill name: $name"
    echo "Flatten aborted: duplicate basenames"
    exit 1
  fi
  prev=$name
done < "$list"

rm -rf .agents/skills
mkdir -p .agents/skills

# Copy each listed skill dir into the flat target.
while IFS=$(printf '\t') read -r name skill_dir; do
  cp -R "$skill_dir" ".agents/skills/$name"
done < "$list"

rm -f "$list"

count=0
# Count flat skill directories.
for d in .agents/skills/*; do
  if [ -d "$d" ]; then
    count=$((count + 1))
  fi
done

echo "Flattened $count skills into .agents/skills"
