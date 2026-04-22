---
name: aspire-postgres-primary-path
overview: Introduce .NET Aspire in a low-risk way while moving Gambol from file-authoritative persistence to PostgreSQL-primary persistence. Keep existing app files for app functionality, not as mirrored state recovery.
todos:
  - id: baseline-doc-alignment
    content: "Confirm and document the target authority model: Postgres primary, files remain app assets only."
    status: pending
  - id: aspire-bootstrap
    content: Add Aspire AppHost + ServiceDefaults and wire Gambol.Server with Postgres resource for local run parity.
    status: pending
  - id: db-primary-runtime
    content: Refactor server startup/persistence selection to DB-primary behavior with explicit failure modes.
    status: pending
  - id: tests-cutover
    content: Add or update tests for DB-primary semantics and startup behavior when DB is unavailable.
    status: pending
  - id: azure-parity
    content: Map local Aspire config shape to Azure deployment configuration and document a single operational model.
    status: pending
isProject: false
---

# .NET Aspire + Postgres Primary Plan

## Target Outcome

- Run the app as a single server service with PostgreSQL as the primary datastore in both local and Azure environments.
- Keep app files for normal app features/content only, not as persistence authority.
- Make local and Azure environments structurally consistent through Aspire-managed service wiring.

## Current Baseline (confirmed)

- Server switches persistence mode using `DB_CONNECTION_STRING` in `[C:/dev/amble/gambol/src/Server/Server.fs](C:/dev/amble/gambol/src/Server/Server.fs)`.
- PostgreSQL operations live in `[C:/dev/amble/gambol/src/Server/Database.fs](C:/dev/amble/gambol/src/Server/Database.fs)` and `[C:/dev/amble/gambol/src/Server/DbAgent.fs](C:/dev/amble/gambol/src/Server/DbAgent.fs)`.
- Documentation currently describes file-authority parity/rebuild behavior in `[C:/dev/amble/gambol/doc/postgres-migration.md](C:/dev/amble/gambol/doc/postgres-migration.md)` and `[C:/dev/amble/gambol/doc/persistence-vs-domain-model.md](C:/dev/amble/gambol/doc/persistence-vs-domain-model.md)`.

## Phase 1: Add Aspire AppHost For Local Parity (minimal server change)

- Explicit constraint: no intentional runtime/domain behavior change in this phase.
- Phase 1 changes only how dependencies are provisioned/wired (Aspire AppHost + injected
  DB connection config), not persistence authority semantics.
- Create Aspire orchestration projects (AppHost + ServiceDefaults) and wire `Gambol.Server` as a service.
- Add PostgreSQL as an Aspire resource and inject connection info into server config (compatible with existing `DB_CONNECTION_STRING` path first).
- Keep the server behavior otherwise stable to reduce migration risk while establishing a single local run path.
- Verify local startup, health, and dashboard visibility for server + postgres.

### Setup Expansion: Microsoft-First Bootstrap (local, clean, repeatable)

- Use .NET templates and Aspire tooling directly for setup:
  - `dotnet workload install aspire`
  - `dotnet new aspire-apphost -n Gambol.AppHost`
  - `dotnet new aspire-servicedefaults -n Gambol.ServiceDefaults`
- Add the two projects to the existing solution and reference `Gambol.ServiceDefaults`
  from the server project.
- In `Gambol.AppHost`, register:
  - PostgreSQL resource
  - database (logical) resource for Gambol data
  - `Gambol.Server` project with a reference to the database resource so Aspire injects
    connection information in the expected config shape.
- Keep server runtime reading from one canonical connection setting path first
  (`DB_CONNECTION_STRING` compatible), then move to stricter DB-required behavior in
  Phase 2.
- Run locally via AppHost as the default developer flow:
  - `dotnet run --project ./src/Gambol.AppHost`
  - confirm server starts, db is reachable, and health checks are green.

### Local State To Make Transferable To Another Dev Machine

- Commit to git:
  - AppHost and ServiceDefaults project files
  - solution changes
  - any checked-in config templates (`appsettings.Development.json` shape, not secrets)
  - README setup steps and required SDK/workload versions.
- Do not commit:
  - actual secrets
  - machine-specific certs
  - local database volume content.
- Standardize local prerequisites:
  - .NET SDK version via `global.json`
  - Aspire workload installation step in docs
  - Docker Desktop requirement (for local container-backed Postgres).
- Keep one command path for developers (`dotnet run --project ./src/Gambol.AppHost`) so
  setup and runtime behavior are consistent across clients.

### Azure Uploadability: What Reconfigures vs What Stays The Same

- Stays the same (code/topology intent):
  - `Gambol.Server` process model
  - DB-primary authority model
  - connection-setting key names expected by server
  - health-check behavior and startup failure policy.
- Requires Azure reconfiguration (environment binding):
  - replace local Aspire-managed Postgres container with Azure Database for PostgreSQL
    (or equivalent managed Postgres)
  - set production connection secret values in Azure configuration/Key Vault
  - configure hosting target for server (App Service or Container Apps) and networking
    (firewall/private access as needed)
  - set diagnostics/monitoring sinks (Application Insights, logging retention, alerts).
- Use Aspire Azure integration where useful, but treat it as deployment wiring; do not
  change core server persistence behavior between local and cloud.
- Define a parity checklist before go-live:
  - same env var names
  - same migration/startup sequence
  - same health endpoints
  - same fail-fast behavior when DB is unavailable.

## Phase 2: Shift To Postgres-Primary Runtime Behavior

- Refactor startup in `[C:/dev/amble/gambol/src/Server/Server.fs](C:/dev/amble/gambol/src/Server/Server.fs)` so DB-backed mode is the default/required production path.
- Remove or gate file-authority parity/rebuild logic so files are no longer treated as canonical persistence state.
- Define explicit startup failure behavior when DB is unavailable (fail fast instead of silent fallback).
- Keep file usage only where it belongs to app-level assets/content.

## Phase 3: Test-First Cutover Hardening

- Add/adjust server tests to prove DB-primary semantics (no implicit file-authority fallback).
- Keep DB integration tests keyed off test connection settings and ensure deterministic setup/teardown.
- Add tests for startup behavior when DB connection is missing/unhealthy.

## Phase 4: Azure Parity Mapping

- Map the same service topology to Azure (single app service + managed postgres or equivalent hosting choice), preserving configuration shape from AppHost.
- Standardize environment variable/secret names between local and Azure.
- Document one operational flow for local and cloud to avoid drift.

## Documentation Updates

- Update `[C:/dev/amble/gambol/doc/postgres-migration.md](C:/dev/amble/gambol/doc/postgres-migration.md)` with the new authority model (Postgres primary).
- Update `[C:/dev/amble/gambol/doc/persistence-vs-domain-model.md](C:/dev/amble/gambol/doc/persistence-vs-domain-model.md)` to reflect final ownership of persistence concerns.
- Add a short “run locally with Aspire” section to `[C:/dev/amble/gambol/README.md](C:/dev/amble/gambol/README.md)`.

## Risk Controls

- Keep migration incremental: orchestration first, authority flip second.
- Preserve rollback capability during cutover window by feature-gating mode selection until confidence is high.
- Avoid broad architecture changes unrelated to persistence authority and environment parity.

