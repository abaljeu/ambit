---
name: Slice 1 simplified
overview: "A compressed Slice 1 plan: enforce owned File/Directory placement at graph-op time, drop reconciliation entirely, model file state as one small typed value, and keep sync/expand/stale/git as thin reuse of existing paths."
todos:
  - id: reporter
    content: Add general status reporter - VM status field + StatusMessage type, render in renderStatus into repurposed
    status: completed
  - id: placement
    content: Add owned File/Directory placement check (~6 lines) to placementError pick in Graph.replace; confirm owned-name uniqueness via test
    status: completed
  - id: create-flow
    content: Update owned-create/insert in FileNodeOps to place owner under nearest valid ancestor + ref at focus when focus is invalid
    status: completed
  - id: filestate
    content: Add FileState typed value + accessors and its persistence slot on Node (Serialization, GraphProjection, Snapshot, DB), default Unparsed
    status: completed
  - id: sync-planner
    content: Add pure shallow sync planner (reuse/create/ignore-refs/skip .git and .amb; kind collision auto-renames graph node, disk wins) returning a summary for status
    status: completed
  - id: sync-command
    content: Wire Sync tree command + server DataDir/@label listing; optional desktop mapped-root listing via same planner
    status: completed
  - id: expand-parse
    content: "Implement expand-to-parse: read one file, run existing format read, attach children, set Parsed(mtime)"
    status: completed
  - id: stale
    content: Add stale detection on expand/reparse via mtime compare; indicator + reparse action, no auto-replace
    status: completed
  - id: delete
    content: Owned-special delete removes owner and replaces each ref with a Normal node containing [[pathexpr]] (derived via NodeDesktopPath before removal); no ref promotion; ViewModelDeleteOps
    status: completed
  - id: git
    content: Add workspace-scoped git init/status/commit under @label/ (server) and desktop mapped root
    status: completed
  - id: tests
    content: Add trimmed Shared tests for placement, create-flow, sync, delete, expand, stale, and git scope
    status: completed
isProject: false
---

# Workspace Scale Import — Slice 1 (Simplified)

Replaces the reconciliation-heavy draft. Aligns with the locked doc [workspace-scale-import-slice1-plan.md](doc/roadmap/workspace-scale-import-slice1-plan.md).

## What changed from the previous draft

- **Reconciliation dropped.** No "scan and repair illegal owners", no owner-moves-with-ref, no conflict-walking, no reconcile command, and the ~4 reconciliation tests go away. The invariant is enforced only when a graph op is planned.
- **No standalone `SpecialPlacement.fs`.** Placement is ~6 lines added to the existing `placementError` pick in `Graph.replace`.
- **"Nearest valid owner" never fails.** ROOT is always a `Special Workspace` ancestor, so the walk-up in the create flow always terminates at a valid owner — no error path needed.
- **File state is one typed value** (`FileState`), not four loose fields; no hash/size/format-hint in Slice 1.
- **No "conflict reporting" dead-end in sync.** Disk is authoritative; a same-name/different-kind collision auto-resolves (rename the graph node that has no disk backing, create the disk stub). This removes the need for a conflict-resolution UI.
- **General-purpose status reporter added** (repurposes the unused `#key-last-key` element) so sync results, placement rejections, and stale notices have somewhere to surface.

## The ownership rule (unchanged intent)

Owned `Special File` / `Special Directory` may be owned only by `Special Workspace` (incl. ROOT) or `Special Directory`. Refs to files/dirs are unrestricted. Path = owner chain + name.

## 0. Status reporter (foundation)

We currently have no result/status reporting surface. Repurpose the unused `#key-last-key` element ([gambol.template.html](src/Server/wwwroot/gambol.template.html) L19, class `.amb-last-result`, today `display:none`) into a general one-line status/result line. Longer detail text is acceptable occasionally.

- **CSS** ([style.css](src/Server/wwwroot/style.css) L137): make `.amb-last-result` fill the row width (`flex: 1 1 auto`, `display:block`, ellipsis/wrap for overflow) and stay legible; keep it subtle.
- **Model-driven, MVU convention.** Add a `status` field to the VM (e.g. `status: StatusMessage option` where `StatusMessage = { text: string; kind: Info | Warn | Error }`). Render it in `renderStatus` ([View.fs](src/Client/View.fs) L611), the same place `#sync-status`/`#db-status` and the last-key line are already written from model fields.
- **Subsumes the last-key diagnostic.** The imperative `setLastKeyDisplay` write (View.fs L711, Controller.fs L73) becomes one low-priority message source routed through `status`; errors/results take precedence.
- **Consumers this slice:** placement rejection from `Graph.replace`, sync summary (created N stubs, resolved M collisions), and stale notice all set `status`.

## 1. Placement invariant (Shared)

Extend the existing `placementError` pick in `Graph.replace` ([src/Shared/Model.fs](src/Shared/Model.fs) ~L474). Add: reject a child where `child.ref = Owner`, `childNode.kind = Special (Directory|File)`, and the parent's kind is not `Special Workspace` or `Special Directory`.

