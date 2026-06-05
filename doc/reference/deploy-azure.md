# Deploying to Azure

## One-time setup (Azure Portal)

1. [portal.azure.com](https://portal.azure.com) → **Create a resource → Web App**
2. Runtime stack: **.NET 10**, OS: **Linux**
3. Under **Configuration → Application settings**, add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `Auth__Username` = your username
   - `Auth__Password` = your password
   - `WEBSITES_ENABLE_APP_SERVICE_STORAGE` = `true`
   - `Persistence__Mode` = `db` (production default; use `file` only for rollback/testing)
   - `DB_CONNECTION_STRING` = PostgreSQL connection string (see [[doc/reference/postgres-environments.md]])

4. Place **`appsettings.Production.json`** on the persistent **`/home`** mount (not only in the deployed zip) so config survives redeploys. The server loads it from `/home/appsettings.Production.json` on App Service ([[src/Server/Server.fs]]).

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

## Upload data (persistence mode)

Whether you need to seed files depends on **`Persistence:Mode`**:

### `db` mode (default production)

- **PostgreSQL is authority.** An empty database starts empty; the app does not import local `data/` files on startup.
- File upload to `/home/data` is **optional** — used only for backup/export artifacts the server may write, not as the source of truth.
- Provision and connect Azure Database for PostgreSQL Flexible Server per [[doc/reference/postgres-environments.md]].

### `file` mode (rollback / testing)

Seed the on-disk document (first deploy or migration):

1. Portal → your Web App → **Advanced Tools** → Go (opens Kudu)
2. **Debug console → CMD**
3. Navigate to `/home/data/` (server `DataDir` on Azure)
4. Drag and drop `gambol`, `gambol.log`, `gambol.meta` from your local `data/` folder

Data under `/home/data` persists across redeploys — the zip only overwrites `/home/site/wwwroot/`.

## URLs

| | |
|---|---|
| App | https://collaborative-systems.org/ambit |
| Login | https://collaborative-systems.org/ambit/login |

## Redirect from cPanel host

`/.htaccess` redirects `/ambit` (and subpaths) to Azure via proxy.php.
Upload `.htaccess` to the cPanel host root after any changes.

## Troubleshooting

- **Startup error in browser** — the server shows a `500` page with the full exception on startup failure.
- **Log stream** — Portal → Monitoring → Log stream for live stdout.
- **Restart** — Portal → Overview → Restart (needed after uploading data files in **file** mode via Kudu).
