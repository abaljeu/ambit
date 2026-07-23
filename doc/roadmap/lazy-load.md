# Lazy Load

Category: Workspace scale
Status: Partial — disk-to-graph reconciliation implemented; expand-to-parse planned next; reconcile wired after WebDAV upload+finish-commit (Desktop Upload structure moving client-side — [[workspace-upload-client-structure]])
See also: [[workspaces-checklist]], [[workspace-file-sync]], [[workspace-upload-client-structure]], [[workspace-file-model]], [[workspace-scale-import]], [[workspace-scale-file-and-db-management]], [[doc/current/persistence-model]], [[doc/roadmap/reference-expressions.md]]

Lazy Load turns workspace files on server disk into a browsable graph without eagerly parsing every file. File-tree sync ([[workspace-file-sync]]) is a separate capability. **Today:** Lazy Load reacts after a successful WebDAV upload + finish-commit and publishes ordinary graph changes for existing polling clients. **Planned:** Desktop Upload builds Directory/File stubs on the client and drops that post-upload reconcile; disk→graph reconcile remains for web Upload without Desktop and for repair ([[workspace-upload-client-structure]]).

## User-visible behavior

- A successful workspace Upload makes newly added source paths appear as Directory and File stubs under the matching named Workspace (today via post-upload reconcile; planned Desktop path via client stubs — [[workspace-upload-client-structure]]). File contents are not parsed, so a new File stub has no parsed child nodes.
- `Ctrl+Shift+>` parses the focused Unparsed File, or the Unparsed File that owns the focused owner occurrence, in place. On a named Workspace (or scoped focus) it instead uploads that scope via WebDAV. `Ctrl+Shift+<` downloads the focused scope to the desktop. Upload / Download direction: [[workspace-file-sync]].
- Expanding an unparsed File will later read and parse that file, merge the parsed result into its existing graph identity, and expose editable child nodes.
- Freshness will distinguish **current**, **unparsed**, **client older**, and **client newer**. A successful desktop Download means client and server files match and are current; it does not make unchanged graph content unparsed.
- A deleted workspace file trashes its graph node under `//TRASH/`; the workspace disk path is gone and the UI shows a missing target. The user can recover from TRASH in the graph or from server history on disk. Moving out of TRASH may recreate disk content if the user chooses.
- Document load units will later load and unload whole graph documents independently of path reconciliation.
- Server residency, client unload/LRU, repo-wide search, and annotation migration remain later scale work.

## High-level implementation

```text
successful WebDAV push + finish-commit
  → compare pre-commit and post-commit repository state
  → extract changed workspace-relative paths (A / D / R / M)
  → pure disk-to-graph reconciliation planner
  → standard graph Change through the active agent / FileAgent path
  → existing poll response delivers graph changes to clients

later: expand File
  → read source + metadata
  → parse and merge under the existing File node
  → persist metadata and publish normal graph changes
```

The planner is diff-scoped and one-way disk → graph. It resolves the named Workspace, reuses matching owned path nodes, creates missing structural nodes through standard FileNodeOps and graph operations, and reports kind conflicts instead of guessing. Server `.git` internals are excluded. A file named exactly `.amb` is the document artifact for its containing Directory, or for the Workspace at the repository root, so it does not create a child File stub. Other names ending in `.amb`, such as `x.amb`, are ordinary File paths; the extension is interpreted only by document serialization and parsing.

## Implemented capability: complete stub reconciliation after server tree change

After a successful server tree update that advances `HEAD` (historically pack receive; target: WebDAV push + finish-commit), the server compares the old and new `HEAD` and extracts added, deleted, renamed, and modified paths. An initial sync with no old `HEAD` treats every path in the new tree as added. Missing Directory ancestors and File stubs are created under the workspace named by the label. Deletes move owned stubs to TRASH, identity-preserving renames preserve graph identity, and modified backing documents become **Unparsed** without parsing their content. Existing matching state makes repeated reconciliation idempotent.

The server submits the planned ops through the active graph agent using a graph-only path. This preserves revision/change-log behavior and existing polling while advancing the live-save baseline without moving or rewriting workspace files that sync already changed. No file content is read or parsed.

Reconciliation is best effort after transport success. Diff, planning, kind-conflict, or graph-apply failures are logged, while the already-completed sync response is returned unchanged. A failed push does not run reconciliation. This policy avoids reporting a completed Push as failed and leaves a later reconciliation capability to repair the graph explicitly.

### Unparsed operation invariant

