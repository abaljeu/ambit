# 31 — Skip workspace-inventory when Unloaded

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context

Load still POSTs `/_desktop/workspace-inventory` before push. When the Workspace Node is Unloaded, stub ops are empty and the inventory items are unused. [[src/Client/UpdateWorkspaceSync.fs]] `startWorkspacePush` always emits `ContinueWorkspaceStubsThenPush`. [[src/Client/App.fs]] always posts inventory. Verified remaining on 2026-09-02.

## What to build

When childrenStatus is Unloaded, Load skips the inventory request and continues to push. Stub skip for empty ops stays.

- [ ] Unloaded Load does not POST `/_desktop/workspace-inventory`.
- [ ] Loaded Load still inventories when stubs are required.
- [ ] Empty stub path still skips the structure `/changes` POST.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Inventory skip is not in code yet.

## See also

[[tmp/load-performance-audit.md]], [[src/Shared/WorkspaceUploadStructure.fs]]
