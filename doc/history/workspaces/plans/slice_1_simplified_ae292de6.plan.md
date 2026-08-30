---
name: Slice 1 simplified
overview: "A compressed Slice 1 plan: enforce owned File/Directory placement at graph-op time, drop reconciliation entirely, model file state as one small typed value, and keep sync/expand/stale/git as thin reuse of existing paths."
todos: []
isProject: false
---

# Workspace Scale Import — Slice 1 (Simplified)

Replaces the reconciliation-heavy draft. Aligns with the locked doc [workspace-scale-import-slice1-plan.md](doc/roadmap/workspace-scale-import-slice1-plan.md).

## What changed from the previous draft

- **Reconciliation dropped.** No "scan and repair illegal owners", no owner-moves-with-ref, no conflict-walking, no reconcile command, and the ~4 reconciliation tests go away. The invariant is enforced only when a graph op is planned.
- **No standalone `SpecialPlacement.fs`.** Placement is ~6 lines added to the existing `placementError` pick in `Graph.replace`.
- **"Nearest valid owner" never fails.** ROOT is always a `Special Workspace` ancestor, so the walk-up in the create flow always terminates at a valid owner — no error path needed.
- **File state is one typed value** (`FileState`), not four loose fields; no hash/size/format-hint in Slice 1.

## The ownership rule (unchanged intent)

Owned `Special File` / `Special Directory` may be owned only by `Special Workspace` (incl. ROOT) or `Special Directory`. Refs to files/dirs are unrestricted. Path = owner chain + name.

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
- same name, different kind → report conflict
- refs elsewhere → ignore
- skip `.git` + a minimal ignore set

No auto-delete of owned children missing on disk.

## 5. Sync tree command + I/O

- Server: read immediate children under `DataDir/{label}/...`, feed the planner, post the ops.
- Client: workspace/directory "Sync tree" command submits planned ops; surface conflicts via existing status/error UI.
- Optional same slice: desktop lists the mapped label root through the same planner.

## 6. Expand to parse

On expanding an `Unparsed` file: fetch content (server or `/_desktop/file`), run existing format read ([src/Shared/DocumentFormat.fs](src/Shared/DocumentFormat.fs)), attach children for that file only, set `FileState = Parsed diskMtime`. Never parse at sync time.

## 7. Stale

On expand/reparse: if `Parsed m` and `diskMtime > m`, show a stale indicator + reparse action. No auto-replace.

## 8. Delete owned special

Deleting an owned File/Directory removes the owner and all refs to it; never promote a ref to owner. Normal-node ref-promotion behavior is unchanged. Handle in [src/Shared/ViewModelDeleteOps.fs](src/Shared/ViewModelDeleteOps.fs) with existing destructive-delete confirmation.

## 9. Workspace git

`git init` under `DataDir/{label}/` on first need; `status`/`commit` scoped to that repo only. Desktop: same via LocalProxy when the mapped root is a git repo. No pull/push (Slice 2).

## Tests (trimmed)

- Owned File under Workspace / Dir under Directory → legal
- Owned File under File / Dir under Normal → rejected
- File ref under Normal or File → legal
- Duplicate owned name (incl. file-vs-dir) under one parent → rejected
- Create owned under invalid focus → owner lands under nearest valid ancestor, ref at focus
- Sync: create stub, reuse match, ignore refs, kind conflict, skip `.git`
- Delete owned file with refs → owner+refs gone, no promotion; normal-node promotion preserved
- Expand unparsed → children attached, `Parsed`
- Stale on newer mtime → stale, no auto-reparse
- Commit scoped to `{label}/` only

## Verify

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SpecialPlacementTests|FullyQualifiedName~WorkspaceTreeSyncTests|FullyQualifiedName~ModelTests|FullyQualifiedName~DeleteOpsTests"
```