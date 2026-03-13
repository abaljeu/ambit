# Deploying to Azure

## One-time setup (Azure Portal)

1. [portal.azure.com](https://portal.azure.com) → **Create a resource → Web App**
2. Runtime stack: **.NET 10**, OS: **Linux**
3. Under **Configuration → Application settings**, add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `Auth__Username` = your username
   - `Auth__Password` = your password
   - `WEBSITES_ENABLE_APP_SERVICE_STORAGE` = `true`

## Build and deploy

```powershell
# 1. Build Fable client into wwwroot
cd src/Client
dotnet fable . -o ../Server/wwwroot
cd ../..

# 2. Publish server
dotnet publish src/Server -c Release -o ./publish

# 3. Zip
Compress-Archive -Path ./publish/* -DestinationPath ./site.zip -Force
```

Then deploy via Kudu:

1. Portal → your Web App → **Advanced Tools** → Go
2. **Tools → Zip Push Deploy**
3. Drag and drop `site.zip` onto the page

## Upload data (first deploy only)

Existing data files live in `data/` and are not included in the zip.
To seed Azure with your local data:

1. Portal → your Web App → **Advanced Tools** → Go (opens Kudu)
2. **Debug console → CMD**
3. Navigate to `/home/site/data/`
4. Drag and drop `gambol`, `gambol.log`, `gambol.meta` from your local `data/` folder

Data persists across redeploys — the zip only overwrites `/home/site/wwwroot/`.

## URLs

| | |
|---|---|
| App | https://collaborative-systems.org/amble |
| Login | https://collaborative-systems.org/login |

## Redirect from cPanel host

`/.htaccess` redirects `/gambol` (and `/amble`) to Azure.
Upload `.htaccess` to the cPanel host root after any changes.

## Troubleshooting

- **Startup error in browser** — the server shows a `500` page with the full exception on startup failure.
- **Log stream** — Portal → Monitoring → Log stream for live stdout.
- **Restart** — Portal → Overview → Restart (needed after uploading data files via Kudu).
