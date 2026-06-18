# Workspace Stage Plan

Status: Draft
Authority: Planning document for implementation sequencing.
Stage status and terminology: [[doc/roadmap/workspace-file-model.md]] (Implementation Stages).
See also: [[doc/roadmap/reference-expressions.md]], [[doc/current/persistence-model.md]]

## Scope Relationship

This document expands the workspace-only slice of [[doc/roadmap/workspace-file-model.md]] with implementation detail, verification, and sequencing.

The file-model document defines target end-state behavior across Stages 1–9 and tracks current status (`[x]` done, `[~]` partial, `[ ]` not started). Differences between this plan's workspace-only execution scope and the full file-model stages are intentional where noted.

Status legend (from file-model):

- `[x]` implemented in the current codebase
- `[~]` partially implemented or represented in the model, but not yet wired through
- `[ ]` target design only

## Stage Map

| File-model stage | This plan | Status |
| --- | --- | --- |
| 1 — model vocabulary | §1 | `[x]` |
| 2 — invariants/ops as behavior-bearing | §1, §2 | `[x]` |
| 3 — shared label → workspace-root mapping | §3 | `[x]` |
| 4 — desktop `@label:relative` via local mapping | §4 | `[x]` |
| 5 — unresolved UI; file-status | §5 | deferred |
| 6 — user commands for structure | §2, §6 | `[ ]` |
| 7 — server `DataDir` live-save + path moves | §7 | `[ ]` |
| 8 — snapshot integration; incremental persist | §7 | `[ ]` |
| 9 — document membership (`docId`) | §7 | `[ ]` |

Corrections tracked in the file-model (placement rules, persistence-split docs, RefExpr namespace semantics) are noted inline where they affect this stage's work.

## Goal

Implement workspace and outline **structure commands** (Stage 6) without waiting on Stage 5 server file-status or full unresolved UI.

Stage 5 (unresolved indicators; primary server file-status) is **deferred** — desktop file-status for locally mapped paths remains; server-side status waits on Stage 7.

For Stages 6–7, "structure support" means:

- users can define workspaces and create owned `Special Directory` / `Special File` nodes via **Insert…**
- users can rename workspace, directory, and file nodes via **Rename** (F2)
- soft delete continues to reparent under **TRASH** (Stage 6 retires `Special Trash` in favor of `Special Directory` with `Node.name = TRASH`)
- shared planners emit `DocumentPathMove` descriptors for rename, reparent, and move-to-TRASH (Stage 6 — computation and tests only; Stage 7 executes on disk)

Stage 1 vocabulary for `Special Directory` and `Special File` already exists in the shared model; Stage 6 adds command surfaces. Server `DataDir` materialization and live path moves land in Stage 7.

## Explicit Scope

### In Scope (this stage)

- Workspace identity and uniqueness.
- Workspace root nodes as first-class special nodes under `Workspaces`.
- A stable `WORKSPACES` container node directly under `ROOT` (the only required top-level structural anchor besides implicit `ROOT`).
- Shared mapping: workspace label → workspace root node (Stage 3 — done).
- Local mapping: workspace label → absolute local filesystem root (secondary; Stage 4 — done).
- User-visible **Insert…** and **Rename** commands for workspace, directory, and file structure (Stage 6).
- User-visible desktop app configuration JSON containing workspace mappings.
- A desktop-accessible workspace-node command for "open workspace in explorer".
- No commands to set, update, clear, or list local mappings.
- Unresolved-workspace indication when a label is unknown (Stage 5 — deferred; not blocking Stage 6).
- Server-side persistence of directory and file **documents** under `DataDir/@label/...` (requirement documented; Stages 7–8 — not implemented).

### Out Of Scope (follow-on stages)