Owned-name uniqueness is already covered by the existing "sibling name conflict" check (it dedupes owner names case-insensitively, so a same-name file-vs-dir already collides). Confirm with a test; only extend if a gap shows.

## 2. Create/insert flow (Shared/Client)

When an owned File/Directory would be created from an invalid focus (under File/Normal): create it under the nearest valid `Workspace`/`Directory` ancestor and place a `Ref` at focus. A tiny owner-chain walk in [src/Shared/FileNodeOps.fs](src/Shared/FileNodeOps.fs); ROOT guarantees a target, so no error case. Everything else keeps rejecting via `Graph.replace`.

## 3. File state type (Shared)

Add a small typed value + accessors, e.g.:

```fsharp
type FileState =
    | Unparsed
    | Parsed of sourceMtimeUtc: int64
```

Accessors: `isParsed`, `mtime`, `isStale diskMtimeUtc`. `Unparsed` = never read; `Parsed m` = parsed at `m`; stale when `diskMtimeUtc > m`.

Persistence cost to note: this adds a slot on `Node` (or a file-node-only field), so it round-trips through [src/Shared/Serialization.fs](src/Shared/Serialization.fs), [src/Shared/GraphProjection.fs](src/Shared/GraphProjection.fs), [src/Shared/Snapshot.fs](src/Shared/Snapshot.fs), and the server DB. Default is `Unparsed`; existing graphs load without migration.

## 4. Shallow sync planner (Shared)

Pure, one directory at a time. Inputs: node id, its owned File/Dir children, immediate disk entries (name, kind, mtime). Rules:

- same name + same kind owned child → reuse
- no owned child with that name → create owned stub
- same name, different kind → **disk wins**: rename the colliding graph node (the one with no disk backing) to a deterministic free name (e.g. append ` (was file)` / a numeric suffix), then create the correct-kind disk stub. Emit a `status` note. This is a rare case (a file renamed, then a directory renamed to the old name, all outside Gambol and un-synced).
- refs elsewhere → ignore
- skip `.git`, skip `.amb` (the directory's own info/document artifact — it is read to populate the directory, never imported as a separate file node), plus a minimal ignore set

No auto-delete of owned children missing on disk. The planner returns a small summary (created / reused / renamed counts) for the `status` line.

## 5. Sync tree command + I/O

- Server: read immediate children under `DataDir/@label/...`, feed the planner, post the ops.
- Client: workspace/directory "Sync tree" command submits planned ops; surface the summary/errors via the status reporter (§0).
- Optional same slice: desktop lists the mapped `@label:` root through the same planner.

## 6. Expand to parse

On expanding an `Unparsed` file: fetch content (server or `/_desktop/file`), run existing format read ([src/Shared/DocumentFormat.fs](src/Shared/DocumentFormat.fs)), attach children for that file only, set `FileState = Parsed diskMtime`. Never parse at sync time.

## 7. Stale

On expand/reparse: if `Parsed m` and `diskMtime > m`, show a stale indicator + reparse action. No auto-replace.

## 8. Delete owned special

Deleting an owned File/Directory removes the owner, but refs to it are **not** deleted (a ref may itself be another file's content). Before removing the owner, derive its path expression via `NodeDesktopPath.pathForNodeId graph nodeId` (owner chain must still be intact); then replace each ref node with a Normal node whose text is `[[pathexpr]]` (the dangling path expression of what it pointed at). Never promote a ref to owner. Normal-node ref-promotion behavior is unchanged. Handle in [src/Shared/ViewModelDeleteOps.fs](src/Shared/ViewModelDeleteOps.fs) with existing destructive-delete confirmation.

The existing file-status/missing-target indicator already flags that the path no longer resolves, so no new "doesn't exist" UI is needed.

## 9. Workspace git

`git init` under `DataDir/@label/` on first need; `status`/`commit` scoped to that repo only. Desktop: same via LocalProxy when the mapped root is a git repo. No pull/push (Slice 2).

## Tests (trimmed)

- Owned File under Workspace / Dir under Directory → legal
- Owned File under File / Dir under Normal → rejected
- File ref under Normal or File → legal
- Duplicate owned name (incl. file-vs-dir) under one parent → rejected
- Create owned under invalid focus → owner lands under nearest valid ancestor, ref at focus
- Sync: create stub, reuse match, ignore refs, skip `.git` and `.amb`
- Sync kind collision → colliding graph node renamed to a free name + correct-kind disk stub created (disk wins)
- Delete owned file with refs → owner removed; each ref becomes a Normal node with `[[pathexpr]]`; no promotion; normal-node promotion preserved
- Expand unparsed → children attached, `Parsed`
- Stale on newer mtime → stale, no auto-reparse
- Commit scoped to `@label/` only

## Verify

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SpecialPlacementTests|FullyQualifiedName~WorkspaceTreeSyncTests|FullyQualifiedName~ModelTests|FullyQualifiedName~DeleteOpsTests"
```