# PostgreSQL Environment Management: Dev → Prod

How to provision, configure, and operate PostgreSQL across local development, CI, and production for Gambol.

---
on desktop, db password is postgres/postgres

## Current state

- **Code:** `Database.fs`, `DbAgent.fs`, and `Persistence:Mode` in `DatabaseSetup` / `Server.fs` are implemented: `db` is the default strict PostgreSQL authority; `file` keeps the file-authority rollback path.
- **Schema:** Auto-created by `Database.initSchema` on startup (4 tables: `changes`, `graph`, `nodes`, `node_children`). No external migration tool.
- **Production:** Azure App Service (`Amble`) has a production PostgreSQL host: Azure Database for PostgreSQL Flexible Server `gambol-pg` in `Canada Central`, with database `gambol`. Network access from App Service to the DB has already been configured.
- **Dev:** Use `Persistence:Mode=file` for local file-authority work without PostgreSQL. Use the default `db` mode with `DB_CONNECTION_STRING` when developing the DB-authority path.

## Decision now

- **PostgreSQL is Step 1.** We will provision production PostgreSQL and make the authority mode
  explicit before relying on DB authority.
- **Provider-neutral app contract:** The app only depends on `DB_CONNECTION_STRING` and startup schema/parity logic in code. Cloud-specific work is limited to provisioning a PostgreSQL host and setting environment variables.

---

## 1. Local development

### Option A: Docker Compose

Add a `docker-compose.dev.yml` at the repo root:

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: gambol
      POSTGRES_PASSWORD: gambol_dev
      POSTGRES_DB: gambol
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

Start it:

```bash
docker compose -f docker-compose.dev.yml up -d
```

Connection string for local dev:

```
Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev
```

Set in your shell before running the server:

```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev"
dotnet watch run --project src/Server
```

Or in a `.env` file (gitignored):

```
DB_CONNECTION_STRING=Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev
```

### Option B: Native install

Install PostgreSQL 17 via the Windows installer or `winget install PostgreSQL.PostgreSQL`. Create a `gambol` database and user with `createdb` / `psql`. Same connection string as above.

### Running without Postgres

Set `Persistence:Mode=file` and omit `DB_CONNECTION_STRING`. The server uses `FileAgent` and flat
files in `data/`. This remains fully supported.

Default `db` mode requires a working `DB_CONNECTION_STRING`. It should show a startup error rather
than silently falling back to files.

### Dev file mode and DB mirroring

When `Persistence:Mode=file` and `DB_CONNECTION_STRING` is set, files are still the API authority.
If the DB is empty, startup may seed it from the loaded file state. Successful file writes mirror to
DB when it is available.

When `Persistence:Mode=db`, startup does not import files. An empty DB stays empty unless an explicit
operator import/migration is run.

---

## 2. Testing

### Automated DB tests

`tests/Server.Tests/DbAgentTests.fs` runs against a real PostgreSQL instance gated on `TEST_DB_CONNECTION_STRING`.

```powershell
$env:TEST_DB_CONNECTION_STRING = "Host=localhost;Database=gambol_test;Username=gambol;Password=gambol_dev"
dotnet test tests/Server.Tests
```

Use a separate `gambol_test` database so test runs (which may drop/recreate tables) don't disturb your dev data.

### CI

If CI is added later, use a PostgreSQL service container:

```yaml
# GitHub Actions example
services:
  postgres:
    image: postgres:17
    env:
      POSTGRES_USER: gambol
      POSTGRES_PASSWORD: gambol_ci
      POSTGRES_DB: gambol_test
    ports:
      - 5432:5432
    options: >-
      --health-cmd pg_isready
      --health-interval 5s
      --health-timeout 5s
      --health-retries 5
```

---

## 3. Production

**Host:** Azure Database for PostgreSQL – Flexible Server (Burstable B1ms, 1 vCPU / 2 GB, ~$13–25/month). Managed backups, patching, and point-in-time restore included.

**Why this now:** This is the fastest path to execute Step 1 (production PostgreSQL) without changing deployment architecture.

### Setup

#### Provision (Azure CLI)