- Stage 5 corrections: full cross-scope unresolved UI; server file-status before Stage 7 is wired.
- Server `DataDir` path materialization and filesystem moves for directory/file document roots (Stage 7).
- Hard delete under TRASH (artifact removal — Stage 7 slice).
- Document membership in the model (`docId`, load/unload — Stage 9).
- Namespace wildcard resolution under workspaces.
- Automatic filesystem sync/import/reconciliation (manual Import/Export via desktop continues).
- RefExpr postfixes (`.text`, `[n]`, filters) and command/assignment syntax.

Directory and file **node identity** is Stage 1 vocabulary; Stage 6 adds create/rename command surfaces and TRASH-as-directory model change. Stage 7 adds server `DataDir` persist and unified path moves.

## Deliverables

- Shared model updates that enforce workspace invariants (§1 — done).
- Operation/change surface for workspace lifecycle (§2 — done).
- Shared persistence for workspace label mapping (§3 — done).
- Desktop-local persistence for local workspace root mappings via config JSON (§4 — done).
- UI/command surface for workspace management, Insert…, Rename, and desktop workspace actions (§5–§6).
- Reference resolver support for workspace namespace base references (§1 correction — done).
- Tests for invariants, persistence round-trips, and command behavior.

## Implementation Plan

## 1. Shared Model And Invariants

Status: Stages 1–4 `[x]` complete.

Stage 1 — model vocabulary (done):

- `SpecialKind` includes `Workspace`, `Directory`, and `File` in the shared model.
- `workspacesId` canonical node with `kind = Special Workspaces`.
- `Workspaces` is permanent under `ROOT` (same permanence as `Trash`).

Stage 2 — behavior-bearing concepts (done):

- Add explicit workspace identity type (label, normalized label).
- Define canonical workspace-label comparison rules (case-insensitive identity; preserve display casing).
- Ensure `ROOT` has exactly one `WORKSPACES` owner child node.
- Add graph-level workspace index: label → workspace node id.
- Enforce one workspace root node per workspace label.
- Ensure workspace nodes are `Special Workspace` direct children of `Workspaces`.
- Preserve existing owner/ref semantics unchanged.

Corrections (done — see [[doc/current/workspace-graph.md]]):

- Placement restrictions apply only to `workspaces`/`workspace`; `directory`, `file`, and `normal` nodes may be placed anywhere in the ownership tree.
- Treat workspace, directory, and file as context-defining special nodes for traversal and resolution (`RefExpr.refContext`, `RefExpr.match_`).
- Align RefExpr semantics with directory-first member lookup (`DirStep`/`FileStep`) and `^` structural-container lookup — [[doc/roadmap/reference-expression-interpretation.md]].

Verification:

- Shared tests prove uniqueness and normalization rules.
- Shared tests prove workspace create/rename invariants.

## 2. Shared Operations And Change Replay

Status: Stage 2 `[x]`; Stage 6 structure commands `[ ]`.

- Add change operations for workspace lifecycle:
  - create workspace
  - rename workspace
- Stage 6 adds directory/file create (Insert…) and Rename (F2) — see §6.
- Define conflict and idempotency behavior for replay.
- Ensure replay preserves canonical labels and uniqueness.

Verification:

- Reducer tests for each operation success/failure case.
- Replay tests with duplicate, out-of-order, and conflicting changes.

## 3. Shared Persistence Shape

Status: Stage 3 `[x]`. Correction (persistence-split documentation) `[x]`.

Done — see [[doc/current/workspace-graph.md]] and [[doc/current/persistence-model.md]].

Shared workspace-label → workspace-root mapping is stored in the graph projection only (not server `DataDir` file layout):

- `Special Workspace` nodes under `Workspaces` with `Node.name` = label
- `nodes.kind` / `nodes.name` in PostgreSQL (`GraphProjection`)
- change-log JSON (`Op.NewSpecialNode`, `Op.SetName`, `Op.Replace`)
- `Snapshot.write` may emit `@label:` path text for workspace nodes (write-only hint); directory and file path bodies in snapshot text are not shared persistence authority

