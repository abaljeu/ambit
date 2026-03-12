#!/bin/bash
APP_DIR="$HOME/gambol"
LOG="$APP_DIR/gambol.log"
PIDFILE="$APP_DIR/gambol.pid"

if [ -f "$PIDFILE" ] && kill -0 "$(cat $PIDFILE)" 2>/dev/null; then
    exit 0  # already running
fi

cd "$APP_DIR"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet"
# Allow running a net7 app on hosts with newer runtimes installed (for example, .NET 10 only).
export DOTNET_ROLL_FORWARD="LatestMajor"
export ASPNETCORE_URLS="http://localhost:5000"
export ASPNETCORE_ENVIRONMENT="Production"

nohup dotnet Gambol.Server.dll >> "$LOG" 2>&1 &
echo $! > "$PIDFILE"
sleep 2

if ! kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
    echo "Gambol failed to start; recent log output:"
    tail -n 40 "$LOG" || true
    rm -f "$PIDFILE"
    exit 1
fi
