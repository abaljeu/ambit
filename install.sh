#!/bin/sh
# Public Origin CLI installer, published to https://downloads.cursor.com/origin/install.sh
# and run as `curl -fsSL https://downloads.cursor.com/origin/install.sh | sh`.
#
# Its release data is baked in when a channel is promoted. It picks the build
# for this OS/arch, verifies its SHA256 before trusting the bytes, extracts the
# archive, and points ~/.local/bin/origin at a versioned install dir. Reruns are
# idempotent.
#
# This is the standalone public installer. It is distinct from the internal
# mise installer at packages/origin-cli/install.sh, which delegates to
# `mise install co` for employee and cloud-agent checkouts. Public users must
# never be asked to run mise.
#
# The release lane still stores artifacts under the co/ CDN prefix (the binary,
# per-version manifest, and channel pointers keep their original names). The
# rendered download URLs use co/; only the public entry point uses origin/.
set -eu

# ORIGIN_* is the documented name; CO_* is the legacy fallback `origin update`
# also reads, so a channel pinned for one is honored by both.
CHANNEL="${ORIGIN_INSTALL_CHANNEL:-${CO_INSTALL_CHANNEL:-stable}}"
if [ -n "${ORIGIN_INSTALL_CHANNEL:-}" ] || [ -n "${CO_INSTALL_CHANNEL:-}" ]; then
  channel_explicit=true
else
  channel_explicit=false
fi
case "$CHANNEL" in
latest | stable) ;;
*)
  echo "origin install: unknown channel '$CHANNEL' (expected 'latest' or 'stable')." >&2
  exit 1
  ;;
esac
# Accept the legacy CO_* names as a fallback so a custom location set for one of
# the installer or `origin update` is honored by both (update reads the same
# ORIGIN_*-then-CO_* order). ORIGIN_* wins when both are set.
INSTALL_DIR="${ORIGIN_INSTALL_DIR:-${CO_INSTALL_DIR:-$HOME/.local/share/cursor/origin}}"
BIN_DIR="${ORIGIN_BIN_DIR:-${CO_BIN_DIR:-$HOME/.local/bin}}"
CONFIG_FILE="$HOME/.config/origin-cli/config.json"

fail() {
  echo "origin install: $1" >&2
  exit 1
}

# --- Detect platform (must match the keys the release pipeline publishes) ---
os="$(uname -s)"
case "$os" in
Darwin)
  # sysctl, not `uname -m`: under Rosetta `uname -m` reports x86_64 on arm64.
  if [ "$(sysctl -n hw.optional.arm64 2>/dev/null || echo 0)" = "1" ]; then
    platform="darwin-arm64"
  else
    platform="darwin-x64"
  fi
  ;;
Linux)
  # glibc only. A glibc binary on musl fails at runtime with a confusing
  # loader error, so refuse up front.
  if ldd --version 2>&1 | grep -qi musl; then
    fail "musl libc is not supported yet (glibc only)."
  fi
  case "$(uname -m)" in
  x86_64 | amd64) platform="linux-x64" ;;
  aarch64 | arm64) platform="linux-arm64" ;;
  *) fail "unsupported architecture: $(uname -m)" ;;
  esac
  ;;
*) fail "unsupported OS: $os (mac and linux only)" ;;
esac

command -v curl >/dev/null 2>&1 || fail "curl is required."

case "$CHANNEL" in
latest)
  version="2026.09.03-12-04-37-b7e9da7"
  case "$platform" in
  darwin-arm64)
    url="https://downloads.cursor.com/co/2026.09.03-12-04-37-b7e9da7/darwin-arm64/co.tar.gz"
    sha="02193723369afb341768e0a4712358f4c80549a1710ca8834610132aaa450404"
    ;;
  darwin-x64)
    url="https://downloads.cursor.com/co/2026.09.03-12-04-37-b7e9da7/darwin-x64/co.tar.gz"
    sha="8b0ed0328321c7705dbd6f7ca7bec7e226c13587bb8b9025402782f5f36a4192"
    ;;
  linux-arm64)
    url="https://downloads.cursor.com/co/2026.09.03-12-04-37-b7e9da7/linux-arm64/co.tar.gz"
    sha="9a32e65179fae7932b0196f8a7e49612f8263b362e12ca0ce01a698830653c43"
    ;;
  linux-x64)
    url="https://downloads.cursor.com/co/2026.09.03-12-04-37-b7e9da7/linux-x64/co.tar.gz"
    sha="1d60e629ae21a198850dc682719c1f3050b3568164c6549fe8112db32b0855bb"
    ;;
  *) fail "no $platform build baked for $CHANNEL" ;;
  esac
  ;;
