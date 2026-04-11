# PostgreSQL Migration Plan

## Status

- Phase 1 ✅ — DbAgent implemented, tests pass
- Phase 2 ⬜ — Local PG smoke test
- Phase 3 ⬜ — Validate endpoint test

---

## Files Changed (worktree → gambol/src/Server/)

| File | Change |
|---|---|
| `Database.fs` | **New** — Npgsql+Dapper helpers: `initSchema`, `appendChange` (stores `server_revision_after`), `getChangesAfterSnapshotRevision`, `insertSnapshot`, `getLatestSnapshot` |
| `DbAgent.fs` | **New** — PostgreSQL-backed `MailboxProcessor<FileAgentMsg>`; startup loads snapshot + replays changes |
| `Api.fs` | **Modified** — `AgentHandle` record abstraction; `AgentHandle.ofFile` / `AgentHandle.ofDb` factories |
| `Server.fs` | **Modified** — selects backend via `DB_CONNECTION_STRING` env var; dev-only `/ambit/validate` endpoint |
| `Gambol.Server.fsproj` | **Modified** — adds Npgsql 9.0.3 + Dapper 2.1.35; compile order: `Database.fs → DbAgent.fs` between `FileAgent.fs` and `Api.fs` |
| `tests/Server.Tests/DbAgentTests.fs` | **New** — `gambol_test` smoke test; skipped unless `TEST_DB_CONNECTION_STRING` is set |
| `tests/Server.Tests/AssemblyInfo.fs` | **New** — disables parallel test runs in this assembly (env var isolation) |
| `tests/Server.Tests/StateEndpointTests.fs` | **Modified** — clears `DB_CONNECTION_STRING` while starting `WebApplicationFactory` (file-backend tests) |
| `tests/Server.Tests/Gambol.Server.Tests.fsproj` | **Modified** — `Xunit.SkippableFact` package |

---

## Phase 2 — Local PostgreSQL Smoke Test

PostgreSQL 16.2 is installed at `D:/Program Files/PostgreSQL/16/bin/`.
Host: localhost:5432, superuser: postgres.

### Step 1 — Create app user and database

```bash
"D:/Program Files/PostgreSQL/16/bin/psql" -U postgres -h localhost
```

Inside psql:

```sql
CREATE USER gambol_dev WITH PASSWORD 'gambol_dev';
CREATE DATABASE gambol_dev OWNER gambol_dev;
GRANT ALL PRIVILEGES ON DATABASE gambol_dev TO gambol_dev;
\q
```

### Step 2 — Run the server with DB backend

```bash
export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=gambol_dev;Username=gambol_dev;Password=gambol_dev"
cd d:/dev/amble/gambol
dotnet run --project src/Server/Gambol.Server.fsproj
```

### Step 3 — Verify schema created

```bash
"D:/Program Files/PostgreSQL/16/bin/psql" -U gambol_dev -h localhost -d gambol_dev -c "\dt"
```

Expected: tables `changes` and `snapshots` listed.

### Step 4 — Verify writes

Make an edit in the app, then:

```bash
"D:/Program Files/PostgreSQL/16/bin/psql" -U gambol_dev -h localhost -d gambol_dev \
  -c "SELECT * FROM changes ORDER BY seq_id DESC LIMIT 5;"
```

Expected: rows appearing with `change_id`, `payload`, `recorded_at`.

### Step 5 — Verify restart restores state

Stop the server, restart with same `DB_CONNECTION_STRING`. Confirm app state matches what was there before restart.

---

## Phase 3 — Validate Endpoint

With `DB_CONNECTION_STRING` set AND a file snapshot present (i.e. the file backend has been used before), hit:

```bash
curl http://localhost:5000/ambit/validate
```

Expected response: `{"match":true}`

> **Note**: The `/ambit/validate` endpoint is dev-only (`ASPNETCORE_ENVIRONMENT=Development`).
> It instantiates both a FileAgent and DbAgent for `"gambol"` simultaneously. The FileAgent holds a FileStream lock on the `.log` file — this should be safe within the same process but worth watching for lock conflicts.

---

## Architecture Summary

### AgentHandle abstraction (Api.fs)

```
type AgentHandle = {
    getState    : unit -> Async<string>
    getRevision : unit -> Async<int>
    postChange  : string -> Async<Result<string,string>>
}
```

`AgentHandle.ofFile` wraps `FileAgent`, `AgentHandle.ofDb` wraps `DbAgent`. All three route handlers use `AgentHandle` — no direct agent references in route logic.

### Backend selection (Server.fs)

`getHandle` checks `Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")`:
- If set → `DbAgent.create connStr |> AgentHandle.ofDb`
- If null/empty → `FileAgent` (unchanged behaviour)

### DB Schema

```sql
CREATE TABLE changes (
    seq_id                 BIGSERIAL PRIMARY KEY,
    change_id              INT NOT NULL,
    server_revision_after  INT NOT NULL,
    payload                TEXT NOT NULL,
    recorded_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE snapshots (
    id          BIGSERIAL PRIMARY KEY,
    revision    INT NOT NULL,
    content     TEXT NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

`payload` is TEXT (not JSONB) to stay symmetric with the existing ChangeLog format (Thoth JSON strings). `content` in snapshots uses the same `Snapshot.write` format as the file snapshot.

`server_revision_after` is the server revision **after** applying that row (same index semantics as the file `.meta` + `.log` replay). `initSchema` adds the column on existing databases and backfills with `ROW_NUMBER() OVER (ORDER BY seq_id)` where it was null. Replay loads the latest snapshot, then applies changes with `server_revision_after` **greater than** the snapshot’s `revision`.

---

## Test database (`gambol_test`)

Automated DB tests in `tests/Server.Tests/DbAgentTests.fs` use a **separate** database so `TRUNCATE` never touches `gambol_dev` data. Create it once as the PostgreSQL superuser (`postgres`):

```sql
CREATE DATABASE gambol_test OWNER gambol_dev;
```

Run tests with `TEST_DB_CONNECTION_STRING` set (same host/user/password as dev, different database name):

```bash
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=gambol_test;Username=gambol_dev;Password=gambol_dev"
dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj
```

If `TEST_DB_CONNECTION_STRING` is unset, DB tests are **skipped** (other `Server.Tests` still run).

---

## Next Steps (Phase 4+)

- Azure PostgreSQL deployment (import production data)
- Remove `/ambit/validate` endpoint once parity is confirmed
- Consider switching `payload` column to JSONB for query flexibility