Directory and file node identity (`kind`, `name`, owner link) may exist in the graph projection; no server `DataDir` path materialization for them yet (Stages 7–8).

Target persistence split (documented): workspace, directory, and file documents persist separately; serialization stops at nested document roots — see [[doc/roadmap/workspace-file-model.md]] Persistence Shape and [[doc/roadmap/workspace-file-persistence.md]].

Lookup: `RefExpr.refContext`, `RefExpr.match_`, and owner-name scans in search helpers.

Desktop-local label → absolute root mapping is separate — [[doc/current/workspace-local-mapping.md]]. Server `DataDir` is primary file authority; desktop mapping is secondary (download/export, Import, local file-status).

## 4. Desktop-Local Workspace Configuration

Status: Stage 4 `[x]`.

### Done (interim API)

See [[doc/current/desktop-local-files.md]] and [[doc/current/workspace-local-mapping.md]].

- Local config at `%LocalAppData%/Gambol/config.json`; loaded at proxy startup.
- `/_desktop/capabilities`, `/_desktop/file-status`, `GET/POST /_desktop/file` for import/export.
- `@label:relative` path resolution when `workspacePaths` capability is enabled.
- Tests: `tests/Shared.Tests/WorkspaceLocalMappingTests.fs`.

Resolves workspace label + relative path via readonly local mapping. Independent of server path layout.

### 4a. Remaining (target API — desktop-local only)

Not in file-model Stage 4 (Stage 4 is done at interim import/export resolution). This extended loopback API is still planned work for a follow-on slice.

This loopback API resolves paths against the desktop's mapped absolute workspace roots. It is **separate** from server `DataDir/@label/...` persistence (§7), which writes under the server's configured `DataDir`.

- Expose desktop-local endpoints (loopback + local auth token required):
  - GET workspaces -> workspace labels only
  - GET dir -> directory contents with metadata (name, kind, size, modifiedUtc)
  - PUT dir -> create directory
  - DELETE dir -> delete directory
  - GET file -> text content + modifiedUtc
  - PUT file -> replace text content, requires expected modifiedUtc match
  - DELETE file -> delete file
- Path safety:
  - Client sends workspace label + relative path only
  - Reject absolute paths and any upward traversal (`..`)
  - Resolve to absolute path under mapped workspace root only; reject root escapes
- Timestamp validation:
  - PUT file compares expected modifiedUtc with current modifiedUtc
  - On mismatch, return conflict and include current modifiedUtc in response

Verification:

- Local config read/write tests.
- Config validation tests (missing path, duplicate label, malformed JSON).
- Tests proving local mapping changes do not alter shared graph state.
- Endpoint contract tests for GET/PUT/DELETE dir/file and GET workspaces.
- Security tests for loopback-only + token requirement.
- Path validation tests (absolute path, upward traversal, root escape).
- Conflict tests proving timestamp mismatch returns conflict + current modifiedUtc.

## 4b. Desktop Startup Workspace Registration

When the desktop app starts, it reads the local workspace config and ensures the cloud server's shared graph contains a workspace node for each configured label. This bootstraps the shared model from desktop-local config without requiring the user to issue create commands manually.

- Load local workspace config at startup (before the local proxy begins serving requests) and hold it in memory so `/_desktop/*` workspace endpoints can resolve paths immediately.
- If local config is absent or empty, skip registration; no requests are sent to the server.
- If credentials are not available at startup (user not yet logged in), defer registration until the first successful login is detected.
- Query the server for its current workspace label set.
- For each label in local config that is absent from the server's workspace set: send a create-workspace operation to the server using the stored credentials.
- Treat a conflict response (label already exists) as success; this covers concurrent startup or prior partial sync.
- Labels present in the server but absent from local config: leave unchanged. The server is authoritative for workspace existence; local config only supplies path bindings.
- On any server error during registration: log the failure and continue. Desktop remains usable; unregistered labels will simply lack workspace nodes on the server until the next startup or until the user creates them via the normal command surface.

