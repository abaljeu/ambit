#!/usr/bin/env bash
# Build command-dock.svg from icons in other/lucide/icons/
# Mappings: doc/reference/command-icon-index.md
# Run other/fetch-lucide.sh first if icons/ is missing.
set -euo pipefail
ICON_DIR="./other/lucide/icons"
OUT="./src/Server/wwwroot/command-dock.svg"
if [ ! -d "$ICON_DIR" ]; then
    echo "Missing $ICON_DIR — run ./other/fetch-lucide.sh first"
    exit 1
fi
icons=(
    "amb-icon-undo:undo-2"
    "amb-icon-redo:redo-2"
    "amb-icon-zoom-out:zoom-out"
    "amb-icon-zoom-in:zoom-in"
    # move-tools, select-tools, sel-*, move-*, *-to-* are custom — see demo index.html
    "amb-icon-find:search"
    "amb-icon-delete:trash-2"
    "amb-icon-jump:external-link"
    "amb-icon-more:ellipsis"
    "amb-icon-close:x"
    "amb-icon-palette:command"
    "amb-icon-copy:copy"
    "amb-icon-duplicate:copy-plus"
    "amb-icon-edit-classes:tags"
    "amb-icon-move-selected:move"
)
{
    echo '<?xml version="1.0" encoding="UTF-8"?>'
    echo '<!-- Icon paths from Lucide (ISC): other/lucide/ — see other/fetch-lucide.sh -->'
    echo '<svg xmlns="http://www.w3.org/2000/svg" style="display:none">'
    for pair in "${icons[@]}"; do
        id="${pair%%:*}"
        name="${pair##*:}"
        src="$ICON_DIR/$name.svg"
        if [ ! -f "$src" ]; then
            echo "Missing icon: $src" >&2
            exit 1
        fi
        echo "  <symbol id=\"$id\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">"
        grep -E '<(path|circle|line|polyline|polygon|rect|ellipse) ' "$src" | sed 's/^/    /'
        echo "  </symbol>"
    done
    echo '</svg>'
} > "$OUT"
echo "Wrote $OUT (${#icons[@]} symbols)"
