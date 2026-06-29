# PostgreSQL Environment Management: Dev → Prod

Category: Operations
See also: [[doc/current/persistence-model.md]], [[doc/reference/deploy-azure.md]]

How to provision, configure, and operate PostgreSQL across local development, CI, and production for Gambol.

On desktop, db password is postgres/postgres.

## Current state

- **Code:** `Database.fs` and `DbAgent.fs` are implemented. PostgreSQL is the authority; legacy `Persistence:Mode` / `FileAgent` rollback hooks remain in code pending removal.
- **Schema:** Auto-created by `Database.initSchema` on startup (4 tables: `changes`, `graph`, `nodes`, `node_children`). No external migration tool.
- **Production:** Azure App Service (`Amble`) has a production PostgreSQL host: Azure Database for PostgreSQL Flexible Server `gambol-pg` in `Canada Central`, with database `gambol`. Network access from App Service to the DB has already been configured.
- **Dev:** Requires `DB_CONNECTION_STRING` and a running PostgreSQL instance (Docker Compose or native install below).

**Provider-neutral app contract:** The app only depends on `DB_CONNECTION_STRING` and startup schema logic in code. Cloud-specific work is limited to provisioning a PostgreSQL host and setting environment variables.

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

### PostgreSQL required

The server requires a working `DB_CONNECTION_STRING`. A missing or unreachable database is a startup error — the server does not fall back to file authority.

Correlated document artifacts under `DataDir` are written from DB state after accepted changes; they are not read at startup to rebuild graph state. See [[doc/current/persistence-model.md]].

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

## 4. Migration history (completed)

Phased rollout from flat-file authority to PostgreSQL-primary is complete.

### Phase 1: PostgreSQL host

1. Provision the production PostgreSQL instance (section 3). Completed.
2. Set `DB_CONNECTION_STRING` in production. Completed.

### Phase 2: Explicit modes and strict DB authority

- Implemented `db` / `file` persistence modes for rollout and rollback. Completed.
- Strict DB authority: startup loads from DB only; no silent file import. Completed.

### Phase 3: Production DB authority

- Production runs with PostgreSQL as authority. Completed.

### Current direction

- **Always database-backed** — no persistence mode switch.
- **Correlated files** — `DataDir` artifacts map to document nodes; DB edits auto-persist to disk.
- **Legacy cleanup** — remove `Persistence:Mode` / `FileAgent` file-authority path from server startup.

See [[doc/current/persistence-model.md]] and [[doc/roadmap/workspace-file-persistence.md]].

---

## 5. Backups

Automated backups are enabled by default (7-day retention, configurable up to 35 days). Point-in-time restore is available via the portal or CLI. For extra safety, schedule a `pg_dump` to Azure Blob Storage via a cron job or Azure Automation runbook.

### On-disk artifacts and PostgreSQL backups

Correlated document artifacts under `DataDir` are projections of DB state, not a separate authority or disaster-recovery path for the graph. PostgreSQL managed backups and `pg_dump` remain the database disaster-recovery path.

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
| `DB_CONNECTION_STRING` | Required | `gambol_test` DB on localhost | Azure Flexible Server |
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

A missing or unreachable database is a startup error. The server does not silently read or mutate a file-authority store.

### DigitalOcean fallback

If Azure costs are unfavorable after the first month, DigitalOcean Managed PostgreSQL is an alternative (~$15/month, no stop/start but simpler pricing). The app is provider-neutral — only the connection string and provisioning commands change. Evaluate after one billing cycle.

---

## 9. Checklist: first production DB deployment

- [x] Provision Azure Database for PostgreSQL – Flexible Server
- [x] Create `gambol` database and user
- [x] Configure network access (firewall rules or VNet)
- [x] Set `DB_CONNECTION_STRING` in Azure App Service environment variables
- [x] Deploy the application with PostgreSQL authority
- [ ] Spot-check DB contents via `psql`
- [ ] Confirm auto-persist to correlated `DataDir` artifacts works
- [ ] Confirm backups are running
