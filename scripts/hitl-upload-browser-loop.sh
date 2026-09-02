#!/usr/bin/env bash
# HITL loop for: local edit → Load/Upload → content appears in browser.
# Agent runs this; human answers in the terminal.
#
# Usage (from repo root):
#   scripts/hitl-upload-browser-loop.sh

set -euo pipefail

step() {
  printf '\n>>> %s\n' "$1"
  read -r -p "    [Enter when done] " _
}

capture() {
  local var="$1" question="$2" answer
  printf '\n>>> %s\n' "$question"
  read -r -p "    > " answer
  printf -v "$var" '%s' "$answer"
}

step "Open Gambol Desktop (or local proxy browser) and sign in."

capture FOCUS_KIND "What is focused before Load? (file|directory|workspace|other)"

capture FILE_REL "Relative path of the file you edited (e.g. doc/a.md):"

capture MARKER "Unique marker string you put in the local file (paste exact text):"

step "Run Load (Ctrl+Shift+>) once. Wait until status is idle (not Uploading/Loading)."

capture STATUS_DETAIL "What did the status/detail line say after Load? (paste or 'none')"

capture BROWSER_HAS_MARKER "Does the browser outline/body show your MARKER now? (y/n)"

capture REFRESHED "Did you hard-refresh the page after Load? (y/n)"

capture AFTER_REFRESH "If you refreshed: is MARKER visible after refresh+Load again? (y/n|n/a)"

printf '\n--- Captured ---\n'
printf 'FOCUS_KIND=%s\n' "$FOCUS_KIND"
printf 'FILE_REL=%s\n' "$FILE_REL"
printf 'MARKER=%s\n' "$MARKER"
printf 'STATUS_DETAIL=%s\n' "$STATUS_DETAIL"
printf 'BROWSER_HAS_MARKER=%s\n' "$BROWSER_HAS_MARKER"
printf 'REFRESHED=%s\n' "$REFRESHED"
printf 'AFTER_REFRESH=%s\n' "$AFTER_REFRESH"