Shared operation application rejects any edit, rename, or structural operation involving an Unparsed document. This applies to File, Directory, and Workspace document roots and to owner-contained nodes within the nearest document boundary, so client, server, replay, and history paths share the same rule. A structural `Replace` involves its parent and Owner children; a Ref child identity does not make the target document involved, because the occurrence belongs to the parent document. Direct mutation of the target is still governed by the target's owning document.

`SetDocumentState` is the explicit transition exception. A valid parse batch starts with `SetDocumentState(fileId, Unparsed, Current)` and only then replaces the File tree; the reverse order is rejected. The desktop parser behind `Ctrl+Shift+>` follows this ordering and parses the existing File identity rather than creating an arbitrary import target. Marking a Current document Unparsed remains legal for reconciliation.

New reconciliation stubs are built while Current and marked Unparsed only after their structure is complete. Structural reconciliation of a document that was already Unparsed is rejected under the absolute invariant; it is not silently bypassed. Content-only invalidation can still mark Current documents Unparsed and repeated invalidation is a no-op.

## Implemented structural reconciliation details

`LazyLoadReconciliation.planChangedPaths` extends create-only handling across structural changes and `M` invalidation. **Not part of step 2:** content parse/merge, richer current/older/newer metadata or UI, repair/retry policy, and client pull. Expand-to-parse and richer freshness remain planned step 3 work below.

Server disk already matches the committed tree before reconciliation runs. Reconciliation is diff-scoped, one-way disk → graph, and treats kind conflicts as errors rather than guesses.

### Path-change signals

Extract via `WorkspaceGit.changedPathsBetween` (`git diff --name-status -z -M old new`). Skip `.git/**` and invalid filename components (existing validation). Rename similarity uses the default **50%** threshold; only `R` rows are identity-preserving moves.

| Diff reports | Reconciliation |
|---|---|
| `A` path | Direct — existing `planAddedPaths` |
| `D` path | Direct — trash owned stub at path (deepest first for directory trees) |
| `R<score>` old→new | Direct — identity-preserving rename or reparent |
| `M` path | Preserve identity and structure; mark the corresponding File or exact `.amb` owner document **Unparsed** |
| `D` + `A` without `R` | Unrelated — trash the deleted path and add the new path independently |
| Initial sync (`oldHead = None`) | Direct — all paths are `A` (existing) |

Same-path content change is `M`, not `D` + `A`. Only `R` preserves node identity; do not infer rename from `D` + `A`.

### Disk cases → graph behavior

Path resolution walks workspace owned children by name (case-insensitive), same as `ownedChildNamed` in [[src/Shared/LazyLoadReconciliation.fs]]. `.amb` paths map to the parent Directory, never a File child.

| Case | Graph behavior |
|---|---|
| File or Directory delete (`D`) | MoveToTrash owned stub; parsed children stay on the trashed node |
| Directory delete (many `D` under prefix) | Deepest owned stubs first; remove Directory when no remaining disk paths map to it |
| Rename/move (`R`) | Preserve node ID — `Op.SetName` (same parent) or reparent `Op.Replace` (cross parent) |
| Kind change (file↔dir at same path) | Error — no conversion |
| `.amb` added | Ensure Directory exists (existing create path) |
| `.amb` deleted alone | No-op on graph structure (marker only) |
| Workspace root `.amb` | No-op (existing classify rule) |
| `.amb` modified | Mark its containing Directory, or the Workspace at repository root, **Unparsed** |
| `x.amb` file | Ordinary File — rename/delete/move like any other file |
| Ordinary file modified | Mark its File node **Unparsed** |
| No graph node at deleted path | No-op (idempotent) |
| Add at path with existing stub | Reuse (existing idempotent create) |

**Apply order:** deleted (deepest first) → renamed/moved → added → modified invalidation.

### Locked delete and ref semantics

- **`D` → MoveToTrash**, not hard remove. Preserves node ID and subtree under `//TRASH/`.
- **No workspace disk write** on structural trash. Sync already removed the workspace path; reconciliation is a graph structural op only. TRASH lives under `DataDir/TRASH/`, outside the per-workspace git repo at `DataDir/{label}/`. Path moves no-op when the source file is already gone. In-document edits cause saves; structural ops do not spontaneously write workspace file bodies.
- **Refs to a trashed filesystem entity:** before trashing, capture the target path via `NodeDesktopPath.pathForNodeId` (owner chain intact); find ref occurrences; replace each ref with a Normal node whose text is `[[pathexpr]]` for that path; then trash the owner. No ref→owner promotion. The expression may target a missing file; the user can retarget it later. See [[doc/history/workspaces/plans/slice_1_simplified_c97b7f48.plan.md]] § Delete owned special.

