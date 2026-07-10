---
name: workspace-scale-slice1
overview: "Implement the documented Workspace Scale Import Slice 1 end to end: ownership-derived paths, reconciliation of illegal owned specials, shallow sync, metadata, expand-to-parse, stale handling, workspace git commands, and tests."
todos:
  - id: align-docs-with-reconciliation
    content: Update Slice 1 docs to replace abort-only illegal-owner behavior with explicit reconciliation semantics
    status: pending
  - id: placement-reconciliation-core
    content: Add Shared placement classifier and reconciliation planner for owned File/Directory specials
    status: pending
  - id: graph-placement-enforcement
    content: Enforce owned File/Directory parent restrictions in Graph.replace while leaving refs unrestricted
    status: pending
  - id: wire-command-boundaries
    content: Wire insert/move/reconcile command planning so invalid owner intent becomes owner-up/ref-here ops
    status: pending
  - id: file-metadata-model
    content: Add minimal parsed/stale/mtime metadata model and persistence path
    status: pending
  - id: shallow-sync-planner
    content: Add Shared shallow sync planner for immediate disk children and owned stubs
    status: pending
  - id: server-desktop-sync-io
    content: Add server and optional desktop directory-listing I/O for sync-tree commands
    status: pending
  - id: expand-to-parse
    content: Implement one-file expand-to-parse behavior and metadata updates
    status: pending
  - id: stale-reparse-flow
    content: Implement stale detection and explicit reparse behavior without auto-replacing children
    status: pending
  - id: workspace-git-commands
    content: Add minimal status/commit commands scoped to the workspace repo
    status: pending
  - id: test-and-verify
    content: Add/update focused tests and run targeted then shared verification
    status: pending
isProject: false
---

# Workspace Scale Import Slice 1 Implementation

This replaces the narrow placement-only plan with an end-to-end implementation plan for the documented Slice 1 work.

Assumptions:
- Reconciliation is now part of the Slice 1 target: when an owned `Special File` or `Special Directory` is found under an invalid owner, leave a `Ref` at that outline location and move the single `Owner` occurrence upward to a valid `Workspace` or `Directory` owner.
- The reconciliation target is the nearest valid ancestor that can accept the node without an owned-name conflict. If that ancestor conflicts, walk upward. If no valid target exists, return a user-visible error.
- Reconciliation is explicit `Op.Replace` changes, not hidden mutation inside `Graph.fromNodes`, so sync, undo, and history stay understandable.
- Full git gateway pull/push remains Slice 2; Slice 1 only needs local/manual git status/commit for the workspace root.

## 1. Align The Docs

Update the docs that still say illegal owner moves abort so the implementation target is unambiguous:
- [doc/roadmap/workspace-scale-import-slice1-plan.md](doc/roadmap/workspace-scale-import-slice1-plan.md)
- [doc/roadmap/revising-workspace-file-model.md](doc/roadmap/revising-workspace-file-model.md)
- [doc/current/workspace-graph.md](doc/current/workspace-graph.md)

The docs should say: Graph validation rejects newly planned illegal owners, but the Slice 1 reconciliation command repairs existing illegal owners by replacing the illegal owner occurrence with a ref and moving the owner to the nearest valid Workspace/Directory ancestor.

## 2. Shared Placement And Reconciliation Core

Add a pure Shared module, likely [src/Shared/SpecialPlacement.fs](src/Shared/SpecialPlacement.fs), and register it after [src/Shared/Model.fs](src/Shared/Model.fs) in [src/Shared/Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj).

Core responsibilities:
- Classify legal owned placement for `Workspace`, `Directory`, `File`, `Normal`, `Workspaces`, and canonical nodes.
- Detect illegal owned `Directory` / `File` occurrences.
- Plan reconciliation ops: replace illegal owner child with `Ref`; insert the `Owner` occurrence under the nearest valid ancestor using `Graph.fileTreeInsertIndex`.
- Preserve refs anywhere.
- Report deterministic errors for sibling name conflicts when no valid ancestor can accept the moved owner.

Then update [src/Shared/Model.fs](src/Shared/Model.fs) `Graph.replace` to enforce the target invariant for new accepted graph operations: owned `Directory` / `File` children may only be placed under `Workspace` including ROOT, or `Directory`.

## 3. Command And Import Boundaries

Wire reconciliation where invalid ownership can be encountered intentionally:
- Add a reconciliation command/planner that scans the current graph and posts the explicit ops.
- Update insert/create flows in [src/Shared/FileNodeOps.fs](src/Shared/FileNodeOps.fs): creating an owned File/Directory while focused under File/Normal should either create under the nearest valid ancestor and place a ref at focus, or use the reconciliation planner to produce that same shape.
- Update move/reparent command planning so illegal owner moves become reconciliation-shaped ops when that is the intended UX, rather than raw invalid `Graph.replace` operations.
- Keep low-level `Graph.replace` rejection for direct invalid ops from tests, sync, or malformed callers.

## 4. Metadata Model

Add minimal Slice 1 file metadata. Prefer a small Shared type/module rather than spreading ad hoc fields:
- `parsed: bool`
- `stale: bool`
- `sourceMtimeUtc: int64 option`
- optional `sourceHash: string option`
- optional `size: int64 option` and format hint only if an existing persistence slot makes this cheap

