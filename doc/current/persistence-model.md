# Persistence model (Graph / Node)

Category: Persistence
See also: [[doc/current/sync-mvp.md]], [[doc/arch.md]], [[doc/reference/postgres-environments.md]], [[doc/roadmap/workspace-file-persistence.md]]

How Gambol persists the graph: PostgreSQL is always the source of truth; on-disk files under `DataDir` correlate with database nodes and are written automatically from accepted DB state.

## Principles

1. **PostgreSQL authority** — The server always runs database-backed. Startup initializes the schema and loads graph state from PostgreSQL only. An empty DB stays empty; startup does not silently import from disk.

2. **Correlated files** — A directory tree under `DataDir` holds persisted artifacts that map to graph nodes (workspace, directory, and file document roots). File paths and membership follow the rules in [[doc/roadmap/workspace-file-persistence.md]].

3. **DB-to-disk auto-persist** — Each accepted change commits to PostgreSQL first. The server then writes or updates the correlated on-disk artifacts for affected documents. Disk is a projection of DB state, not a separate authority or startup input.

4. **No outline blobs in PostgreSQL** — Do not store the file snapshot's **line-oriented outline syntax** as the graph source of truth in SQL (no monolithic `Snapshot.write` text as the projection). That syntax exists only in the **file** layer (`src/Shared/Snapshot.fs`).

5. **Relational schema mirrors `Model.fs` fields** — Tables and columns reflect domain records (`Node`, child lists, `Ownership`, etc.). They do **not** mirror outline indentation or line grammar. **`Graph.parentByChild`** and **`Graph.ownerParentByChild`** are **derived** in code from nodes and child rows (same as `Graph.fromNodes` in `Model.fs`); they need not be stored as separate tables if the node and child-edge data are complete.

6. **Append-only change log in PostgreSQL** — Rows in the `changes` table record one persisted `Change` per accepted batch (`payload` = full change JSON). Replay uses `server_revision_after` against the `graph.revision` checkpoint.

## Running against PostgreSQL

[[src/Server/Database.fs]] maintains append-only `changes` and a normalized projection: singleton `graph`, `nodes`, `node_children`. The legacy `snapshots` SQL table is dropped on `initSchema` (outline blob checkpoints are not used in PostgreSQL).

- **`DB_CONNECTION_STRING`** is required.
- Startup loads from the DB only; correlated files are not read to rebuild graph state.
- After each accepted change, the server updates the DB projection and auto-persists affected document artifacts under `DataDir`.

For automated DB tests, set `TEST_DB_CONNECTION_STRING` (see [[tests/Server.Tests/DbAgentTests.fs]]). Environment setup: [[doc/reference/postgres-environments.md]].

---

## Domain types (reference)

From `src/Shared/Model.fs` (abbreviated):

- **`NodeId`** — `Guid` (use `UUID` in PostgreSQL).
- **`Node`** — `id`, `text`, `name` (`string option`), `kind` (`NodeKind`: `Normal` or `Special` of `File` / `Directory` / `Workspace` / system kinds), `children` (`ChildNode list`), `cssClasses` (ordered list of class names; see `CssClass.fs`).
- **`ChildNode`** — `ref` (`Ownership`), `id` (`NodeId`).
- **`Ownership`** — `Owner` | `Ref` (whether the child list holds the owning edge or a reference).
- **`Graph`** — `root` (`NodeId`), `nodes` (`Map<NodeId, Node>`), plus derived maps `parentByChild`, `ownerParentByChild` computed from the node map and child lists.

The canonical root id is fixed: `Graph.rootId` (`Guid.Empty`).

---

## On-disk artifacts (correlated with nodes)

Disk files are **projections** of DB state, keyed to document roots in the graph. They are written after successful DB commits and are not read at startup to rebuild the graph.

| Concern | Role |
|---------|------|
| **Document artifacts** | Outline or payload text per workspace, directory, or file document root, under `DataDir/{workspaceLabel}/...`. Path layout, membership, incremental writes, and path moves: [[doc/roadmap/workspace-file-persistence.md]]. |
| **Outline syntax** | Tab-indented lines (optional `{...}` class meta) via `Snapshot.read` / `Snapshot.write` in [[src/Shared/Snapshot.fs]]. Serialization stops at nested document roots. |

Parity between disk and DB is defined on **`Graph`** and **revision**, not on matching raw outline text byte-for-byte to SQL rows.

---

## PostgreSQL schema

Auto-created by `Database.initSchema` on startup (four tables). No external migration tool.

### `changes` — append-only log

One row per persisted client change; `payload` is the same JSON string concept as a historical `.log` line (full `Change`, including `ops`).

