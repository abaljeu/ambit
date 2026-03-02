# Deploying Gambol alongside WordPress (SSH access)

## Step 1: Install .NET 10 on the server

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

Copy `publish/` and `data/` to the server via `scp` or `rsync`:

```bash
rsync -av publish/ user@yourserver:/var/www/gambol/
rsync -av data/ user@yourserver:/var/www/gambol-data/
```

## Step 3: Create a systemd service

On the server, create `/etc/systemd/system/gambol.service`:

```ini
[Unit]
Description=Gambol ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=/var/www/gambol
ExecStart=/root/.dotnet/dotnet Gambol.Server.dll
Restart=always
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable gambol && sudo systemctl start gambol
```

## Step 4: Configure Nginx reverse proxy

Add a location block to your WordPress site's Nginx config:

```nginx
location /gambol/ {
    proxy_pass http://localhost:5000/gambol/;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
}
```

```bash
sudo nginx -s reload
```

## Step 5: Update appsettings.json

Set `DataDir` in `src/Server/appsettings.json` to the data directory on the server:

```json
{
  "DataDir": "/var/www/gambol-data"
}
```

## Notes

- Requires a VPS with systemd (not managed/shared hosting)
- Requires `sudo` access to edit Nginx config and manage services
- The app targets .NET 10; verify `dotnet --version` after install