Verification:

- Test: startup with local=[A,B,C], server=[B,C,D] → creates A only, leaves B/C/D unchanged.
- Test: startup with empty local config → no server requests sent.
- Test: server returns conflict for a label during sync → counted as success, not retried.
- Test: server unreachable during sync → error logged, remaining labels skipped, no crash.
- Test: no credentials at startup → registration deferred; on login, sync runs with full label set.
- Test: in-memory config is populated before the first `/_desktop/workspaces` request is served.

## 5. Client UI And Unresolved References

Status: Stage 5 **deferred** (bypassed for Stage 6).

Stage 5 is not blocking Stage 6 structure commands. Remaining Stage 5 work:

- File-status uses the desktop query surface for locally mapped paths (unchanged).
- Primary server live-save and server-side file-status are not wired (Stage 7).
- Full unresolved `@label:` UI across workspace, directory, and file scopes is not done.

Corrections (deferred):

- Unresolved UI should cover namespace resolution failures across workspace, directory, and file scopes.
- File-status should query server persistence when Stage 7 is wired; desktop query remains for secondary mapped paths until then.

### 5a. Search by namespace references

Done.

- [[@src/Shared/ViewModelSearch.fs:79-93]] existing search method
- [[@src/Shared/RefExpr.fs:340-357]] new match method. Should return same NodeSearchResult.
- The search dialog will merge these results. RefExpr matches first.

### 5b. Import children of node

Done.

### 5c. Export node children to disk

Done — see [[doc/current/desktop-local-files.md]].

### 5d. Workspace UI (remaining)

- Add a desktop-accessible command on workspace nodes for opening the mapped workspace root in explorer.
- Add clear "not locally mapped" feedback when desktop action is invoked without a local mapping.
- Display unresolved workspace label state in UI where references are shown.

Unresolved references must not silently behave as successful empty results — see file-model Resolution Semantics.

Verification:

- Command-level tests for happy path and invalid input.
- UI state tests for unresolved labels and unmapped desktop actions.

## 6. User Commands (Structure)

Status: Stage 6 `[ ]` — target design; next implementation slice.

### TRASH model change

**Today:** canonical `trashId` is `Special Trash` — not a document root; no on-disk folder.

**Target:** `trashId` becomes **`Special Directory`** with `Node.name = TRASH` (display `text` may remain `Trash`). Retire `SpecialKind.Trash` / `Special Trash`.

| Concern | Treatment |
| --- | --- |
| Permanence | Same as today — fixed `trashId`, permanent owner child of ROOT, not renamable/removable |
| Delete semantics | Unchanged at graph layer — `MoveToTrash` appends owner under `trashId` |
| Snapshot / `.amb` | Keep stable sid `#TRASH`; owner line includes name token `TRASH` |
| Path resolution | `NodeDesktopPath` resolves TRASH as `@:/TRASH/` (under nameless ROOT workspace) |
| Row styling | Map `trashId` to existing trash row class/symbol (by id, not kind) |

On disk (Stage 7): TRASH is a persisted directory document — folder `TRASH/` with artifact `TRASH.amb` under the ROOT workspace path in `DataDir` — see [[doc/roadmap/workspace-file-persistence.md]].

### Insert… command

Single command (**Insert…**, key `f`).

**Pick existing (unchanged):** Enter or click a search result inserts `{ ref = Ref; id = fileNodeId }` at focus via `planInsertFileRefAtFocus`.

**Create (local owner child — no remote tree walk, no Ref on create):**

| Focus context | Buttons | Result |
| --- | --- | --- |
| **`Workspaces` node** | **New Workspace** | `Special Workspace` under `workspacesId` |
| **Anywhere else** | **New File** / **New Folder** | `Special File` or `Special Directory` as owner child of **focus node** |

