# 82 — The mailbox pushes an Edit's special nodes

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [72 — Push parsed nodes to the persist stack](72-push-parsed-nodes-to-the-persist-stack.md). It adds `PersistHandlers.pushPersist` and `PersistRequestStack.pushBoundNode`.

## 3. Look at
- Files:
  - `src/Server/Core/CoreEventDispatch.fs`
  - `src/Server/Core/PersistThread.fs`
  - `src/Shared/DocumentOpImpact.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/Core/DbAgent.fs`
  - `src/Server/Core/CoreMsg.fs` (where `PersistHandlers` is defined)
  - `tests/Server.Tests/TestBackend.fs`
  - `tests/Server.Tests/FileAgentFailureTests.fs`
- Types:
  - `PersistHandlers`
  - `CoreEventDispatch.Context`
  - `PersistRequest`
  - `Notice.NodePersisting`
- Functions:
  - `CoreEventDispatch.persist`, `CoreEventDispatch.withConfirmedOps`
  - `PersistThread.opAnchor`, `PersistThread.owningSpecials` (and its private `ownerOf`)
  - `FileAgent.persistCollected`, `FileAgent.openForWrite`, `FileAgent.markUnpersisted`, `FileAgent.persistedNode`, `FileAgent.pushIds`
  - `DbAgent.submitLiveOps`, `DbAgent.commitPostChange`, `DbAgent.takePendingNotify`, `DbAgent.noteWriteFailed`, `DbAgent.noteStamped`, `DbAgent.workspacesTouched`
  - `GraphMutate.setPersistState`
  - The helper that pushes today: `PersistRequestStack.pushBoundNode`, reached as `persist.pushPersist`.

## 4. Context
An accepted Edit reaches the persist stack through a collector inside each agent's `applyEvent`.

- `FileAgent.persistCollected` runs after `FileAgent.processPostEvents` applies a non-graph-only Change. It
  - appends the event id to `pendingCheckpoint`
  - finds the owning special nodes of the applied ops with `PersistThread.owningSpecials`
  - sets each Persisted one Unpersisted in `loaded.state` through `openForWrite`, because `Op.NewSpecialNode` creates a node Persisted and the persist thread skips a Persisted node
  - posts `Notice.NodeStamped` when there are no owning special nodes
  - pushes each owning special node with `pushIds`
- `DbAgent.submitLiveOps` stores the owning special nodes in `pendingNotify` when live save has a data directory. `DbAgent.commitPostChange` pushes them after the projection write. `DbAgent` does not set a new special node Unpersisted, so its first write is skipped. In database-only mode `DbAgent` pushes nothing for an Edit.
- In production only `DbAgent` takes an Edit. A File-mode server makes its browser door read-only (`RouteRegistration.isWritable`). Tests post Edits to a `FileAgent` host.
- `CoreEventDispatch` is compiled before `PersistThread`, so it cannot call `PersistThread.owningSpecials`.

## 5. Changes
- Move `opAnchor`, `ownerOf` and `owningSpecials` from `PersistThread` to `DocumentOpImpact`. `owningSpecials` and `opAnchor` stay public. Callers that change:
  - `FileAgent.persistCollected` (`owningSpecials`)
  - `DbAgent.submitLiveOps` (`owningSpecials`)
  - `DbAgent.workspacesTouched` (`opAnchor`)
- In `CoreEventDispatch.persist`, after `context.persist.applyEvent` returns `Ok` for a Change that is not graph-only:
  - read the state with `context.persist.getState`
  - take the ops of the confirmed event (`withConfirmedOps`)
  - find their owning special nodes with `DocumentOpImpact.owningSpecials` on that state's graph
  - set each Persisted one Unpersisted with `GraphMutate.setPersistState` and `context.persist.replaceGraph (Notice.NodePersisting nodeId)`, so `DbAgent` writes its `persist_state` row
  - call `context.persist.pushPersist` for each owning special node, Parsed or Unparsed
- A graph-only Change pushes nothing.
- The Edit path sets no node Persisted. Only the persist thread's `Notice.NodePersisted` sets Persisted.
- `FileAgent.persistCollected` keeps the `pendingCheckpoint` append and the `Notice.NodeStamped` post when there are no owning special nodes. It stops marking and pushing. Delete `FileAgent.openForWrite`, `FileAgent.markUnpersisted`, `FileAgent.persistedNode` and `FileAgent.pushIds`. [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md) removes the append.
- Delete the `DbAgent` push for an Edit:
  - `submitLiveOps` stops setting `pendingNotify`
  - `commitPostChange` stops reading and pushing `pendingNotify`
  - delete `LoadedPersist.pendingNotify` and `takePendingNotify`
  - `noteWriteFailed` and `noteStamped` stop reading `pendingNotify`

## 6. Tests
- Add `An Edit pushes its owning special node` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. A host built on `TestBackend.cannedHandlers` with a recording `pushPersist` posts an Edit to a File Node's text. The recorded ids are that File Node.
- Add `A graph-only change pushes nothing` in the same file. The same Edit posted graph-only records no id.
- Keep `new special node writes its file without NodePersisting` in `tests/Server.Tests/FileAgentFailureTests.fs` passing unchanged. It now proves the mailbox mark.
- Filters:
  - `scripts/agent-test.sh --filter "An Edit pushes its owning special node"`
  - `scripts/agent-test.sh --filter "A graph-only change pushes nothing"`
  - `scripts/agent-test.sh --filter "new special node writes its file without NodePersisting"`
  - `scripts/agent-test.sh --filter "PersistThreadTests"`

## 7. Done
- The three named tests pass.
- `PersistThread.fs` defines no `owningSpecials` or `opAnchor`.
- No file under `src/` names `openForWrite`, `pendingNotify` or `takePendingNotify`.
- Upload, import, parse, Load, undo and persist complete.