```bash
RESOURCE_GROUP=Amble_group
PG_SERVER=gambol-pg
PG_ADMIN_USER=gambol_admin
PG_ADMIN_PASSWORD=<generate-strong-password>
LOCATION="Canada Central"

az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $PG_SERVER \
  --location $LOCATION \
  --admin-user $PG_ADMIN_USER \
  --admin-password "$PG_ADMIN_PASSWORD" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 17 \
  --public-access 0.0.0.0   # allow Azure services; lock down further below

az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $PG_SERVER \
  --database-name gambol
```

**Current recorded result:** `gambol-pg.postgres.database.azure.com` exists, the `gambol` database exists, and Azure created an allow-Azure firewall rule during provisioning.

#### Network access

- Allow only the App Service's outbound IPs (find under App Service → Properties → Outbound IP Addresses).
- Or use VNet integration + private endpoint if both resources are in the same VNet.

```bash
# Allow specific App Service outbound IPs
for IP in $(az webapp show -g $RESOURCE_GROUP -n Amble --query outboundIpAddresses -o tsv | tr ',' ' '); do
  az postgres flexible-server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --name $PG_SERVER \
    --rule-name "appservice-${IP//./-}" \
    --start-ip-address $IP \
    --end-ip-address $IP
done
```

**Current recorded result:** Network access from Azure App Service (`Amble`) to the PostgreSQL server has already been configured well enough for Step 1 to proceed.

#### Connection string

```
Host=gambol-pg.postgres.database.azure.com;Database=gambol;Username=gambol_admin;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

#### Set in App Service

```bash
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name Amble \
  --settings DB_CONNECTION_STRING="Host=gambol-pg.postgres.database.azure.com;Database=gambol;Username=gambol_admin;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
```

To avoid storing the password in shell history or committed docs, use the helper script:

```powershell
./scripts/set-azure-db-connection-string.ps1
```

The script prompts for the password interactively, builds the connection string, and writes the `DB_CONNECTION_STRING` app setting to Azure App Service.

**Future note:** We may want to move `DB_CONNECTION_STRING` fully out of `appsettings.Production.json` and rely on Azure App Service settings for the secret instead. That would make password rotation cleaner and keep secrets out of the production config file. If we do that, verify configuration precedence in startup so App Service environment variables win over the production JSON file.

---

## 4. Migration sequence (file-only → explicit modes → DB-primary)

This is the phased rollout, not a one-shot cutover.

### Phase 1: PostgreSQL host

1. Provision the production PostgreSQL instance (section 3). Completed.
2. Set `DB_CONNECTION_STRING` in production. Completed.

### Phase 2: Implement explicit `db` and `file` modes

- Rename the internal file-first implementation mode to `File`, exposed as config value `"file"`.
- Make `""` and `"db"` resolve to strict DB authority.
- In `file` mode, keep startup file import into an empty DB and keep DB mirroring.
- In `db` mode, do not call `DocumentLoader.loadState` or `rebuildFromDocumentFiles` on startup.
- Add a periodic disk backup from DB state only.

### Phase 3: File mode mirror verification

- Deploy in `Persistence:Mode=file`.
- On first startup, the server finds an empty DB, runs `initSchema`, and calls
  `rebuildFromDocumentFiles` to populate it from the existing flat files.
- Files remain the source of truth and DB is maintained as a mirror/projection.
- **Verify:** After startup, spot-check a few nodes in the DB via `psql` to confirm parity.

### Phase 4: Production DB authority

- Ensure production DB has the intended graph. If needed, run an explicit import while still in
  `file` mode.
- Set production to `Persistence:Mode=db` or omit the mode because `db` is the default.
- Verify `/ambit/state` comes from DB and that the periodic file-format backup appears in `DataDir`.
- **Trigger:** Explicit operator decision that DB contents are correct and should be authoritative.

---

## 5. Backups

Automated backups are enabled by default (7-day retention, configurable up to 35 days). Point-in-time restore is available via the portal or CLI. For extra safety, schedule a `pg_dump` to Azure Blob Storage via a cron job or Azure Automation runbook.

### During file mirror and DB authority modes

In `file` mode, the flat files in `data/` remain the authoritative backup path and DB mirror source.
In `db` mode, the server writes periodic file-format backups from DB state only. PostgreSQL managed
backups and `pg_dump` remain the real database disaster-recovery path.

---

## 6. Schema evolution

`Database.initSchema` uses `CREATE TABLE IF NOT EXISTS`, so it's safe to run on every startup. For future schema changes:

1. **Additive changes** (new columns with defaults, new tables): Add to `initSchema` with `IF NOT EXISTS` / `ADD COLUMN IF NOT EXISTS`. These are safe to run on existing databases.
2. **Breaking changes** (column renames, type changes): Write a one-time migration function in `Database.fs` that runs at startup, gated on a schema-version check. Keep it simple — no migration framework needed for a single-user app.
3. **Testing migrations:** Run the migration against a copy of the production database locally before deploying.

---

## 7. Environment variable summary

| Variable | Dev | Test | Production |
|----------|-----|------|------------|
| `Persistence:Mode` | `file` for file work, default `db` for DB work | set per test | `db` for DB authority, `file` for rollback |
| `DB_CONNECTION_STRING` | Required for `db`, optional for `file` mirror | `gambol_test` DB on localhost | Azure Flexible Server |
| `TEST_DB_CONNECTION_STRING` | `gambol_test` DB on localhost | Same | Not set (don't run tests in prod) |
| `DataDir` | `../../data` (default) | `../../data` | `/home/site/data` (Azure App Service) |

---

## 8. Cost management: start/stop automation

Provisioning the database starts a recurring cost (~$13–25/month on Azure Burstable). While actively
developing or testing DB persistence, a start/stop system keeps costs near zero during idle periods.

### Azure: stop/start the Flexible Server

Azure Flexible Server supports stop/start. A stopped server incurs no compute charges (only storage, ~$3.84/month for 32 GB).

```bash
# Stop (no compute charges while stopped)
az postgres flexible-server stop \
  --resource-group $RESOURCE_GROUP \
  --name $PG_SERVER

