# 83 — NodePersisted takes over NodeStamped's work

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md)

## 3. Look at
- Files:
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Core/PersistThread.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/Core/DbAgent.fs`
  - `src/Server/Bookkeeping.fs`
  - `tests/Server.Tests/PersistThreadTests.fs`
  - `tests/Server.Tests/CoreMailboxDoorTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/FileAgentFailureTests.fs`
- Types:
  - `Notice.NodePersisted`, `Notice.NodeStamped`
  - `PersistHandlers`
  - `FileAgent.LoadedFile`, `DbAgent.LoadedPersist`
- Functions:
  - `CoreMailboxLoad.applyNotice` (the `NodePersisted` and `NodeStamped` arms), `CoreMailboxLoad.writePersisted`
  - `PersistThread.runRequest`
  - `FileAgent.persistCollected`, `FileAgent.persistPostChange`, `FileAgent.takeCheckpoint`, `FileAgent.noteStamped`
  - `DbAgent.noteStamped`, `DbAgent.requestFileDiff`, `DbAgent.countDiffMark`
  - `Bookkeeping.writeEventId`

## 4. Context
After [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md), `Notice.NodeStamped` has two posts and two handlers.

- Posts:
  - `PersistThread.runRequest` posts `Notice.NodePersisted`, then `Notice.NodeStamped`, after each successful write.
  - `FileAgent.persistCollected` posts `Notice.NodeStamped` for an Edit with no owning special node.
- The `NodeStamped` arm of `applyNotice` logs it, keeps the graph, and calls `persist.noteStamped ()`.
- `FileAgent.noteStamped` takes the oldest id from `pendingCheckpoint`. When `persistClean` is true it writes that id with `Bookkeeping.writeEventId` and copies the graph to `lastPersisted`. A failed write sets `persistClean` false.
- `DbAgent.noteStamped` copies the graph to `baselineGraph`. When `diffWorkspaces` is not empty it requests the file diff, or sets `diffNeeded` while a diff is in progress.
- Nothing reads `FileAgent.lastPersisted` or `DbAgent.baselineGraph`.
- `pendingCheckpoint` gains one id per Edit and loses one per stamp, so the checkpoint can name an Edit whose file is not yet written.

## 5. Changes
- The `NodePersisted` arm of `applyNotice` calls `persist.noteStamped ()` after `writePersisted` returns `Ok`.
- The `NodeStamped` arm keeps logging and keeps the graph. It stops calling `persist.noteStamped`.
- `PersistThread.runRequest` posts only `Notice.NodePersisted` after a successful write.
- `FileAgent.noteStamped` writes the checkpoint by this rule:

  ```fsharp
  // runs on NodePersisted, after Persisted is written
  let settled =
      graph.nodes |> Map.forall (fun _ n -> n.persistState <> PersistState.Unpersisted)
  if persistClean && settled then
      match Bookkeeping.writeEventId dataDir state.eventId with
      | Ok () -> ()
      | Error _ -> persistClean <- false
  ```

- Delete from `FileAgent`:
  - `persistCollected` and its call in `persistPostChange`
  - `pendingCheckpoint` and `takeCheckpoint`
  - `lastPersisted`
- `DbAgent.noteStamped` keeps only the file-diff request. Delete `LoadedPersist.baselineGraph` and each write to it in `trimDeletedIds`, `applyMaintenance` and `commitPostChange`.

## 6. Tests
- Change `persist thread writes from the live graph` in `tests/Server.Tests/PersistThreadTests.fs`. It checks `Notice.NodePersisted` and that no `Notice.NodeStamped` is posted.
- Change `persist thread posts NodePersisted on success only` in the same file. A success posts `[ Notice.NodePersisted ]`.
- Change `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. It waits for `Notice.NodePersisted` only, and the events after the stored one are `[ Notice.NodePersisted ]`.
- Add `NodePersisted writes the checkpoint when no node is Unpersisted` in `tests/Server.Tests/FileAgentFailureTests.fs`. An Edit to one File Node is written, and `Bookkeeping.readEventId` returns that Edit's id. A second File Node left Unpersisted keeps the checkpoint at its earlier value.
- The helper `persistTipSettled` in `tests/Server.Tests/StateEndpointTests.fs` stops requiring `persisted = stamped`.
- Filters:
  - `scripts/agent-test.sh --filter "PersistThreadTests"`
  - `scripts/agent-test.sh --filter "CoreMailboxApi.postEvent appends an Ev that eventsSince returns"`
  - `scripts/agent-test.sh --filter "NodePersisted writes the checkpoint when no node is Unpersisted"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`
  - `scripts/agent-test.sh --filter "soft-fail log is replayed into FileAgent state after restart"`

## 7. Done
- The named tests pass.
- Only tests post `Notice.NodeStamped`.
- No file under `src/` names `persistCollected`, `pendingCheckpoint`, `lastPersisted` or `baselineGraph`.
- Upload, import, parse, Load, undo and persist complete.