### Implemented steps

1. `WorkspaceGit.changedPathsBetween` — NUL-safe `A` / `D` / `R` / `M` extraction + server tests.
2. `LazyLoadReconciliation.resolveOwnedPath` — path → `(nodeId, kind)` under workspace label.
3. `planDeletedPaths` — deepest-first trash with pathexpr preservation for refs (dedicated lazy-load planner, not full `ViewModelDeleteOps` selection semantics).
4. `planRenamedPaths` — `NodeRenameOps.planRenameNode` or reparent `Replace` for `R` rows only; pair with `DocumentPathMove` planners in tests.
5. `planChangedPaths` — orchestrate delete → rename → add → modified invalidation; extend `LazyLoadReconciliationServer.reconcileAddedPaths` → `reconcileChangedPaths`.
6. Snapshot/persist behavior — structural lazy-load reconciliation uses graph-only apply and does not write workspace file bodies or execute disk path moves.
7. Wire reconcile after server tree commit (today: push-complete hook; target: WebDAV finish-commit — [[workspace-file-sync]]).

### Tests (Shared first)

- File/dir delete trashes stub; repeated delete no-op.
- `R` rename preserves node ID and parsed child count.
- Cross-dir `R` reparents under correct Directory ancestor.
- `docs/.amb` delete alone leaves Directory when other paths remain.
- `x.amb` rename/delete as ordinary File.
- Kind conflict (`D` file + `A` dir same path) → Error.
- `D` + `A` without `R` → trash old path and add new stub (new ID on add side).
- Owned File with Ref elsewhere → pathexpr conversion, then trash; no promotion.
- Directory tree delete ordering (deepest first).
- Modified File and exact `.amb` owner documents become **Unparsed** without structural changes.
- Idempotent re-reconcile after apply.

Server integration: server commit with rename/delete → reconcile produces correct graph ops; persist does not fail when disk already matches the committed tree.

## Capability sequence

1. **Workspace file sync** — Partial: WebDAV Class 1 Upload / Download, server finish-commit, `git check-ignore` for `.gitignore`. Authority: [[workspace-file-sync]].
2. **Disk-to-graph stub reconciliation** — implemented for add, delete, rename/move, and `M` → **Unparsed**, with exact `.amb` semantics and graph-only persistence; wired after finish-commit via `/ambit/workspace/reconciliation/directory`.
3. **Expand-to-parse and freshness** — planned next: parse one File on expansion, merge into its existing identity, and add richer current/unparsed/older/newer metadata and UI.
4. **Document load units** — define membership and whole-document loading/unloading for one graph with many documents.
5. **Residency and search** — bound server/client working sets, add repo-wide query, and address annotation migration.

## Locked decisions and boundaries

- File-tree sync ends at successful finish-commit or client Download. Lazy Load is not part of WebDAV transfer.
- **Desktop Upload structure (planned):** client owns stub creation; no post-upload directory reconcile on that path ([[workspace-upload-client-structure]]). Disk→graph reconcile remains for web / repair.
- **No delayed persist after parse:** parse must not schedule a time-delayed persist (locked on [[workspace-upload-client-structure]]).
- The server owns disk→graph reconciliation when used, because it owns authoritative disk and graph. Clients observe resulting Changes through polling.
- Reconciliation uses changed paths rather than a full workspace walk and flows only from server disk to graph.
- Structural reconciliation does not parse source contents.
- Unparsed documents are immutable until an ordered parse transition marks them Current; this can make a later structural reconciliation best-effort failure.
- There is no manual “Sync tree” command.
- Pull/freshness UI, expand-to-parse, document load units, and residency/search are independent follow-on capabilities.
- Parsing is a merge into existing nodes; it does not delete the existing File identity first.
- Only `R` preserves node identity on rename/move. `D` + `A` without `R` are unrelated trash + add operations.
- `D` trashes the graph node; refs become `[[pathexpr]]` before trash; no ref promotion.

## Planned next work

- Desktop Upload: move structure to client stubs; drop post-upload reconcile on that path ([[workspace-upload-client-structure]]); keep reconcile for web / repair.
- Add richer source metadata and current/unparsed/older/newer freshness UI.
- Add lazy server residency, client LRU/unload, repo-wide search, and annotation migration.
