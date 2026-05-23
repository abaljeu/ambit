# PostgreSQL (Gambol)

**Canonical design** — Tables and the `db` / `file` authority split are specified in
[[doc/roadmap/persistence-vs-domain-model.md]].

**Current code** — [[src/Server/Database.fs]] maintains append-only `changes` and a normalized
projection: singleton `graph`, `nodes`, `node_children`. The legacy `snapshots` SQL table is dropped
on `initSchema` (outline blob checkpoints are not used in PostgreSQL).

**Planned mode behavior** — `db` is the default strict authority. It requires a working
`DB_CONNECTION_STRING`, does not import files on startup, and periodically writes a disk backup from
DB state. `file` is the explicit file-authority mode: files are primary, an empty DB may be seeded
from files, and successful writes mirror to DB when available.

**Running against Postgres** — In `db` mode, set `DB_CONNECTION_STRING` for the server. In `file`
mode, `DB_CONNECTION_STRING` is optional and enables mirroring. For automated DB tests, set
`TEST_DB_CONNECTION_STRING` (see [[tests/Server.Tests/DbAgentTests.fs]]).