stable)
  version="2026.09.01-22-54-12-04810e3"
  case "$platform" in
  darwin-arm64)
    url="https://downloads.cursor.com/co/2026.09.01-22-54-12-04810e3/darwin-arm64/co.tar.gz"
    sha="0f0767345ee8d265ff28d56be2b4d6ce2ffec241788ba42b483d347233c95185"
    ;;
  darwin-x64)
    url="https://downloads.cursor.com/co/2026.09.01-22-54-12-04810e3/darwin-x64/co.tar.gz"
    sha="70d705496dd25542e90c7bc5e428611793b301e4511c4def6356628ba91ea0c9"
    ;;
  linux-arm64)
    url="https://downloads.cursor.com/co/2026.09.01-22-54-12-04810e3/linux-arm64/co.tar.gz"
    sha="40ceec1dd630b4325ad385a3d5533984c273ad73ca55a40d2dfdeb99b7d9abab"
    ;;
  linux-x64)
    url="https://downloads.cursor.com/co/2026.09.01-22-54-12-04810e3/linux-x64/co.tar.gz"
    sha="4cc9b1f90a400f8beb1b625e760c10eec1217a12f188a57ec723b071604a5d83"
    ;;
  *) fail "no $platform build baked for $CHANNEL" ;;
  esac
  ;;
esac

# --- Download + verify (SHA256 before we trust the bytes) ---
tmp="$(mktemp -d)"
staging=""
backup=""
trap 'rm -rf "$tmp" "${staging:-}" "${backup:-}"' EXIT
echo "Downloading origin $version ($platform)..."
# Do not follow redirects away from the baked artifact URL.
curl -fsSL --max-redirs 0 "$url" -o "$tmp/origin.tar.gz" || fail "download failed: $url"

if command -v sha256sum >/dev/null 2>&1; then
  echo "$sha  $tmp/origin.tar.gz" | sha256sum -c - >/dev/null 2>&1 || fail "SHA256 mismatch (expected $sha)"
elif command -v shasum >/dev/null 2>&1; then
  echo "$sha  $tmp/origin.tar.gz" | shasum -a 256 -c - >/dev/null 2>&1 || fail "SHA256 mismatch (expected $sha)"
else
  fail "no sha256sum/shasum available to verify the download."
fi

# --- Install: extract to a staging dir, verify, then swap into place so a
# failed download/extract/move never wipes a working install. ---
# This swap does not take the update.lock that `origin update` uses. A manual
# curl|sh overlapping an `origin update` on the same machine is a low-probability
# edge, and a correct shared POSIX-sh lock with stale-lock recovery would add
# disproportionate complexity and its own regression risk. Revisit if dogfooding
# shows real contention.
dest="$INSTALL_DIR/$version"
staging="$dest.incoming.$$"
backup="$dest.backup.$$"
rm -rf "$staging" "$backup"
mkdir -p "$staging"
# --no-same-owner: extract as the invoking user (never honor archived uid/gid).
# Modern tar already rejects `../`/absolute members; this makes the intent
# explicit on the curl|sh trust boundary.
tar --no-same-owner -xzf "$tmp/origin.tar.gz" -C "$staging"
# The archive still carries a legacy `co` hard link next to `origin` for
# pre-rename installs; the public installer intentionally exposes only `origin`.
# Reject a symlinked `origin` member: `-f` follows symlinks, so a malicious
# archive could point ~/.local/bin/origin at an attacker-chosen target. A hard
# link (the legacy `co`) is still a regular file and passes (mirrors the lstat
# check in `origin update`).
if [ -L "$staging/origin" ] || [ ! -f "$staging/origin" ]; then
  fail "archive did not contain a regular 'origin' binary"
fi
chmod +x "$staging/origin"
# Move any existing install aside first, put the new one in, then drop the
# backup. If the move fails (disk full, permissions), restore the prior install
# so a reinstall of the same version never leaves $dest missing.
if [ -e "$dest" ]; then
  mv "$dest" "$backup"
fi
if mv "$staging" "$dest"; then
  rm -rf "$backup"
  backup=""
else
  # Restore the prior install, and clear $backup either way so the EXIT trap
  # never deletes the only surviving copy when the restore itself fails.
  if [ -e "$backup" ] && mv "$backup" "$dest"; then
    backup=""
    fail "could not install origin $version to $dest (restored previous install)"
  else
    kept="$backup"
    backup=""
    fail "could not install origin $version to $dest; previous install kept at $kept"
  fi
fi

mkdir -p "$BIN_DIR"
ln -sfn "$dest/origin" "$BIN_DIR/origin"

# Preserve existing config through the CLI when the channel was explicit.
if [ "$channel_explicit" = true ]; then
  # Channel binaries can lag the main-pinned installer template.
  "$dest/origin" config set-channel "$CHANNEL" >/dev/null 2>&1 ||
    "$dest/origin" set-channel "$CHANNEL" >/dev/null 2>&1 ||
    echo "origin install: could not persist channel preference to config.json (non-fatal)" >&2
# A fresh default install needs only the literal initial config. Noclobber keeps
# a racing reinstall or symlink from overwriting an existing path.
elif [ ! -e "$CONFIG_FILE" ] && [ ! -L "$CONFIG_FILE" ]; then
  if ! mkdir -p "${CONFIG_FILE%/*}" 2>/dev/null || ! (
    umask 077
    set -C
    cat >"$CONFIG_FILE" <<EOF
{
  "channel": "$CHANNEL",
  "channelExplicit": false
}
EOF
  ); then
    echo "origin install: could not persist channel preference to config.json (non-fatal)" >&2
  fi
fi

echo "Installed origin $version to $BIN_DIR/origin"
case ":$PATH:" in
*":$BIN_DIR:"*) ;;
*) echo "Note: $BIN_DIR is not on your PATH. Add it, e.g.: export PATH=\"$BIN_DIR:\$PATH\"" ;;
esac
