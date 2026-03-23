#!/usr/bin/env bash
# Convert file(s) to CRLF line endings.
# Usage: crlf.bash [file(s)]

set -e

if [[ $# -eq 0 ]]; then
  echo "Usage: $0 <file> [file ...]" >&2
  exit 1
fi

for f in "$@"; do
  if [[ -f "$f" ]]; then
    tmp=$(mktemp)
    # Strip existing CR, then add CR before each LF (LF -> CRLF)
    sed 's/\r$//;s/$/\r/' "$f" > "$tmp" && mv "$tmp" "$f"
  else
    echo "Not a file: $f" >&2
    exit 1
  fi
done
