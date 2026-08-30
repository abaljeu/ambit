#!/usr/bin/env bash
# Extract vendored lucide-static npm package into other/lucide/icons/
# Package: lucide-static (ISC) https://github.com/lucide-icons/lucide
set -euo pipefail
ROOT="./other"
TGZ=$(ls "$ROOT"/lucide-static-*.tgz 2>/dev/null | sort -V | tail -1)
if [ -z "$TGZ" ]; then
    echo "No other/lucide-static-*.tgz found. Run: npm pack lucide-static --pack-destination ./other"
    exit 1
fi
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
tar -xzf "$TGZ" -C "$TMP"
rm -rf "$ROOT/lucide/icons"
mkdir -p "$ROOT/lucide"
mv "$TMP/package/icons" "$ROOT/lucide/icons"
cp "$TMP/package/LICENSE" "$ROOT/lucide/LICENSE"
cp "$TMP/package/README.md" "$ROOT/lucide/README.md"
echo "Extracted $(find "$ROOT/lucide/icons" -name '*.svg' | wc -l) icons from $(basename "$TGZ")"
