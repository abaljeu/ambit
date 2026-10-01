# Persistence model (Graph / Node)

Category: Information

See Also:

- [Multi-client sync](sync-mvp.md)
- [Db agent](db-agents.md)
- [Parse and persist](parse-persist.md) — persist thread and Core file access

The database is the permanent store of graph and event info.

## Shape

### Domain types

[x] Domain types: the abbreviated records in [Model](src/Shared/Model.fs).

[x] NodeId: `Guid`. PostgreSQL stores it as `UUID`.

[x] Node: `id`, `text`, `name` (`string option`), `kind`, `children` (`ChildNode list`), and `cssClasses`. `kind` is `NodeKind`: `Normal`, or `Special` of `File`, `Directory`, `Workspace`, or a system kind. `cssClasses` is an ordered list of class names. [CssClass](src/Shared/CssClass.fs).

[x] ChildNode: `ref` (`Ownership`) and `id` (`NodeId`).

[x] Ownership: `Owner` or `Ref`. `Owner` means the child list holds the owning edge. `Ref` means the child list holds a reference.

[x] Graph: the in-memory data store, with `root` (`NodeId`) and `nodes` (`Map<NodeId, Node>`). Derived maps `parentByChild` and `ownerParentByChild` are computed from the node map and the child lists.

[x] Root id: fixed, `Graph.rootId` (`Guid.Empty`).

### On-disk artifacts

[x] Offline file read: the program can start with the database offline. It reads file data and recreates a partial graph. That partial graph is not used for editing.

[x] Document artifacts: the `.amb` format can hold a total graph. The program does not use `.amb` for the whole graph. Each workspace, directory, or file document root has outline or payload text under `DataDir/{label}/...`. Path layout, membership, incremental writes, and path moves: [Workspace file persistence](doc/roadmap/workspace-file-persistence.md).

[x] DataDir default: on-disk artifacts under `DataDir` default to `data/` locally and `/home/data` on Azure. [Database](src/Server/Database.fs), [Database setup](src/Server/DatabaseSetup.fs), [Document loader](src/Server/DocumentLoader.fs). [PostgreSQL environments](doc/reference/postgres-environments.md).

[x] Outline syntax: lines are tab-indented. An optional `{...}` class meta is allowed. `Snapshot.read` and `Snapshot.write` in [Snapshot](src/Shared/Snapshot.fs) read and write that syntax. Serialization stops at nested document roots.

[x] Parity: defined on `Graph` and revision. It is not a byte-for-byte match of raw outline text to SQL rows.

### PostgreSQL schema

[x] changes row: one row records one persisted client change. `payload` is the full `Change` JSON, including `ops`. This is the same JSON string concept as a historical `.log` line.

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

[x] `server_revision_after`: the server revision after that row is applied. Replay uses rows whose value is greater than the stored checkpoint revision, `graph.revision`. [Database](src/Server/Database.fs).

[x] graph row: the server holds one graph. Store `Graph.root` as `root_id`. `revision` matches `Revision` and is the log replay boundary beside the node projection. `revision` is not a field of `Graph`. The model has no `document_name`.

```sql
CREATE TABLE graph (
    singleton   SMALLINT PRIMARY KEY DEFAULT 1 CHECK (singleton = 1),
    root_id     UUID        NOT NULL,
    revision    INT         NOT NULL
);
```

[x] nodes row: one row is one `Node`. Columns map to `Model.Node` except `children`, which is normalized into `node_children`.

[x] `id`: `UUID`, `Node.id` (`NodeId`).

[x] `text`: `TEXT`, `Node.text`.

[x] `name`: `TEXT`, `Node.name` (nullable).

[x] `kind`: `TEXT`, `Node.kind` (`normal`, `file`, `directory`, `workspace`, `workspaces`, `trash`).

[x] `css_classes`: `JSONB` or `TEXT[]`, `Node.cssClasses` (ordered class names).

```sql
CREATE TABLE nodes (
    id              UUID        PRIMARY KEY,
    text            TEXT        NOT NULL,
    name            TEXT        NULL,
    kind            TEXT        NOT NULL DEFAULT 'normal',
    css_classes     JSONB       NOT NULL
);
```

[x] `css_classes`: the same ordered list as `CssClasses`. An example value is the JSON `["amb-row-owned"]`.

[x] node_children row: each row is one `Model.ChildNode` in the `children` list of `parent_id`, in list order. `child_id` is `ChildNode.id`. `ownership` maps `ChildNode.ref`: `'owner'` is `Owner`, and `'ref'` is `Ref`.

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

[x] Foreign keys: this `CREATE TABLE` does not declare foreign keys. The recommended keys are `parent_id` to `nodes(id)` and `child_id` to `nodes(id)`. A bulk rebuild may need deferred constraints or a controlled insert order.

## Invariants

[x] Correlated files: a directory tree under `DataDir` holds persisted artifacts that map to graph nodes. Those nodes are workspace, directory, and file document roots.

[x] Database first: each accepted change commits to the database first. The server then writes or updates the correlated on-disk artifacts for the affected documents. After each accepted change, the server updates the database projection and auto-persists those artifacts under `DataDir` for the affected document roots. [Workspace file persistence](doc/roadmap/workspace-file-persistence.md).

[x] Post-receive stubs: after a successful Git receive, the server may inspect added paths and submit ordinary graph Changes that create missing Directory and File stubs. It does not parse file contents. The accepted graph Change stays authoritative. [Lazy load](doc/roadmap/lazy-load.md).

