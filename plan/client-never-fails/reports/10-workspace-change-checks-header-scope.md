# 10 — Workspace change checks Header scope

Slice: planStubOps checks the workspace Header. The isLoaded skip goes.

## Signatures

[ ] `WorkspaceUploadStructure.planStubOps` in `src/Shared/WorkspaceUploadStructure.fs` returns `Ok []` when the workspace Header is present and `GraphChildren.isLoaded` is false.

## Match sites

[ ] A missing workspace Header falls through to stub planning.
[ ] The skip exists so an empty child list would not invent ids that collide with server names.

## Existing tests

[ ] ``Load Unloaded stub plan must not name-conflict on resident server`` in [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]] expects no stub ops while the workspace child list is absent.
[ ] ``planStubOps creates Directory named .agents with file child`` in `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` plans stubs when the workspace child list is present.

## Copy-from

[ ] `workspaceByLabel` is the lookup `planStubOps` already uses before the loaded check.

## write-ticket

### Source files

[ ] [[src/Shared/WorkspaceUploadStructure.fs]] — `planStubOps`.

### Tests

[ ] Change ``Load Unloaded stub plan must not name-conflict on resident server`` so the gate is the workspace Header.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