Removed from prior design: path-based create, `planAddFileAtFocus`, `planCreateFileInWorkspaces`, path-based `isNewEnabled`.

Shared create ops: `planCreateWorkspace`, `planCreateOwnedFile`, `planCreateOwnedDirectory`.

### Rename command

**Key: F2** (`SelectionOnly`). **Edit node** keeps **Enter** only — F2 reassigned from Edit node to Rename.

Prompt on focused node. **Workspace / Directory / File:** `Op.SetName`. **Normal:** `Node.name` only (not `text`). Reject ROOT, Workspaces, and canonical TRASH node.

Stage 6 applies graph op only; emits optional `DocumentPathMove` descriptor (consumed in Stage 7).

Shared: extend `Graph.setName` for Normal nodes; `planRenameNode` → `Op.SetName` + `planPathMoveForSetName`.

### DocumentPathMove (Stage 6 stub, Stage 7 consumer)

Replace narrow rename-only planning with a shared move descriptor:

```fsharp
type DocumentPathMove = {
    nodeId: NodeId
    oldPath: string
    newPath: string
}
```

Planners (Stage 6 — path computation and tests; no I/O):

- `planPathMoveForSetName` — rename workspace/directory/file (`Op.SetName`)
- `planPathMoveForReparent` — reparent (`Op.Replace` owner parent change); move-to-TRASH uses `planPathMoveForReparent graph nodeId trashId`

For subtrees containing persisted document roots, Stage 7 may move multiple artifacts (directory tree move).

UI: Insert… dialog with three context-gated buttons (title **Insert…**); Rename prompt overlay.

Verification:

- Command-level tests for Insert… create paths and pick-existing insert.
- Rename tests for workspace, directory, file, and normal `name`.
- TRASH kind/placement/path; trash row styling by id.
- `DocumentPathMove` computation for rename and MoveToTrash (no filesystem I/O).

## 7. Server File Persistence And Document Membership

Status: Stages 7–9 `[ ]` — documented requirements only.

### Stage 7 — server `DataDir` live-save and unified path moves

Not implemented. Full spec: [[doc/roadmap/workspace-file-persistence.md]], [[doc/roadmap/workspace-text-outline-conversion.md]].

- **Path:** `{DataDir}/@{workspaceLabel}/{canonicalRelativePath}` (the `@` is part of the on-disk path segment).
- **Write pattern:** live-save on accepted change — same snapshot-backup mechanics as outline snapshot backup (`writeStateBackup`, `ensureSnapshotBackup`).
- **Stop at nested document root:** when serializing a parent document, do not recurse into nested workspace/directory/file document roots; persist them as separate artifacts.
- **Unified path moves:** one handler for any graph change that alters canonical on-disk location of workspace/directory/file document roots. Triggers: **Rename** (`Op.SetName`), **Reparent** (`Op.Replace` owner parent), **Soft delete** (`MoveToTrash` → reparent under `trashId`, same handler as reparent into `@:/TRASH/...`). No separate delete persist path for soft delete. Hard delete under TRASH (subtree artifact removal) is a separate Stage 7 slice.
- **TRASH on disk:** materialize `TRASH/TRASH.amb` under the ROOT workspace path in `DataDir`.
- **Stage 6 handoff:** shared layer emits `DocumentPathMove` lists from planners; Stage 7 server executes filesystem moves with backup rotation. Cross-workspace reparent needs no special case — `oldPath` / `newPath` differ by workspace prefix and the same handler applies.

### Stage 8 — snapshot integration

- Existing write path (`Snapshot.write` / `FileAgent` / db backup) emits ROOT plus per-document artifacts.
- Incremental persist skips unchanged documents.

### Stage 9 — document membership

- `docId` (or equivalent), derivation from document roots, client document load/unload and replication unit — [[doc/roadmap/postgres-roadmap.md]] §5–6.

