# Persistence vs domain model (Graph / Node)

## Aims (stated goals)

These are the persistence goals for Gambol, independent of how much is implemented in code today.

1. **File authority** — The on-disk document remains the source of truth: outline snapshot file,
   `.meta` (revision), and `.log` (append-only changes). The database is a **projection**, not a
   competing authority.

2. **Safe path from files to PostgreSQL** — While the DB is introduced, **dual persistence**
   means the DB is kept aligned with files so we can detect mistakes before trusting the DB alone.

3. **Parity at startup** — After loading, compare canonical **`Graph`** from files with the
   **`Graph`** implied by the database (relational read and/or replay). **On mismatch:** report
   clearly, **rebuild** the database content from the file-derived graph (and log rows as
   applicable), then **continue** running.

4. **No outline blobs in PostgreSQL** — Do not store the file snapshot’s **line-oriented outline
   syntax** as the graph source of truth in SQL (no monolithic `Snapshot.write` text as the
   projection). That syntax exists only in the **file** layer (`src/Shared/Snapshot.fs`).

5. **Relational schema mirrors `Model.fs` fields** — Tables and columns reflect domain records
   (`Node`, child lists, `Ownership`, etc.). They do **not** mirror outline indentation or line
   grammar. **`Graph.parentByChild`** and **`Graph.ownerParentByChild`** are **derived** in code
   from nodes and child rows (same as `Graph.fromNodes` in `Model.fs`); they need not be stored as
   separate tables if the node and child-edge data are complete.

6. **Change log parity** — Rows in the SQL change log should **correspond** to lines in `gambol.log`
   (one persisted change per row, comparable payloads), so the event history can be audited against
   the file log.

Implementation status: normalized projection and startup parity/rebuild follow this document; see
`doc/postgres-migration.md` for a short operational summary.

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

## File persistence (authority)

| Artifact | Role |
|----------|------|
| **Snapshot file** | Outline **syntax** (lines, optional `{...}` class meta). Read into a `Graph`
  via `Snapshot.read`. |
| **`.meta`** | Server revision integer after snapshot + log replay. |
| **`.log`** | Append-only JSON lines, one submitted `Change` per line (same idea as SQL `changes`
  rows). |

Parity with the database is defined on **`Graph`** (and on the change sequence), not on matching
raw outline text to SQL.

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

`Database.initSchema` creates **`changes`** plus **`graph`**, **`nodes`**, and **`node_children`**,
and drops any legacy **`snapshots`** table. `DbAgent` loads the projection row and node/child rows,
replays the `changes` tail (`server_revision_after` beyond the projection revision), and on each
accepted change appends a `changes` row and replaces the projection in one transaction.

`Server` startup calls **`DocumentLoader.loadState`** (files) and **`Database.loadPersistedState`**
(DB); on graph or revision mismatch it runs **`rebuildFromDocumentFiles`**, which truncates SQL
tables and writes the projection from file authority (same semantics as the loader, including
snapshot + `.meta` + tail replay). The dev endpoint **`GET /ambit/validate`** is not used; parity
is handled at startup.

The obsolete doc **`db-change-doc-mode.md`** (blob-first Postgres) was removed; it is not part of
this design.

---

## Why people confuse persistence with the domain

It is natural to assume PostgreSQL “looks like” nested nodes. The **file** side uses a **compact
outline syntax** for snapshots; that is **not** how the relational model should be designed. The
**domain** shape is `Model.fs`; SQL should follow that. The **event log** stays one row (or one
line) per `Change`, not one row per low-level `Op`, unless that is changed deliberately later.

