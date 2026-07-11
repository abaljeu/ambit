# Lazy Load

Category: Workspace scale
Status: Partial — disk-to-graph reconciliation implemented; expand-to-parse planned next
See also: [[workspaces-checklist]], [[git-sync-gateway]], [[workspace-file-model]], [[workspace-scale-import]], [[workspace-scale-file-and-db-management]], [[doc/current/persistence-model]], [[doc/roadmap/reference-expressions.md]]

Lazy Load turns workspace files delivered by Git into a browsable graph without eagerly parsing every file. Git transport remains a separate completed capability: Lazy Load reacts after Git changes and publishes ordinary graph changes for existing polling clients.

## User-visible behavior

- A successful workspace push makes newly added source paths appear as Directory and File stubs under the matching named Workspace. File contents are not parsed, so a new File stub has no parsed child nodes.
- `Ctrl+Shift+>` parses the focused Unparsed File, or the Unparsed File that owns the focused owner occurrence, in place. On a named Workspace it instead pushes that Workspace to the server. `Ctrl+Shift+<` pulls the focused named Workspace to the desktop. Both Workspace branches require desktop Git capability.
- Expanding an unparsed File will later read and parse that file, merge the parsed result into its existing graph identity, and expose editable child nodes.
- Freshness will distinguish **current**, **unparsed**, **client older**, and **client newer**. A successful desktop pull means client and server files match and are current; it does not make unchanged graph content unparsed.
- A git-deleted workspace file trashes its graph node under `//TRASH/`; the workspace disk path is gone and the UI shows a missing target. The user can recover from TRASH in the graph or from Git history on disk. Moving out of TRASH may recreate disk content if the user chooses.
- Document load units will later load and unload whole graph documents independently of path reconciliation.
- Server residency, client unload/LRU, repo-wide search, and annotation migration remain later scale work.

## High-level implementation

```text
successful git-receive-pack
  → compare pre-receive and post-receive repository state
  → extract changed workspace-relative paths (A / D / R / M)
  → pure disk-to-graph reconciliation planner
  → standard graph Change through the active agent / FileAgent path
  → existing poll response delivers graph changes to clients

later: expand File
  → read source + metadata
  → parse and merge under the existing File node
  → persist metadata and publish normal graph changes
```

The planner is diff-scoped and one-way disk → graph. It resolves the named Workspace, reuses matching owned path nodes, creates missing structural nodes through standard FileNodeOps and graph operations, and reports kind conflicts instead of guessing. Git internals are excluded. A file named exactly `.amb` is the document artifact for its containing Directory, or for the Workspace at the repository root, so it does not create a child File stub. Other names ending in `.amb`, such as `x.amb`, are ordinary File paths; the extension is interpreted only by document serialization and parsing.

## Implemented capability: complete post-receive stub reconciliation

After a successful `git-receive-pack`, the server compares the old and new `HEAD` and extracts added, deleted, renamed, and modified paths. An initial push with no old `HEAD` treats every path in the new tree as added. Missing Directory ancestors and File stubs are created under the workspace named by the gateway label. Deletes move owned stubs to TRASH, Git renames preserve graph identity, and modified backing documents become **Unparsed** without parsing their content. Existing matching state makes repeated reconciliation idempotent.

The server submits the planned ops through the active graph agent using a graph-only post-receive path. This preserves revision/change-log behavior and existing polling while advancing the live-save baseline without moving or rewriting workspace files that Git already changed. No file content is read or parsed.

Post-receive reconciliation is best effort after transport success. Diff, planning, kind-conflict, or graph-apply failures are logged, while the already-completed Git response is returned unchanged. A failed receive does not run reconciliation. This policy avoids reporting a completed push as failed and leaves a later reconciliation capability to repair the graph explicitly.

### Unparsed operation invariant

Shared operation application rejects any edit, rename, or structural operation involving an Unparsed document. This applies to File, Directory, and Workspace document roots and to owner-contained nodes within the nearest document boundary, so client, server, replay, and history paths share the same rule. A structural `Replace` involves its parent and Owner children; a Ref child identity does not make the target document involved, because the occurrence belongs to the parent document. Direct mutation of the target is still governed by the target's owning document.

`SetDocumentState` is the explicit transition exception. A valid parse batch starts with `SetDocumentState(fileId, Unparsed, Current)` and only then replaces the File tree; the reverse order is rejected. The desktop parser behind `Ctrl+Shift+>` follows this ordering and parses the existing File identity rather than creating an arbitrary import target. Marking a Current document Unparsed remains legal for reconciliation.

New reconciliation stubs are built while Current and marked Unparsed only after their structure is complete. Structural reconciliation of a document that was already Unparsed is rejected under the absolute invariant; it is not silently bypassed. Content-only invalidation can still mark Current documents Unparsed and repeated invalidation is a no-op.

## Implemented structural reconciliation details

`LazyLoadReconciliation.planChangedPaths` extends create-only handling across structural changes and `M` invalidation. **Not part of step 2:** content parse/merge, richer current/older/newer metadata or UI, repair/retry policy, and client pull. Expand-to-parse and richer freshness remain planned step 3 work below.

Git transport already updated disk before reconciliation runs. Reconciliation is diff-scoped, one-way disk → graph, and treats kind conflicts as errors rather than guesses.

### Git signals

