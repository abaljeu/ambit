#!/usr/bin/env bash
# Drive desktop Upload minus WPF: Core Changes + WorkspaceFileSync.post.
set -euo pipefail
ROOT="$(dirname "$0")/.."
MARKER="${STRETCH_MARKER:-stretch-upload-$(date +%s)}"
LABEL="${STRETCH_LABEL:-stretch}"
FIXTURE="${STRETCH_FIXTURE:-/tmp/ambit-stretch-upload}"
AMBIT="${STRETCH_AMBIT:-http://127.0.0.1:5215/ambit}"
mkdir -p "$FIXTURE"
printf '%s\n' "$MARKER" > "$FIXTURE/hello.md"
echo "fixture=$FIXTURE/hello.md marker=$MARKER"
dotnet run --project "$ROOT/scripts/stretch-workspace-upload" -- "$AMBIT" "$FIXTURE" "$LABEL"
echo "STRETCH_MARKER=$MARKER"
