# 10 — Reject an unparsed persist request (scope)

Slice: [node-move-disk arch](../arch.md) section Persist thread write requires `PersistThread.runRequest` to reject the top stack request when `PersistThread.unparsedNode` is true for that node. It must not call `persistNode`. Today's `runRequest` checks only `CoreEventDispatch.persistedNode`. The Unparsed check was dropped when [71 — Persist thread takes from the stack](../../core-refinement/issues/71-persist-thread-takes-from-the-stack.md) moved the thread to the stack. This rule is separate from the node enclosure lock.

## Signatures

[ ] `PersistThread.unparsedNode : Graph -> NodeId -> bool` in `src/Server/Core/PersistThread.fs` is `internal`. It is true when the node exists and `node.documentState.parseState` is `ParseState.Unparsed`.
[ ] `PersistThread.runRequest` is `private` in `src/Server/Core/PersistThread.fs` with type `PersistThreadDeps -> PersistRequest -> unit`.
[ ] `PersistThreadDeps` holds `currentState : unit -> GraphAtEvent`, `take : unit -> PersistRequest option`, `persistNode : Graph -> NodeId -> Result<DateTime, string>`, and `postNotice : Notice -> unit`.
[ ] `PersistRequest` in `src/Server/Core/PersistRequestStack.fs` is `{ nodeId: NodeId }`.
[ ] `CoreEventDispatch.persistedNode : Graph -> NodeId -> bool` in `src/Server/Core/CoreEventDispatch.fs` is true when the node exists and `persistState` is `PersistState.Persisted`.
[ ] `PersistThread.start : PersistThreadDeps -> unit -> unit` is `internal`. It starts the background thread that runs `loop`, which calls `runRequest`.
[ ] `PersistThread.postWhenBound : (Notice -> unit) option ref -> Notice -> unit` is `internal`. It posts when the slot holds `Some post`.

## Match sites

[ ] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` calls `deps.currentState ()`, then `CoreEventDispatch.persistedNode state.graph request.nodeId`. When `stillPersisted` is false it posts `Notice.NodePersisting`, calls `deps.persistNode`, then posts `Notice.NodePersisted` or `Notice.PersistFailed`. It has no call to `unparsedNode`.
[ ] `PersistThread.loop` in `src/Server/Core/PersistThread.fs` calls `runRequest deps request` inside `try`, then always calls `ServerRunRegistry.notePersistFinished ()` in `finally` before the next `take`.
[ ] `DbAgent.startFileDiff` in `src/Server/Core/DbAgent.fs` binds `unparsed id` to `PersistThread.unparsedNode graph id`. It omits unparsed ids from the push list and holds them on `diffWorkspaces`.
[ ] `FileAgent.startPersist` in `src/Server/Core/FileAgent.fs` calls `PersistThread.start` with `currentState`, stack `take`, `persistNode`, and `PersistThread.postWhenBound loaded.persistFinished`.
[ ] `DbAgent` startup in `src/Server/Core/DbAgent.fs` (around the `PersistThread.start` block) wires the same four deps against the agent stack and `persistLive`.
[ ] Reject behavior for Unparsed should mirror the existing Persisted skip: no `persistNode` call, no `Notice.NodePersisting`, and no `Notice.PersistFailed`. The request is consumed; `loop` still runs `notePersistFinished` and takes the next stack entry.
[ ] [node-move-disk arch](../arch.md) Persist thread write says the thread rejects the request and does not call `persistNode`. It does not require a notice on that skip. [node-move-disk arch](../arch.md) Blocked persist posts no `Notice.PersistFailed`; that rule applies to the node enclosure lock, not this Unparsed reject.

## Existing tests

[ ] `tests/Server.Tests/PersistThreadTests.fs` is the dedicated PersistThread test module. It is already listed in `tests/Server.Tests/Gambol.Server.Tests.fsproj`.
[ ] `persist thread writes from the live graph` exercises `PersistThread.start`, stack take, and a real `persistNode` write. It expects `Notice.NodePersisted` only.
[ ] `repeated pushes write once` shows `runRequest` skips further writes when `CoreEventDispatch.persistedNode` is true after the first success. It posts no notice on those later takes.
[ ] `persist thread posts NodePersisted on success only` expects `Notice.NodePersisting` then `Notice.NodePersisted` on success, and `Notice.NodePersisting` then `Notice.PersistFailed` on `Error`.
[ ] `persist thread posts the version it read` and `NodePersisted carries the written file time` cover notice shape after a write.
[ ] `NodeParsed leaves the written node Persisted` wires stack push through `applyPersisted` after parse.
[ ] No test names `runRequest` or `unparsedNode` directly. No test covers an Unparsed node on the persist stack.
[ ] `DbAgent.startFileDiff` filtering through `unparsedNode` has no direct unit test in `PersistThreadTests.fs`. `doc/current/db-agents.md` documents the `startFileDiff` rule only.

## Copy-from

[ ] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` already skips a Persisted node at the `if not stillPersisted` guard. That path calls neither `persistNode` nor `postNotice`. Add an Unparsed guard in the same shape before the unpersisted write branch.

## write-ticket

### Source files

[ ] `src/Server/Core/PersistThread.fs` — `runRequest` calls `unparsedNode` on `state.graph` and `request.nodeId` after `currentState` and before the unpersisted write path (or combines guards so Unparsed never reaches `persistNode` or `Notice.NodePersisting`).

### Tests

[ ] `tests/Server.Tests/PersistThreadTests.fs` — add one fact that pushes a persist request for a special node with `parseState = ParseState.Unparsed` and `persistState = Unpersisted`, runs `PersistThread.start` with a counting `persistNode` and a notice collector, and asserts zero writes and zero notices (`NodePersisting`, `NodePersisted`, `PersistFailed`).
[ ] Optional second fact in the same file: Unparsed with `persistState = PersistState.Persisted` still rejects with no write and no notices (guard order should not matter for the observable seam).
[ ] No new test file. No `Gambol.Server.Tests.fsproj` change.