```sql
CREATE TABLE changes (
    seq_id                 BIGSERIAL PRIMARY KEY,
    change_id              INT            NOT NULL,
    server_revision_after  INT            NOT NULL,
    payload                TEXT           NOT NULL,
    recorded_at            TIMESTAMPTZ    NOT NULL DEFAULT now()
);

CREATE INDEX idx_changes_server_revision_after
    ON changes (server_revision_after);
```

`server_revision_after` is the server revision **after** applying that row; replay uses rows with this value greater than the stored checkpoint revision (see `src/Server/Database.fs`).

### `graph` — `Graph.root` plus server revision (singleton)

`Model.Graph` has `root` and `nodes` (and derived maps). There is no `document_name` in the model; the server holds one outline at a time (e.g. file `gambol`). Persist **`Graph.root`** as `root_id`. **`revision`** is not on `Graph`; it matches `Revision` and tracks the log replay boundary alongside the node projection.

```sql
CREATE TABLE graph (
    singleton   SMALLINT PRIMARY KEY DEFAULT 1 CHECK (singleton = 1),
    root_id     UUID        NOT NULL,
    revision    INT         NOT NULL
);
```

### `nodes` — one row per `Node`

Columns map **`Model.Node`** except **`children`**, which is normalized into `node_children`.

| Column        | Type   | `Model.Node` field |
|---------------|--------|--------------------|
| `id`          | `UUID` | `id` (`NodeId`)    |
| `text`        | `TEXT` | `text`             |
| `name`        | `TEXT` | `name` (nullable)  |
| `kind`        | `TEXT` | `kind` (`normal`, `file`, `directory`, `workspace`, `workspaces`, `trash`) |
| `css_classes` | `JSONB` or `TEXT[]` | `cssClasses` (ordered class names) |

```sql
CREATE TABLE nodes (
    id              UUID        PRIMARY KEY,
    text            TEXT        NOT NULL,
    name            TEXT        NULL,
    kind            TEXT        NOT NULL DEFAULT 'normal',
    css_classes     JSONB       NOT NULL
);
```

`css_classes` stores the same ordered list as `CssClasses` (e.g. JSON `["amb-row-owned"]`).

### `node_children` — `Node.children` as rows

Each row is one **`Model.ChildNode`** in **`parent_id`'s** `children` list, in list order. **`child_id`** is `ChildNode.id`. **`ownership`** maps **`ChildNode.ref`** (`Ownership`): `'owner'` ↔ `Owner`, `'ref'` ↔ `Ref`.

```sql
CREATE TABLE node_children (
    parent_id   UUID        NOT NULL,
    ordinal     INT         NOT NULL,
    child_id    UUID        NOT NULL,
    ownership   TEXT        NOT NULL CHECK (ownership IN ('owner', 'ref')),
    PRIMARY KEY (parent_id, ordinal)
);

CREATE INDEX idx_node_children_child ON node_children (child_id);
```

Foreign keys `parent_id` → `nodes(id)` and `child_id` → `nodes(id)` are recommended (bulk rebuild may need deferred constraints or insert order).

**Rebuilding `Graph`:** read the singleton `graph` row for `root_id`. Load all `nodes`, build a map `NodeId → Node` with **`children = []`**. Load `node_children`, sort by `(parent_id, ordinal)`, append `{ ref = …; id = … }` to each parent's `children` list. Call **`Graph.fromNodes root_id`** with the completed `Map<NodeId, Node>` so **`parentByChild`** and **`ownerParentByChild`** match in-memory derivation.

---

## Server implementation

([[src/Server/DatabaseSetup.fs]], [[src/Server/Server.fs]], [[src/Server/DbAgent.fs]]):

- **`Database.initSchema`** — creates **`changes`**, **`graph`**, **`nodes`**, **`node_children`**; drops legacy **`snapshots`** if present.
- **`DbAgent`** — loads projection + replays `changes` tail; each accepted change appends a row and updates projection in one transaction.
- **Auto-persist to correlated files** — after a successful DB commit, write or update document artifacts under `DataDir` for affected document roots (see [[doc/roadmap/workspace-file-persistence.md]]). Incremental writes skip unchanged documents.

## Not implemented (see roadmap)

- External migration tooling beyond `initSchema` on startup.
- Full per-document snapshot layout and incremental file writes — [[doc/roadmap/workspace-file-persistence.md]].
- Server-authoritative merge and conflict markers — [[doc/roadmap/future-merge-sync.md]].
- Removal of legacy `Persistence:Mode` / `FileAgent` file-authority path from server startup (code still carries rollback hooks).

---

## Why people confuse persistence with the domain

It is natural to assume PostgreSQL "looks like" nested nodes. The **file** side uses a **compact outline syntax** for document artifacts; that is **not** how the relational model should be designed. The **domain** shape is `Model.fs`; SQL should follow that. The **event log** stays one row per `Change`, not one row per low-level `Op`, unless that is changed deliberately later.