Extract via `WorkspaceGit.changedPathsBetween` (`git diff --name-status -z -M old new`). Skip `.git/**` and invalid filename components (existing validation). Git rename similarity uses the default **50%** threshold; only `R` rows are identity-preserving moves.

| Git reports | Reconciliation |
|---|---|
| `A` path | Direct — existing `planAddedPaths` |
| `D` path | Direct — trash owned stub at path (deepest first for directory trees) |
| `R<score>` old→new | Direct — identity-preserving rename or reparent |
| `M` path | Preserve identity and structure; mark the corresponding File or exact `.amb` owner document **Unparsed** |
| `D` + `A` without `R` | Unrelated — trash the deleted path and add the new path independently |
| Initial push (`oldHead = None`) | Direct — all paths are `A` (existing) |

Same-path content change is `M`, not `D` + `A`. Only Git `R` preserves node identity; do not infer rename from `D` + `A`.

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

- **Git `D` → MoveToTrash**, not hard remove. Preserves node ID and subtree under `//TRASH/`.
- **No workspace disk write** on structural trash. Git already removed the workspace path; reconciliation is a graph structural op only. TRASH lives under `DataDir/TRASH/`, outside the per-workspace git repo at `DataDir/{label}/`. Path moves no-op when the source file is already gone. In-document edits cause saves; structural ops do not spontaneously write workspace file bodies.
- **Refs to a trashed filesystem entity:** before trashing, capture the target path via `NodeDesktopPath.pathForNodeId` (owner chain intact); find ref occurrences; replace each ref with a Normal node whose text is `[[pathexpr]]` for that path; then trash the owner. No ref→owner promotion. The expression may target a missing file; the user can retarget it later. See [[doc/history/workspaces/plans/slice_1_simplified_c97b7f48.plan.md]] § Delete owned special.

### Implemented steps

1. `WorkspaceGit.changedPathsBetween` — NUL-safe `A` / `D` / `R` / `M` extraction + server tests.
2. `LazyLoadReconciliation.resolveOwnedPath` — path → `(nodeId, kind)` under workspace label.
3. `planDeletedPaths` — deepest-first trash with pathexpr preservation for refs (dedicated lazy-load planner, not full `ViewModelDeleteOps` selection semantics).
4. `planRenamedPaths` — `NodeRenameOps.planRenameNode` or reparent `Replace` for Git `R` rows only; pair with `DocumentPathMove` planners in tests.
5. `planChangedPaths` — orchestrate delete → rename → add → modified invalidation; extend `LazyLoadReconciliationServer.reconcileAddedPaths` → `reconcileChangedPaths`.
6. Snapshot/persist behavior — structural lazy-load reconciliation uses graph-only apply and does not write workspace file bodies or execute disk path moves.
7. Wire `GitGateway.completeWorkspacePush` to the full diff.

### Tests (Shared first)

- File/dir delete trashes stub; repeated delete no-op.
- Git `R` rename preserves node ID and parsed child count.
- Cross-dir `R` reparents under correct Directory ancestor.
- `docs/.amb` delete alone leaves Directory when other paths remain.
- `x.amb` rename/delete as ordinary File.
- Kind conflict (`D` file + `A` dir same path) → Error.
- `D` + `A` without `R` → trash old path and add new stub (new ID on add side).
- Owned File with Ref elsewhere → pathexpr conversion, then trash; no promotion.
- Directory tree delete ordering (deepest first).
- Modified File and exact `.amb` owner documents become **Unparsed** without structural changes.
- Idempotent re-reconcile after apply.

Server integration: git commit with rename/delete → `completeWorkspacePush` produces correct graph ops; persist does not fail when disk already matches Git.

## Capability sequence

1. **Git workspace transport** — complete; smart HTTPS gateway, authentication, desktop clone/pull/push/status, clean-tree and fast-forward push policy. Authority: [[git-sync-gateway]] and completed record [[workspace-scale-import-slice2-plan]].
2. **Disk-to-graph stub reconciliation** — implemented for add, delete, rename/move, and `M` → **Unparsed**, with exact `.amb` semantics and graph-only post-receive persistence.
3. **Expand-to-parse and freshness** — planned next: parse one File on expansion, merge into its existing identity, and add richer current/unparsed/older/newer metadata and UI.
4. **Document load units** — define membership and whole-document loading/unloading for one graph with many documents.
5. **Residency and search** — bound server/client working sets, add repo-wide query, and address annotation migration.

## Locked decisions and boundaries

- Git transport ends at the successful receive or client pull. Lazy Load is not part of pack transport.
- The server owns post-receive reconciliation because it owns the graph. Clients observe resulting Changes through polling.
- Reconciliation uses changed paths rather than a full workspace walk and flows only from server disk to graph.
- Structural reconciliation does not parse source contents.
- Unparsed documents are immutable until an ordered parse transition marks them Current; this can make a later structural reconciliation best-effort failure.
- There is no manual “Sync tree” command.
- Pull/freshness UI, expand-to-parse, document load units, and residency/search are independent follow-on capabilities.
- Parsing is a merge into existing nodes; it does not delete the existing File identity first.
- Only Git `R` preserves node identity on rename/move. `D` + `A` without `R` are unrelated trash + add operations.
- Git `D` trashes the graph node; refs become `[[pathexpr]]` before trash; no ref promotion.

## Planned next work

- Add richer source metadata and current/unparsed/older/newer freshness UI.
- Add lazy server residency, client LRU/unload, repo-wide search, and annotation migration.
