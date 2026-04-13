# PostgreSQL (Gambol)

**Canonical design** — Tables, file authority, startup parity, and rebuild behavior are specified in
[`persistence-vs-domain-model.md`](persistence-vs-domain-model.md).

**Current code** — `src/Server/Database.fs` maintains append-only `changes` and a normalized
projection: singleton `graph`, `nodes`, `node_children`. The legacy `snapshots` SQL table is
dropped on `initSchema` (outline blob checkpoints are not used in PostgreSQL).

**Running against Postgres** — Set `DB_CONNECTION_STRING` for the server. For automated DB tests,
set `TEST_DB_CONNECTION_STRING` (see `tests/Server.Tests/DbAgentTests.fs`).