**Desktop secondary:** `@label:` local mapping supports Import (unchanged) and download/export via `/_desktop/file` ([[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]]). Server `DataDir` is primary file authority.

Verification (when implemented):

- Directory and file documents write under `DataDir/@label/...` with correct relative paths.
- Rename, reparent, and move-to-TRASH apply filesystem moves from `DocumentPathMove` descriptors; subtree moves cover nested document roots.
- Prior file rotated to `.bak.{date}` on overwrite.
- Path safety: no escape above `DataDir/@label/`.
- TRASH directory document materialized (`TRASH/TRASH.amb`).
- No regression to desktop `/_desktop/file` import/export or `@label:relative` resolution.

## 8. Stage Exit Criteria

This workspace stage is complete when all of the following are true:

- Workspace labels can be created, renamed, and listed.
- Workspace root nodes exist and are uniquely mapped by label under `WORKSPACES`.
- **Insert…** creates workspaces (under `Workspaces`), directories, and files as owner children of focus.
- **Rename** (F2) applies to workspace, directory, file (`Op.SetName`) and normal (`Node.name`); Edit node is Enter-only.
- TRASH is `Special Directory` with `Node.name = TRASH`; soft delete reparents under `trashId`.
- Shared tests cover `DocumentPathMove` for rename and move-to-TRASH (no server I/O).
- Local workspace root mappings can be read from and persisted to desktop config JSON.
- No local-mapping edit/list command surface exists in this stage.
- `@workspace:` resolves for known labels; Stage 5 unresolved UI corrections remain deferred.
- Server `DataDir` document persist, path moves, and `docId` (Stages 7–9) are not required for Stage 6 exit.
- All new behavior has Shared/Client tests where applicable.

## Suggested Order Of Implementation

1. Shared model and invariants (Stages 1–4 — done).
2. **TRASH → Directory** model change + tests.
3. Shared create ops, rename, `DocumentPathMove` planners + tests.
4. **Insert…** and **Rename** command/UI (Stage 6).
5. Doc updates for Stage 6 (this pass).
6. Desktop-local mapping and interim API (Stage 4 — done); desktop startup workspace registration (§4b).
7. Stage 5 unresolved UI and server file-status (deferred — after Stage 7 or in parallel).
8. Server file persistence, unified path moves, TRASH.amb (Stage 7).
9. Snapshot integration and document membership (Stages 8–9).

## Clarifications And Decisions

Aligned with [[doc/roadmap/workspace-file-model.md]] Settled Decisions:

1. Lifecycle in this stage: create/list/rename only; removal is not in this stage.
2. Removal policy (for later stage): soft remove (hidden/disabled).
3. Label handling: store original casing for display; compare case-insensitively for identity.
4. Local mapping cardinality: exactly one local root per workspace label, per desktop.
5. Root graph structure: no default workspace auto-created; `ROOT` contains `WORKSPACES`; workspace nodes are direct children of `Workspaces`.
6. Persistence tiers: server `DataDir` primary; desktop mapping secondary and independent of server path shape.
7. Unresolved behavior: commands that require resolution are blocked when reference cannot resolve; the client must show an explicit diagnostic; silent no-op is invalid.
8. One graph, many documents (target): document membership follows Owner ancestry from document roots; Ref edges do not confer membership.
9. **Rename** → F2; **Edit node** → Enter only.
10. Soft delete → reparent under TRASH (`MoveToTrash`); no separate persist path — Stage 7 treats as reparent into `@:/TRASH/...`.
11. Cross-workspace reparent — no extra logic; unified `DocumentPathMove` handles workspace prefix change.

## Notes

This plan tracks Stages 6–7 structure commands and path-move persistence. Stage 5 is deferred. Directory and file node kinds exist in the model (Stage 1); Stage 6 adds Insert…/Rename and TRASH-as-directory; Stage 7 adds server document persist and filesystem moves per the file-model stage list.