Decide where this metadata lives by inspecting current node persistence: either extend node metadata if there is an established field, or add a focused sidecar/persistence structure if that keeps the graph model cleaner. The first implementation should use mtime before hash unless tests show mtime is insufficient.

## 5. Shallow Sync Planner

Add a pure Shared planner for one directory at a time, likely near [src/Shared/FileNodeOps.fs](src/Shared/FileNodeOps.fs) or a new `WorkspaceTreeSync` module.

Input:
- Workspace/Directory node id
- immediate disk entries with name, kind, mtime, size/hash if available
- current graph

Behavior:
- Match only owned immediate `Directory` / `File` children by name.
- Reuse same-name same-kind owned children.
- Create owned stubs for missing disk children.
- Report same-name kind conflicts.
- Ignore refs elsewhere completely.
- Skip `.git` and a minimal ignore/noise set.
- Do not auto-delete graph children missing on disk; optionally mark missing later.

## 6. Server And Desktop I/O

Server side:
- Add or extend a route/handler that reads immediate children under `DataDir/@label/...` and passes them to the Shared planner.
- Reuse existing Stage 7/8 `DataDir` path helpers and `DocumentPathMove` behavior where possible.
- Keep PostgreSQL authoritative for graph identity; disk is observed only for this explicit sync command.

Desktop side:
- If implemented in Slice 1, add a LocalProxy call to list immediate children under a mapped workspace root.
- Use the same Shared planner as server sync.
- Keep existing import/export and file-status behavior intact.

Client side:
- Add a workspace/directory command such as “Sync tree” that calls the chosen server/desktop listing endpoint and submits the planned ops.
- Surface conflicts and reconciliation errors through existing status/error UI.

## 7. Expand To Parse

Implement file expansion behavior:
- If a `Special File` is unparsed, read that one file from server `DataDir` or desktop mapped root.
- Dispatch through existing document format code such as [src/Shared/DocumentFormat.fs](src/Shared/DocumentFormat.fs), [src/Shared/AmbDocument.fs](src/Shared/AmbDocument.fs), and existing plain text/XML planning as appropriate.
- Attach parsed children under the file node only.
- Set `parsed = true`, `stale = false`, and store current mtime/hash.
- Do not parse every file during tree sync.

## 8. Stale And Reparse

On file expand or explicit reparse:
- Compare disk mtime/hash to stored metadata.
- If disk is newer and the file is already parsed, mark `stale = true` and show an indicator/action.
- Do not auto-replace parsed children.
- Add explicit reparse later in the same slice if the stale indicator is otherwise a dead end.

This preserves the documented “safe if external editor changed the file” behavior without solving annotation migration yet.

## 9. Autosave And Workspace Git

Autosave:
- Reuse existing live-save/document persistence paths so edits under parsed file nodes serialize back to the owning file artifact.
- Verify that reconciliation changes path ownership before persistence moves run, so `DocumentPathMove` sees the intended disk path.

Git:
- Add minimal workspace git commands for `status` and `commit` scoped to `DataDir/@label/` or the desktop mapped root when it is a git repo.
- Initialize a repo only on first need if documented behavior requires it.
- Do not implement pull/push, JIT commit, or gateway protocol in this slice.

## 10. Tests

Use TDD with Shared tests first.

Add focused tests for:
- `Graph.replace` accepts owned File/Directory under Workspace/Directory.
- `Graph.replace` rejects owned File/Directory under File/Normal.
- Refs to File/Directory remain legal under File/Normal.
- Reconciliation of `Normal -> Directory(owner)` leaves `Normal -> Directory(ref)` and moves owner under nearest valid ancestor.
- Reconciliation of `FileA -> FileB(owner)` leaves `FileA -> FileB(ref)` and moves owner under nearest Workspace/Directory.
- Reconciliation walks past conflicting ancestors and fails clearly if no valid target exists.
- Shallow sync creates stubs, reuses owned matches, ignores refs, skips `.git`, and reports kind conflicts.
- Delete of owned File/Directory does not promote refs to owners.
- Expand unparsed file parses one file only and sets metadata.
- Stale detection marks stale without auto-reparse.
- Git command scope is the workspace repo, not the whole `DataDir`.

Likely affected existing tests:
- [tests/Shared.Tests/ModelTests.fs](tests/Shared.Tests/ModelTests.fs)
- [tests/Shared.Tests/WorkspaceOpsTests.fs](tests/Shared.Tests/WorkspaceOpsTests.fs)
- [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs)
- [tests/Shared.Tests/DocumentAssemblyTests.fs](tests/Shared.Tests/DocumentAssemblyTests.fs)
- [tests/Shared.Tests/DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs)

## Verification

Run focused Shared tests first:
```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SpecialPlacementTests|FullyQualifiedName~WorkspaceTreeSyncTests|FullyQualifiedName~ModelTests|FullyQualifiedName~DocumentPathMoveTests|FullyQualifiedName~DocumentAssemblyTests|FullyQualifiedName~DeleteOpsTests"
```

Run server/desktop focused tests only after I/O endpoints are touched, then run the shared suite in the background:
```bash
./scripts/test.sh shared
```

Manual success check: attach or open a medium repo, sync tree, see stubs only, expand one file, edit and autosave it, create a ref to that file under a note, reconcile any illegal owned specials, commit only that workspace repo, restart, and confirm paths still derive from Workspace/Directory ownership.