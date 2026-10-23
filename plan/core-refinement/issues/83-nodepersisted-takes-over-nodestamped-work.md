# 83 — NodePersisted takes over NodeStamped's work

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md)
- [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md)

## 3. Look at
- Files:
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Core/PersistThread.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/Core/DbAgent.fs`
  - `tests/Server.Tests/PersistThreadTests.fs`
  - `tests/Server.Tests/CoreMailboxDoorTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
- Types:
  - `Notice.NodePersisted`, `Notice.NodeStamped`
  - `PersistHandlers`
  - `DbAgent.LoadedPersist`
- Functions:
  - `CoreMailboxLoad.applyNotice` (the `NodePersisted` and `NodeStamped` arms), `CoreMailboxLoad.writePersisted`
  - `PersistThread.runRequest`
  - `FileAgent.persistCollected`, `FileAgent.persistPostChange`
  - `DbAgent.noteStamped`, `DbAgent.requestFileDiff`, `DbAgent.trimDeletedIds`, `DbAgent.applyMaintenance`, `DbAgent.commitPostChange`

## 4. Context
After [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md) and [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md), `Notice.NodeStamped` has two posts and one handler that does work.

- Posts:
  - `PersistThread.runRequest` posts `Notice.NodePersisted`, then `Notice.NodeStamped`, after each successful write.
  - `FileAgent.persistCollected` posts `Notice.NodeStamped` for an Edit with no owning special node. It does nothing else.
- The `NodeStamped` arm of `applyNotice` logs it, keeps the graph, and calls `persist.noteStamped ()`.
- `FileAgent.persistHandlers` sets `noteStamped = ignore`.
- `DbAgent.noteStamped` copies the graph to `baselineGraph`. When `diffWorkspaces` is not empty it requests the file diff, or sets `diffNeeded` while a diff is in progress.
- Nothing reads `DbAgent.baselineGraph`.
- Only `Notice.NodePersisted` sets a node Persisted.

## 5. Changes
- The `NodePersisted` arm of `applyNotice` calls `persist.noteStamped ()` after `writePersisted` returns `Ok`. On `DbAgent` this runs the file-diff request. On `FileAgent` it does nothing.
- The `NodeStamped` arm keeps logging and keeps the graph. It stops calling `persist.noteStamped`.
- `PersistThread.runRequest` posts only `Notice.NodePersisted` after a successful write.
- Delete `FileAgent.persistCollected` and its call in `persistPostChange`.
- `DbAgent.noteStamped` keeps only the file-diff request. Delete `LoadedPersist.baselineGraph` and each write to it in `makeLoaded`, `trimDeletedIds`, `applyMaintenance` and `commitPostChange`.

## 6. Tests
- Change `persist thread writes from the live graph` in `tests/Server.Tests/PersistThreadTests.fs`. It checks `Notice.NodePersisted` and that no `Notice.NodeStamped` is posted.
- Change `persist thread posts NodePersisted on success only` in the same file. A success posts `[ Notice.NodePersisted ]`.
- Change `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. It waits for `Notice.NodePersisted` only, and the events after the stored one are `[ Notice.NodePersisted ]`.
- The helper `persistTipSettled` in `tests/Server.Tests/StateEndpointTests.fs` stops requiring `persisted = stamped`.
- Filters:
  - `scripts/agent-test.sh --filter "PersistThreadTests"`
  - `scripts/agent-test.sh --filter "CoreMailboxApi.postEvent appends an Ev that eventsSince returns"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`

## 7. Done
- The named tests pass.
- Only tests post `Notice.NodeStamped`.
- No file under `src/` names `persistCollected` or `baselineGraph`.
- Upload, import, parse, Load, undo and persist complete.
