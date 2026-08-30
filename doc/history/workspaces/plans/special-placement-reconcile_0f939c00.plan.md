---
name: special-placement-reconcile
overview: "Implement Slice 1 owned-special placement enforcement with explicit reconciliation: illegal owned File/Directory occurrences become refs in place, while the owner occurrence moves upward to a valid Workspace/Directory location."
todos:
  - id: add-special-placement-tests
    content: Add failing Shared.Tests coverage for owned Directory/File reconciliation and placement rejection
    status: pending
  - id: add-special-placement-module
    content: Add Shared SpecialPlacement planner helpers and register the file in the project
    status: pending
  - id: enforce-graph-placement
    content: Update Graph.replace to reject invalid owned Directory/File placements while leaving refs unrestricted
    status: pending
  - id: wire-reconciliation-callers
    content: Use explicit reconciliation ops at command/import boundaries that may encounter invalid owned specials
    status: pending
  - id: update-affected-tests
    content: Update existing Shared tests that intentionally construct File/Directory ownership cases
    status: pending
  - id: verify-focused-suite
    content: Run focused Shared.Tests filters, then shared suite if focused tests pass
    status: pending
isProject: false
---

# Implement Special Placement Reconciliation

Assumptions for this plan:
- “Special node where it shouldn’t be” means an owned `Special Directory` or `Special File` whose owner parent is not `Special Workspace` (including ROOT) or `Special Directory`.
- Reconciliation targets the nearest valid owner ancestor that can accept the node without a sibling-name conflict. If the nearest valid ancestor conflicts, continue upward; if no valid non-conflicting owner exists, return an error and surface it through the existing status/error path.
- Reconciliation is represented as explicit `Op.Replace` operations, not hidden graph mutation, so undo/history and sync see the same changes users see.

## Code Shape

Add a small Shared module, likely [src/Shared/SpecialPlacement.fs](src/Shared/SpecialPlacement.fs), compiled after [src/Shared/Model.fs](src/Shared/Model.fs) in [src/Shared/Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj).

The module should expose pure helpers:
- `isValidOwnedSpecialParent graph parentId childId` for owned `Directory` / `File` placement.
- `nearestValidOwnerTarget graph illegalParentId childId` walking the owner chain upward.
- `planReconcileOwnedSpecial graph illegalParentId index child` producing:
  - replace illegal owner with a `Ref` at the same parent/index
  - insert the original `Owner` under the chosen valid ancestor using `Graph.fileTreeInsertIndex`
- `planReconcileGraph graph` for legacy/imported graphs, scanning owner occurrences and producing a deterministic batch of reconciliation ops.

Keep `Workspace` placement exactly as current: named workspaces only under `Graph.workspacesId`; `Workspaces` and TRASH permanence rules remain in [src/Shared/Model.fs](src/Shared/Model.fs).

## Enforcement Path

Update [src/Shared/Model.fs](src/Shared/Model.fs) `Graph.replace` placement validation so owned `Directory` / `File` insertions under invalid parents are rejected with a clear message. This keeps low-level graph invariants simple.

Use reconciliation planners at command/import boundaries that may encounter old or invalid placement:
- For explicit reconciliation of an existing graph, apply `SpecialPlacement.planReconcileGraph` as a normal change batch.
- For insert/move planners that intentionally place a File/Directory under File/Normal, generate reconciliation ops instead of a raw illegal owner insertion.
- Leave `Ref` placement unrestricted.

Existing relevant code:
```474:482:src/Shared/Model.fs
                let placementError =
                    newChildren
                    |> List.tryPick (fun child ->
                        let childNode = graph.nodes.[child.id]

                        match childNode.kind with
                        | Special Workspace when parentId <> workspacesId ->
                            Some "Workspace nodes may only be placed under Workspaces"
                        | _ -> None)
```

## Reconciliation Semantics

For a tree like `Workspace -> Normal -> Directory(owner)`, reconciliation produces `Workspace -> Directory(owner)` and `Normal -> Directory(ref)`.

For `Workspace -> FileA -> FileB(owner)`, reconciliation produces `Workspace -> FileA(owner), FileB(owner)` and `FileA -> FileB(ref)`.

For `Workspace -> Directory -> Normal -> File(owner)`, reconciliation moves the File owner to the Directory and leaves a File ref under the Normal.

Do not rename during reconciliation. If all valid ancestors have an owner-name conflict for the same file/directory name, fail the reconciliation with a targeted error rather than silently changing identity or disk path.

## Tests First

Add [tests/Shared.Tests/SpecialPlacementTests.fs](tests/Shared.Tests/SpecialPlacementTests.fs) and register it in [tests/Shared.Tests/Gambol.Shared.Tests.fsproj](tests/Shared.Tests/Gambol.Shared.Tests.fsproj).

Core tests:
- Directory owner under Normal reconciles to owner under nearest Workspace/Directory and ref in original slot.
- File owner under File reconciles to owner under nearest Workspace/Directory and ref in original slot.
- Nested illegal placements reconcile deterministically without producing duplicate owners.
- Ref occurrences under File/Normal remain legal and are not moved.
- Sibling name conflicts try the next valid ancestor, then fail if no valid target exists.
- `Graph.replace` rejects new owned Directory/File placements under File/Normal after enforcement.

Update existing tests that currently build File-owning-Directory graphs through `Graph.replace`, especially in [tests/Shared.Tests/DocumentAssemblyTests.fs](tests/Shared.Tests/DocumentAssemblyTests.fs) and [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs). Where those tests need legacy invalid graphs, build them through raw node maps plus `Graph.fromNodes`, then run reconciliation explicitly.

## Verification

Run focused tests first:
```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SpecialPlacementTests|FullyQualifiedName~ModelTests|FullyQualifiedName~DocumentPathMoveTests|FullyQualifiedName~DocumentAssemblyTests"
```

Then run the shared suite in the background if the focused tests pass:
```bash
./scripts/test.sh shared
```