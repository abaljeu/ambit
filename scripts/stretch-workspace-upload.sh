#!/usr/bin/env bash
# Drive desktop Upload minus WPF: WorkspaceCloudUpload + thin FSI.
set -euo pipefail
ROOT="$(dirname "$0")/.."
MARKER="${STRETCH_MARKER:-stretch-upload-$(date +%s)}"
LABEL="${STRETCH_LABEL:-stretch}"
FIXTURE="${STRETCH_FIXTURE:-/tmp/ambit-stretch-upload}"
AMBIT="${STRETCH_AMBIT:-http://127.0.0.1:5215/ambit}"
mkdir -p "$FIXTURE"
printf '%s\n' "$MARKER" > "$FIXTURE/hello.md"
echo "fixture=$FIXTURE/hello.md marker=$MARKER"
dotnet build "$ROOT/src/Shared/dotnet/Gambol.Shared.DotNet.fsproj" -v q \
  -p:CopyLocalLockFileAssemblies=true
OUT="$ROOT/src/Shared/dotnet/bin/Debug/net10.0"
dotnet fsi --nologo --lib:"$OUT" \
  -r:"$OUT/Gambol.Shared.dll" \
  -r:"$OUT/Gambol.Shared.Documents.dll" \
  -r:"$OUT/Gambol.Shared.DotNet.dll" \
  "$ROOT/scripts/stretch-workspace-upload.fsx" -- \
  "$AMBIT" "$FIXTURE" "$LABEL"
echo "STRETCH_MARKER=$MARKER"
