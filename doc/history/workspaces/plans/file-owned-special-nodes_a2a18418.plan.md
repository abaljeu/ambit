---
name: file-owned-special-nodes
overview: Implement the Stage 2 correction by allowing `Special File` to own `Special Directory` and `Special File` nodes, with tests first and minimal targeted updates.
todos:
  - id: add-failing-placement-tests
    content: Add tests for file-owned directory/file placement in ModelTests
    status: completed
  - id: update-graph-placement-rule
    content: Allow Special File as parent for Special Directory/Special File owner edges in Model.replace
    status: completed
  - id: run-focused-shared-tests
    content: Execute ModelTests, FileNodeOpsTests, and FilePathResolveTests
    status: completed
  - id: sync-roadmap-status-line
    content: Update workspace-file-model Stage 2 correction status if implementation is complete
    status: completed
isProject: false
---

# Implement File-Owned Special Node Placement

## Goal

Enable free-form ownership below `workspace` by updating graph placement invariants so `Special File` may own `Special Directory` and `Special File` (owner edges), then verify related file-creation flows still pass.

## Scope

- Update invariant enforcement in [src/Shared/Model.fs](src/Shared/Model.fs) only where placement is checked.
- Add/adjust tests in [tests/Shared.Tests/ModelTests.fs](tests/Shared.Tests/ModelTests.fs) to codify new allowed placements.
- Run focused shared tests covering invariants and file-node planning behavior.
- Optionally update status wording in [doc/roadmap/workspace-file-model.md](doc/roadmap/workspace-file-model.md) if implementation reaches the stated correction.

## Implementation Steps (TDD)

1. Add failing tests first in [tests/Shared.Tests/ModelTests.fs](tests/Shared.Tests/ModelTests.fs):
   - `Graph.replace accepts Special Directory under Special File`.
   - `Graph.replace accepts Special File under Special File`.
   - Keep existing rejection for normal parents unchanged.
   - Verify: tests fail against current invariant.

2. Update placement logic in [src/Shared/Model.fs](src/Shared/Model.fs):
   - In `Graph.replace` placement check, extend allowed parent kinds for owner placement of `Special Directory`/`Special File` from `{Workspace, Directory}` to `{Workspace, Directory, File}`.
   - Keep all existing `Workspace` under `Workspaces` and canonical root/trash/workspaces protections unchanged.
   - Verify: new tests pass; prior invariant tests still pass.

3. Run focused regression tests:
   - [tests/Shared.Tests/ModelTests.fs](tests/Shared.Tests/ModelTests.fs)
   - [tests/Shared.Tests/FileNodeOpsTests.fs](tests/Shared.Tests/FileNodeOpsTests.fs)
   - [tests/Shared.Tests/FilePathResolveTests.fs](tests/Shared.Tests/FilePathResolveTests.fs)
   - Verify: no regressions in file planning/resolution behavior.

4. Align roadmap status note if now true:
   - In [doc/roadmap/workspace-file-model.md](doc/roadmap/workspace-file-model.md), update Stage 2 correction line only if tests confirm implementation.
   - Keep edit surgical; no broader doc rewrite.

## Success Criteria

- Graph invariant permits owner placement of `Special Directory`/`Special File` under `Special File`.
- Existing canonical/root/workspaces/trash protections remain enforced.
- Relevant shared tests pass.
- Roadmap status line reflects reality (if changed).