[x] No outline blobs: the database does not store the line-oriented outline syntax as the graph source of truth. There is no monolithic `Snapshot.write` text as the projection. That syntax exists only in the file layer, [Snapshot](src/Shared/Snapshot.fs).

[x] Schema follows the domain: tables and columns reflect domain records `Node`, child lists, `Ownership`, and the other records in [Model](src/Shared/Model.fs). They do not mirror outline indentation or line grammar.

[x] Derived parent maps: `Graph.parentByChild` and `Graph.ownerParentByChild` are not stored as separate tables when the node and child-edge data are complete.

[x] Append-only log: each row in `changes` records one persisted `Change` per accepted batch. The log is one row per `Change`, not one row per low-level `Op`.

[x] No merge markers: the server does not apply server-authoritative merge or conflict markers. [Future merge sync](doc/roadmap/future-merge-sync.md).

## Store

[x] Projection tables: [Database](src/Server/Database.fs) maintains append-only `changes` and a normalized projection: singleton `graph`, `nodes`, and `node_children`. Outline blob checkpoints are not used in PostgreSQL.

[x] Incremental projection: ordinary accepted batches update the normalized projection incrementally through typed `ProjectionPatch` commands in [Database projection](src/Server/DatabaseProjection.fs).

[x] Touched rows: the planner derives distinct touched node ids and replaced parent ids from the accepted, persistence-enriched changes. It then reads the complete final node rows and ordered child rows from the applied graph.

[x] Projection transaction: the transaction upserts only those nodes. It deletes and reinserts children only for replaced parents. It advances the singleton revision in the same transaction as the appended change log.

[x] No-op writes: duplicate and unchanged submissions issue no projection writes.

[x] No node delete: there is no persistent node-deletion command. The submitted operation vocabulary has no delete-node operation.

[x] First batch: when the graph singleton is absent, the first accepted batch uses full projection replacement and writes all canonical rows.

[x] Rebuild path: explicit document-file rebuilds keep the full truncate-and-replace path. Initialized ordinary writes do not truncate projection tables.

[x] Read root: a `Graph` rebuild reads the singleton `graph` row for `root_id`.

[x] Load nodes: it loads all `nodes` and builds a map `NodeId → Node` with `children = []`.

[x] Load children: it loads `node_children`, sorts by `(parent_id, ordinal)`, and appends `{ ref = …; id = … }` to each parent's `children` list.

[x] Derive maps: it calls `Graph.fromNodes root_id` in [Model](src/Shared/Model.fs) with that map so `parentByChild` and `ownerParentByChild` match the in-memory derivation.

[x] Startup load: at `DbAgent` startup, projection maintenance loads the complete persisted projection into a frozen Graph.

[x] Startup repair: it then repairs that Graph in one ACID transaction. Unreachable `nodes` are deleted. Foreign-key cascades remove the incident child rows. Surviving `node_children` ownership is rewritten into a ROOT-owned tree.

[x] Canonical repair: ROOT and the other canonical ids are protected. Missing Workspaces, SYSTEM, and TRASH rows, or missing Owned-under-ROOT edges, are inserted.

[x] Repair commit: the planner runs in Shared. The Server commits the plan. That commit does not append `changes`, does not advance `graph.revision`, and does not collect `DataDir` artifacts.

[x] Repair reload: after a commit that is not a no-op, the agent reloads the Graph from the projection so ready state matches the repaired rows.

[x] Absent singleton: an absent graph singleton makes this maintenance a no-op.

[x] Missing root: a present singleton whose `root_id` is missing from `nodes` fails closed, as other maintenance failures do.

[x] Startup reads: the mailbox stays available while startup maintenance runs. In that mode it selectively serves `GetState`, `GetRevision`, and `GetChangesSince` from the frozen loaded state. Ordinary mutations stay buffered in FIFO order.

[x] Readiness: after a successful maintenance commit and Graph reload, or after a no-op that skips reload, the agent publishes readiness and enters its normal serialized receive loop.

[x] Maintenance failure: on maintenance failure, safe reads remain available. Queued mutations and new mutations fail closed with the startup error.

[x] Starting up: state and poll responses carry this readiness value. The browser shows `Starting up…` until a successful poll or state response reports that normal queue processing is active. Client mutation controls stay enabled because the server buffers those requests safely.

[x] Connection string: `DB_CONNECTION_STRING` names the database connection.

[x] Unchanged documents: incremental writes skip unchanged documents.

[o] Sync live-save: sync live-save on an accepted change is the `DataDir` write feeder. `DbAgent` calls `DocumentPersistChange.persistGraphOps` / `persistGraphChange` after the database commit.

[ ] Browser residency: want-driven.

[x] Server files: schema init, startup wiring, and the agent are [Database setup](src/Server/DatabaseSetup.fs), [Server](src/Server/Server.fs), and [Db agent](src/Server/Core/DbAgent.fs).

[x] `initSchema`: `Database.initSchema` creates `changes`, `graph`, `nodes`, and `node_children` on startup. It drops legacy `snapshots` when that table is present. There is no external migration tool.

[x] Change apply: `DbAgent` loads the projection and replays the `changes` tail. Each accepted change appends a row and applies a typed incremental projection patch in one transaction.

[x] Test database: automated database tests set `TEST_DB_CONNECTION_STRING`. [DbAgent tests](tests/Server.Tests/DbAgentTests.fs). [PostgreSQL environments](doc/reference/postgres-environments.md).

## Explanation

People can think that PostgreSQL looks like nested nodes. The file side uses a compact outline syntax for document artifacts. That syntax is not the relational model.
