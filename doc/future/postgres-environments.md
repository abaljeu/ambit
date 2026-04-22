# PostgreSQL Environment Management: Dev → Prod

How to provision, configure, and operate PostgreSQL across local development, CI, and production for Gambol.

---
on desktop, db password is postgres/postgres

## Current state

- **Code:** `Database.fs` and `DbAgent.fs` are complete. The server switches between file-only (`FileAgent`) and PostgreSQL (`DbAgent`) based on the `DB_CONNECTION_STRING` env var.
- **Schema:** Auto-created by `Database.initSchema` on startup (4 tables: `changes`, `graph`, `nodes`, `node_children`). No external migration tool.
- **Production:** Azure App Service running file-only mode at `collaborative-systems.org`. No PostgreSQL instance exists yet.
- **Dev:** File-only by default. DB tests exist (`DbAgentTests.fs`) gated on `TEST_DB_CONNECTION_STRING`.

## Decision now

- **PostgreSQL is Step 1.** We will provision production PostgreSQL now and run dual-write.
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

Omit `DB_CONNECTION_STRING` entirely — the server falls back to `FileAgent` and flat files in `data/`. This remains fully supported.

### Dev → file parity

When `DB_CONNECTION_STRING` is set, startup compares the file-derived graph to the DB projection. On mismatch the DB is rebuilt from files. This means you can freely switch between file-only and DB modes during development without data loss.

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
LOCATION=eastus   # match your App Service region

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

---

## 4. Migration sequence (file-only → dual-write → DB-primary)

This is the phased rollout, not a one-shot cutover.

### Phase 1: PostgreSQL-first dual-write (execute now)

1. Provision the production PostgreSQL instance (section 3).
2. Set `DB_CONNECTION_STRING` in production.
3. Deploy. On first startup the server finds an empty DB, runs `initSchema`, and calls `rebuildFromDocumentFiles` to populate it from the existing flat files.
4. Files remain the source of truth. The DB is a projection.
5. **Verify:** After startup, spot-check a few nodes in the DB via `psql` to confirm parity.

### Phase 2: Confidence period

- Run dual-write in production for a period (weeks).
- Monitor the startup parity check — it should report "DB matches files" every restart.
- If it ever reports a mismatch and triggers a rebuild, investigate the cause before proceeding.

### Phase 3: DB becomes primary (future)

- Flip the authority flag so the DB is the source of truth and files become the projection (or are dropped).
- This requires a code change (not yet written) and is out of scope for now.
- **Trigger:** Zero mismatches over a sustained period, plus merge/sync features relying on DB queries.

---

## 5. Backups

Automated backups are enabled by default (7-day retention, configurable up to 35 days). Point-in-time restore is available via the portal or CLI. For extra safety, schedule a `pg_dump` to Azure Blob Storage via a cron job or Azure Automation runbook.

### During dual-write period

The flat files in `data/` are already backed up (the `.bak.*` files visible in the workspace). These serve as an independent backup of the DB contents throughout Phase 1–2.

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
| `DB_CONNECTION_STRING` | Optional (omit for file-only) | `gambol_test` DB on localhost | Azure Flexible Server |
| `TEST_DB_CONNECTION_STRING` | `gambol_test` DB on localhost | Same | Not set (don't run tests in prod) |
| `DataDir` | `../../data` (default) | `../../data` | `/home/site/data` (Azure App Service) |

---

## 8. Cost management: start/stop automation

Provisioning the database starts a recurring cost (~$13–25/month on Azure Burstable). During Steps 0–1, the database is only needed when actively developing or testing dual-write, not 24/7. A start/stop system keeps costs near zero during idle periods.

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

Azure auto-restarts a stopped server after 7 days. To prevent this, set up an Azure Automation runbook or a scheduled `az` CLI call to re-stop it if it's not needed.

### App behavior when DB is down

The app already handles a missing database gracefully: if the connection fails at startup, it falls back to `FileAgent` and flat-file mode (provided `DB_CONNECTION_STRING` is unset or the connection times out). During the dual-write period, files are the source of truth anyway, so a stopped database just means parity checks are skipped until the next startup with the DB running.

### DigitalOcean fallback

If Azure costs are unfavorable after the first month, DigitalOcean Managed PostgreSQL is an alternative (~$15/month, no stop/start but simpler pricing). The app is provider-neutral — only the connection string and provisioning commands change. Evaluate after one billing cycle.

---

## 9. Checklist: first production DB deployment

- [ ] Provision Azure Database for PostgreSQL – Flexible Server
- [ ] Create `gambol` database and user
- [ ] Configure network access (firewall rules or VNet)
- [ ] Set `DB_CONNECTION_STRING` in Azure App Service environment variables
- [ ] Deploy the application
- [ ] Verify startup logs: schema created, `rebuildFromDocumentFiles` completed
- [ ] Spot-check DB contents via `psql`
- [ ] Confirm backups are running
- [ ] Monitor parity checks on subsequent restarts