# Start
az postgres flexible-server start \
  --resource-group $RESOURCE_GROUP \
  --name $PG_SERVER
```

Repository helper:

```bash
./scripts/azure.sh start
./scripts/azure.sh stop
./scripts/azure.sh status
./scripts/azure.sh web
./scripts/azure-postgres-restop.sh
```

`./scripts/azure.sh web` restarts App Service `Amble` only (same `resource-group` default as below).
After a successful `start` or `stop`, the same script also runs `az webapp restart` for `Amble`, so
the ASP.NET process does not keep stale database connections.

Azure auto-restarts a stopped server after 7 days. That 7-day limit is Azure behavior, not a setting
you can change in the portal or on the `az postgres flexible-server stop` command. To keep the server
off longer, set up an Azure Automation runbook, Task Scheduler job, cron job, or other scheduled call
that re-runs the stop command before the 7-day limit.

### App behavior when DB is down

In `file` mode, the app handles a missing database gracefully: it uses `FileAgent` and skips DB
mirroring until the next startup with the DB running.

In default `db` mode, a missing database is a startup error. This is intentional: strict DB authority
must not silently read or mutate the old file store.

### DigitalOcean fallback

If Azure costs are unfavorable after the first month, DigitalOcean Managed PostgreSQL is an alternative (~$15/month, no stop/start but simpler pricing). The app is provider-neutral — only the connection string and provisioning commands change. Evaluate after one billing cycle.

---

## 9. Checklist: first production DB deployment

- [x] Provision Azure Database for PostgreSQL – Flexible Server
- [x] Create `gambol` database and user
- [x] Configure network access (firewall rules or VNet)
- [x] Set `DB_CONNECTION_STRING` in Azure App Service environment variables
- [x] Implement explicit `db` / `file` persistence modes
- [x] Deploy the application
- [x] Verify `file` mode startup can seed empty DB from files
- [x] Verify `db` mode startup does not import files into an empty DB
- [ ] Spot-check DB contents via `psql`
- [ ] Confirm periodic DB-to-disk backup works in `db` mode
- [ ] Confirm backups are running
