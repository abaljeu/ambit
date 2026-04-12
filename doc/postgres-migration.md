# PostgreSQL (Gambol)

**Canonical design** — Target tables, file authority, startup parity, and migration goals are
specified in [`persistence-vs-domain-model.md`](persistence-vs-domain-model.md). Use that document
for intended `changes`, `graph`, `nodes`, and `node_children` shape.

**Current code** — `src/Server/Database.fs` and `src/Server/DbAgent.fs` still create `changes` and a
`snapshots` table (outline text checkpoint). That is legacy relative to the target doc and will be
replaced when the normalized projection and parity workflow are implemented.

**Running against Postgres** — Set `DB_CONNECTION_STRING` for the server. For automated DB tests,
set `TEST_DB_CONNECTION_STRING` (see `tests/Server.Tests/DbAgentTests.fs`).
