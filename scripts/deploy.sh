#!/bin/bash
set -euo pipefail

SSH_KEY=~/.ssh/collab-sys.rsa
REMOTE=abaljeu@collaborative-systems.org

ARCHIVE=publish.tar.gz

cleanup() {
  [ -f "$ARCHIVE" ] && rm -f "$ARCHIVE" && echo "Cleaned up local $ARCHIVE."
  ssh-agent -k 2>/dev/null && echo "SSH agent stopped." || true
}
trap cleanup EXIT

echo "==> Starting SSH agent and loading key (you will be prompted once for the passphrase)..."
eval "$(ssh-agent -s)"
ssh-add "$SSH_KEY"
echo "    Key loaded."

echo "==> Building client (Fable)..."
dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps
echo "    Client build complete."

echo "==> Publishing server..."
dotnet publish src/Server -c Release -o ./publish
echo "    Server publish complete."

echo "==> Creating release archive..."
tar -czf "$ARCHIVE" -C publish .
echo "    Archive created: $ARCHIVE ($(du -h "$ARCHIVE" | cut -f1))."

echo "==> Uploading archive to $REMOTE..."
scp "$ARCHIVE" "$REMOTE":~/"$ARCHIVE"
echo "    Upload complete."

echo "==> Uploading start script..."
ssh "$REMOTE" "mkdir -p ~/gambol"
sed 's/\r//' scripts/start.sh | ssh "$REMOTE" "cat > ~/gambol/start.sh"
echo "    start.sh uploaded."

echo "==> Deploying on remote..."
ssh "$REMOTE" <<'ENDSSH'
set -euo pipefail

APP_DIR=~/gambol
PIDFILE="$APP_DIR/gambol.pid"
STAGE="$APP_DIR.stage"
HEALTH_URL="http://localhost:5000/login"
HEALTH_TIMEOUT=30

echo "  -> Staging release..."
mkdir -p "$APP_DIR" "$STAGE"
tar -xzf ~/publish.tar.gz -C "$STAGE"
rm -f ~/publish.tar.gz
[ -f "$APP_DIR/start.sh" ] && cp "$APP_DIR/start.sh" "$STAGE/start.sh"

echo "  -> Stopping running instance (if any)..."
if [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
  kill -TERM "$(cat "$PIDFILE")" 2>/dev/null || true
  for i in $(seq 1 10); do
    kill -0 "$(cat "$PIDFILE")" 2>/dev/null || break
    sleep 1
  done
  if kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
    kill -KILL "$(cat "$PIDFILE")" 2>/dev/null || true
    sleep 1
  fi
  rm -f "$PIDFILE"
  echo "  -> Stopped."
else
  echo "  -> No running instance found."
fi

echo "  -> Swapping release..."
rm -rf "$APP_DIR.old"
mv "$APP_DIR" "$APP_DIR.old"
mv "$STAGE" "$APP_DIR"
chmod +x "$APP_DIR/start.sh"

echo "  -> Starting app..."
"$APP_DIR/start.sh"

echo "  -> Health check..."
for i in $(seq 1 $HEALTH_TIMEOUT); do
  if curl -sSf -o /dev/null -w "%{http_code}" "$HEALTH_URL" 2>/dev/null | grep -qE '^([23][0-9]{2}|200)$'; then
    echo "  -> App healthy (HTTP 200)."
    exit 0
  fi
  sleep 1
done
echo "  -> Health check failed; app may not be ready."
tail -n 30 "$APP_DIR/gambol.log" 2>/dev/null || true
exit 1
ENDSSH

echo "==> Deployment complete."
