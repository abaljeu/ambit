# Deploying Gambol alongside WordPress (SSH access)

## [x] Step 1: Install .NET 10 on the server

```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc
```

## Step 2: Publish the app (from Windows)

```bash
dotnet publish src/Server -c Release -o ./publish
```

Copy `publish/` to the server using `scripts/deploy.sh` (zips first to avoid per-file overhead):

```bash
bash scripts/deploy.sh
```

For data (only needed on first deploy or if data files changed):

```bash
scp -i ~/.ssh/collab-sys.rsa -r data/ abaljeu@collaborative-systems.org:~/gambol-data/
```

## Step 3: Keep the app running (shared cPanel hosting — no systemd)

cPanel shared hosting has no `sudo`, systemd, or direct Nginx access.
Instead, use a wrapper script + cron job.

**Create `~/gambol/start.sh`** on the server:

```bash
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
export ASPNETCORE_URLS="http://localhost:5000"
export ASPNETCORE_ENVIRONMENT="Production"

nohup dotnet Gambol.Server.dll >> "$LOG" 2>&1 &
echo $! > "$PIDFILE"
```

```bash
chmod +x ~/gambol/start.sh
~/gambol/start.sh
```

**Add a cron job** in cPanel → Cron Jobs (or via `crontab -e`):

```
*/5 * * * * ~/gambol/start.sh
```

This restarts the app if it crashes or the server reboots. Logs go to `~/gambol/gambol.log`.

## Step 4: Configure reverse proxy via .htaccess

cPanel uses Apache. Add these rules to `~/.htaccess` (the root htaccess that serves the main site), before `# BEGIN WordPress`:

```apache
RewriteEngine On

# Proxy /ambit to the Gambol app
RewriteRule ^ambit(/.*)?$ http://localhost:5000/ambit$1 [P,L]
```

Or using `ProxyPass` directives (place before `# BEGIN WordPress`):

```apache
<IfModule mod_proxy.c>
    ProxyPass /ambit http://localhost:5000/ambit
    ProxyPassReverse /ambit http://localhost:5000/ambit
</IfModule>
```

The app serves all routes under `/ambit` — main UI, login, logout, state, changes, user.css.

If neither works, contact the host to confirm `mod_proxy` / `mod_rewrite [P]` is enabled.

## Step 5: Update appsettings.json

Set `DataDir` in `src/Server/appsettings.json` to the data directory on the server:

```json
{
  "DataDir": "/var/www/gambol-data"
}
```

## Notes

- Server is cPanel shared hosting (Apache, no systemd, no sudo)
- Home directory is `/home3/abaljeu/`; WordPress docs likely under `~/public_html/`
- 5GB disk available; .NET runtime (~300–500MB) + app (~50MB) fits comfortably
- The app targets .NET 10; verify `dotnet --version` after install
- `mod_proxy` support on Apache shared hosts varies — may need to ask host to enable it
- The cron-based restart is best-effort; the host may kill long-running processes
