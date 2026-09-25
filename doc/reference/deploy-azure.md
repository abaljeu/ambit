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
   - `DefaultAiKey` = `cursor`
   - `AiKeys:cursor` = the production Cursor API key
   - `grokbot:WakeUrl` = the grokbot wake URL
   - `grokbot:WakeSecret` = the grokbot wake secret
   - `grokbot:InboundSecret` = the Ambit inbound secret

   An Azure App Setting writes a colon as a double underscore (`AiKeys:cursor` is set as `AiKeys__cursor`).

   Put the production Cursor key in the App Setting `AiKeys:cursor`. That setting is an environment variable. [[src/Server/AiKeys.fs]] binds `AiKeys` as a name-keyed map. The logical name is `cursor`. `DefaultAiKey` selects that name and is `cursor`. Development user-secrets use the same colon names: `AiKeys:cursor`, `DefaultAiKey`, `grokbot:WakeUrl`, `grokbot:WakeSecret`, and `grokbot:InboundSecret`.

   This secrets strategy owns `grokbot:WakeUrl` and `grokbot:WakeSecret`. `grokbot:InboundSecret` is Ambit Server config. For the first slice, store `grokbot:InboundSecret` in the same App Settings. Secret names, environments, and stores are in [[doc/reference/secrets.md]].

4. Place **`appsettings.Production.json`** on the persistent **`/home`** mount (not only in the deployed zip) so config survives redeploys. The server loads it from `/home/appsettings.Production.json` on App Service ([[src/Server/Server.fs]]). Keep non-secret settings in that file. Do not put `AiKeys`, `DefaultAiKey`, or `grokbot` in that file. Extra `/home` JSON loads after `CreateBuilder`. The server then re-adds environment variables so App Settings `DefaultAiKey` and `AiKeys__cursor` win over empty or stale JSON with the same names.

   Add Azure Key Vault references later, when there are many secrets. Do not add Key Vault references in this setup.

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
4. Drag and drop the `.amb` network, `gambol.log`, and `gambol.meta` from your local `data/` folder

Data under `/home/data` persists across redeploys — the zip only overwrites `/home/site/wwwroot/`.

## URLs

| | |
|---|---|
| App | https://collaborative-systems.org/ambit |
| Login | https://collaborative-systems.org/ambit/login |

## Custom domain (cPanel host)

Production URL: `https://collaborative-systems.org/ambit`. The cPanel server transparently forwards `/ambit` to Azure; it does not run Gambol. Implementation: [[doc/reference/cpanel-transparent-proxy.md]].

## Troubleshooting

- **Startup error in browser** — the server shows a `500` page with the full exception on startup failure.
- **Log stream** — Portal → Monitoring → Log stream for live stdout.
- **Restart** — Portal → Overview → Restart (needed after uploading data files in **file** mode via Kudu).
