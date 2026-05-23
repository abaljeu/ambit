# Persistence vs domain model (Graph / Node)

## Aims (stated goals)

These are the persistence goals for Gambol, independent of how much is implemented in code today.

1. **Two explicit persistence modes** — `file` mode keeps the on-disk document as the source of
   truth: outline snapshot file, `.meta` (revision), and `.log` (append-only changes). `db` mode
   makes PostgreSQL the source of truth and does not read files on startup.

2. **Safe path from files to PostgreSQL** — `file` mode keeps dual persistence available: files are
   primary and successful file writes mirror to the DB when it is available. This remains the rollback
   and migration mode.

3. **Strict DB authority** — In `db` mode, startup initializes the schema and loads DB state. An
   empty DB stays empty; startup does not silently import files via `DocumentLoader.loadState` or
   `rebuildFromDocumentFiles`.

4. **DB-to-disk backup** — In `db` mode, disk files are backup/export artifacts only. The server
   periodically writes snapshot text, `.meta`, an empty `.log`, and the existing snapshot backup
   rotation from DB state. It must not create a `FileAgent` for this backup because `FileAgent`
   reads disk at startup.

5. **No outline blobs in PostgreSQL** — Do not store the file snapshot’s **line-oriented outline
   syntax** as the graph source of truth in SQL (no monolithic `Snapshot.write` text as the
   projection). That syntax exists only in the **file** layer (`src/Shared/Snapshot.fs`).

6. **Relational schema mirrors `Model.fs` fields** — Tables and columns reflect domain records
   (`Node`, child lists, `Ownership`, etc.). They do **not** mirror outline indentation or line
   grammar. **`Graph.parentByChild`** and **`Graph.ownerParentByChild`** are **derived** in code
   from nodes and child rows (same as `Graph.fromNodes` in `Model.fs`); they need not be stored as
   separate tables if the node and child-edge data are complete.

7. **Change log parity in file mode** — Rows in the SQL change log should **correspond** to lines in
   `gambol.log`
   (one persisted change per row, comparable payloads), so the event history can be audited against
   the file log while file authority is active.

Implementation status: normalized projection, DB/file agents, and `Persistence:Mode` are implemented.
See [[doc/roadmap/postgres-migration.md]] for a short operational summary.

---

## Domain types (reference)

From `src/Shared/Model.fs` (abbreviated):

- **`NodeId`** — `Guid` (use `UUID` in PostgreSQL).
- **`Node`** — `id`, `text`, `name` (`string option`), `children` (`ChildNode list`),
  `cssClasses` (ordered list of class names; see `CssClass.fs`).
- **`ChildNode`** — `ref` (`Ownership`), `id` (`NodeId`).
- **`Ownership`** — `Owner` | `Ref` (whether the child list holds the owning edge or a reference).
- **`Graph`** — `root` (`NodeId`), `nodes` (`Map<NodeId, Node>`), plus derived maps
  `parentByChild`, `ownerParentByChild` computed from the node map and child lists.

The canonical root id is fixed: `Graph.rootId` (`Guid.Empty`).

---

## File persistence (`file` mode authority)

| Artifact | Role |
|----------|------|
| **Snapshot file** | Outline **syntax** (lines, optional `{...}` class meta). Read into a `Graph`
  via `Snapshot.read`. |
| **`.meta`** | Server revision integer after snapshot + log replay. |
| **`.log`** | Append-only JSON lines, one submitted `Change` per line (same idea as SQL `changes`
  rows). |

In `file` mode, parity with the database is defined on **`Graph`** and revision, not on matching raw
outline text to SQL. When the database is empty, startup may seed it from the loaded file state. In
`db` mode these files are not read during startup and are not API authority.

---

## Target PostgreSQL schemas

The following is the **intended** relational shape. It is **not** required to match every column
name in existing bootstrap DDL until the implementation is updated.

### `changes` — append-only log (aligned with `gambol.log`)

One row per persisted client change; `payload` is the same JSON string concept as a `.log` line
(full `Change`, including `ops`).

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

`server_revision_after` is the server revision **after** applying that row; replay uses rows with
this value greater than the stored checkpoint revision (see `src/Server/Database.fs`).

### `graph` — `Graph.root` plus server revision (singleton)

`Model.Graph` has `root` and `nodes` (and derived maps). There is no `document_name` in the model;
the server holds one outline at a time (e.g. file `gambol`). Persist **`Graph.root`** as `root_id`.
**`revision`** is not on `Graph`; it matches `Revision` / file `.meta` so the DB knows the log
replay boundary alongside the node projection.

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
| `css_classes` | `JSONB` or `TEXT[]` | `cssClasses` (ordered class names) |

```sql
CREATE TABLE nodes (
    id              UUID        PRIMARY KEY,
    text            TEXT        NOT NULL,
    name            TEXT        NULL,
    css_classes     JSONB       NOT NULL
);
```

`css_classes` stores the same ordered list as `CssClasses` (e.g. JSON `["amb-row-owned"]`).

### `node_children` — `Node.children` as rows

Each row is one **`Model.ChildNode`** in **`parent_id`’s** `children` list, in list order.
**`child_id`** is `ChildNode.id`. **`ownership`** maps **`ChildNode.ref`** (`Ownership`): `'owner'`
↔ `Owner`, `'ref'` ↔ `Ref`.

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

Foreign keys `parent_id` → `nodes(id)` and `child_id` → `nodes(id)` are recommended (bulk rebuild
may need deferred constraints or insert order).

**Rebuilding `Graph`:** read the singleton `graph` row for `root_id`. Load all `nodes`, build a map
`NodeId → Node` with **`children = []`**. Load `node_children`, sort by `(parent_id, ordinal)`,
append `{ ref = …; id = … }` to each parent’s `children` list. Call **`Graph.fromNodes root_id`**
with the completed `Map<NodeId, Node>` so **`parentByChild`** and **`ownerParentByChild`** match
in-memory derivation.

---

## Current implementation vs target

**Implemented today** ([[src/Server/DatabaseSetup.fs]], [[src/Server/Server.fs]]):

- **`Persistence:Mode`** — resolved at startup: `""` or `"db"` → strict DB authority; `"file"` → file authority with optional DB mirror when `DB_CONNECTION_STRING` is set.
- **`Database.initSchema`** — creates **`changes`**, **`graph`**, **`nodes`**, **`node_children`**; drops legacy **`snapshots`** if present.
- **`DbAgent`** — loads projection + replays `changes` tail; each accepted change appends a row and updates projection in one transaction.
- **`FileAgent`** — snapshot + `.log` authority in file mode; async snapshot after changes.
- **`startDbBackupIfNeeded`** — in `db` mode, periodic disk backup (snapshot, `.meta`, empty `.log`, rotation) from DB state; does not create a `FileAgent` for startup.

**Optional follow-ups** (not required for the mode split above):

- External migration tooling beyond `initSchema` on startup.
- Multi-document / multi-file snapshot layout.
- Further audit of file↔DB log parity under all edge cases.

The obsolete doc **`db-change-doc-mode.md`** (blob-first Postgres) was removed; it is not part of
this design.

---

## Why people confuse persistence with the domain

It is natural to assume PostgreSQL “looks like” nested nodes. The **file** side uses a **compact
outline syntax** for snapshots; that is **not** how the relational model should be designed. The
**domain** shape is `Model.fs`; SQL should follow that. The **event log** stays one row (or one
line) per `Change`, not one row per low-level `Op`, unless that is changed deliberately later.

