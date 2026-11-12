# 10 — Reject an unparsed persist request
**Status**: defined
One job: The Server drops a persist request for an Unparsed node and writes nothing for it. That skip is the unbuilt persist-thread rule behind story 1 Derived-path comparison in [Node move and disk](plan/node-move-disk/spec.md), so an Unparsed node is not written out as a second file.

Type: bug-fixing
Blockers: none

## Scope

Slice: [node-move-disk arch](plan/node-move-disk/arch.md) section Persist thread write requires `PersistThread.runRequest` to reject the top stack request when `PersistThread.unparsedNode` is true for that node. It must not call `persistNode`. Today's `runRequest` checks only `CoreEventDispatch.persistedNode`. The Unparsed check was dropped when [71 — Persist thread takes from the stack](plan/core-refinement/issues/71-persist-thread-takes-from-the-stack.md) moved the thread to the stack. This rule is separate from the node enclosure lock.

### Signatures

[x] `PersistThread.unparsedNode : Graph -> NodeId -> bool` in `src/Server/Core/PersistThread.fs` is `internal`. It is true when the node exists and `node.documentState.parseState` is `ParseState.Unparsed`.
[x] `PersistThread.runRequest` is `private` in `src/Server/Core/PersistThread.fs` with type `PersistThreadDeps -> PersistRequest -> unit`.
[x] `PersistThreadDeps` holds `currentState : unit -> GraphAtEvent`, `take : unit -> PersistRequest option`, `persistNode : Graph -> NodeId -> Result<DateTime, string>`, and `postNotice : Notice -> unit`.
[x] `PersistRequest` in `src/Server/Core/PersistRequestStack.fs` is `{ nodeId: NodeId }`.
[x] `CoreEventDispatch.persistedNode : Graph -> NodeId -> bool` in `src/Server/Core/CoreEventDispatch.fs` is true when the node exists and `persistState` is `PersistState.Persisted`.
[x] `PersistThread.start : PersistThreadDeps -> unit -> unit` is `internal`. It starts the background thread that runs `loop`, which calls `runRequest`.
[x] `PersistThread.postWhenBound : (Notice -> unit) option ref -> Notice -> unit` is `internal`. It posts when the slot holds `Some post`.

### Match sites

[x] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` calls `deps.currentState ()`, then `CoreEventDispatch.persistedNode state.graph request.nodeId`. When `stillPersisted` is false it posts `Notice.NodePersisting`, calls `deps.persistNode`, then posts `Notice.NodePersisted` or `Notice.PersistFailed`. It has no call to `unparsedNode`.
[x] `PersistThread.loop` in `src/Server/Core/PersistThread.fs` calls `runRequest deps request` inside `try`, then always calls `ServerRunRegistry.notePersistFinished ()` in `finally` before the next `take`.
[x] `DbAgent.startFileDiff` in `src/Server/Core/DbAgent.fs` binds `unparsed id` to `PersistThread.unparsedNode graph id`. It omits unparsed ids from the push list and holds them on `diffWorkspaces`.
[x] `FileAgent.startPersist` in `src/Server/Core/FileAgent.fs` calls `PersistThread.start` with `currentState`, stack `take`, `persistNode`, and `PersistThread.postWhenBound loaded.persistFinished`.
[x] `DbAgent` startup in `src/Server/Core/DbAgent.fs` (around the `PersistThread.start` block) wires the same four deps against the agent stack and `persistLive`.
[ ] Reject behavior for Unparsed should mirror the existing Persisted skip: no `persistNode` call, no `Notice.NodePersisting`, and no `Notice.PersistFailed`. The request is consumed; `loop` still runs `notePersistFinished` and takes the next stack entry.
[ ] [node-move-disk arch](plan/node-move-disk/arch.md) Persist thread write says the thread rejects the request and does not call `persistNode`. It does not require a notice on that skip. [node-move-disk arch](plan/node-move-disk/arch.md) Blocked persist posts no `Notice.PersistFailed`; that rule applies to the node enclosure lock, not this Unparsed reject.

### Existing tests

[x] `tests/Server.Tests/PersistThreadTests.fs` is the dedicated PersistThread test module. It is already listed in `tests/Server.Tests/Gambol.Server.Tests.fsproj`.
[x] `persist thread writes from the live graph` exercises `PersistThread.start`, stack take, and a real `persistNode` write. It expects `Notice.NodePersisted` only.
[x] `repeated pushes write once` shows `runRequest` skips further writes when `CoreEventDispatch.persistedNode` is true after the first success. It posts no notice on those later takes.
[x] `persist thread posts NodePersisted on success only` expects `Notice.NodePersisting` then `Notice.NodePersisted` on success, and `Notice.NodePersisting` then `Notice.PersistFailed` on `Error`.
[x] `persist thread posts the version it read` and `NodePersisted carries the written file time` cover notice shape after a write.
[x] `NodeParsed leaves the written node Persisted` wires stack push through `applyPersisted` after parse.
[x] No test names `runRequest` or `unparsedNode` directly. No test covers an Unparsed node on the persist stack.
[x] `DbAgent.startFileDiff` filtering through `unparsedNode` has no direct unit test in `PersistThreadTests.fs`. `doc/current/db-agents.md` documents the `startFileDiff` rule only.

### Copy-from

[ ] `PersistThread.runRequest` in `src/Server/Core/PersistThread.fs` already skips a Persisted node at the `if not stillPersisted` guard. That path calls neither `persistNode` nor `postNotice`. Add an Unparsed guard in the same shape before the unpersisted write branch.

### write-ticket

#### Source files

[ ] `src/Server/Core/PersistThread.fs` — `runRequest` calls `unparsedNode` on `state.graph` and `request.nodeId` after `currentState` and before the unpersisted write path (or combines guards so Unparsed never reaches `persistNode` or `Notice.NodePersisting`).

#### Tests

[ ] `tests/Server.Tests/PersistThreadTests.fs` — add one fact that pushes a persist request for a special node with `parseState = ParseState.Unparsed` and `persistState = Unpersisted`, runs `PersistThread.start` with a counting `persistNode` and a notice collector, and asserts zero writes and zero notices (`NodePersisting`, `NodePersisted`, `PersistFailed`).
[ ] Optional second fact in the same file: Unparsed with `persistState = PersistState.Persisted` still rejects with no write and no notices (guard order should not matter for the observable seam).
[ ] No new test file. No `Gambol.Server.Tests.fsproj` change.

## Inputs

### Working

#### Persist thread

[ ] `PersistThread.unparsedNode` in `src/Server/Core/PersistThread.fs` has type `Graph -> NodeId -> bool`.
[ ] `unparsedNode` is true when the node exists and `documentState.parseState` is `ParseState.Unparsed`.
[ ] `PersistThread.runRequest` is private and has type `PersistThreadDeps -> PersistRequest -> unit`.
[ ] `runRequest` reads `deps.currentState ()`.
[ ] `runRequest` calls `persistNode` only when `CoreEventDispatch.persistedNode` is false.
[ ] `CoreEventDispatch.persistedNode` is true when persist state is `PersistState.Persisted`.
[ ] When `unparsedNode` is true, `runRequest` returns without calling `persistNode`.
[ ] That return posts no `Notice.NodePersisting`, no `Notice.NodePersisted`, and no `Notice.PersistFailed`.
[ ] `PersistThread.loop` still calls `ServerRunRegistry.notePersistFinished` and takes the next request.
[ ] The Persisted skip stays a return with no notice and no `persistNode` call.

#### Copy-from

[ ] The Persisted branch in `runRequest` is the skip shape.
[ ] ``repeated pushes write once`` in `tests/Server.Tests/PersistThreadTests.fs` checks a later Persisted take posts no notice.
[ ] ``persist thread posts NodePersisted on success only`` in `tests/Server.Tests/PersistThreadTests.fs` checks `Notice.NodePersisting` and `Notice.PersistFailed` on a write that returns Error.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no public signature.
[ ] `DbAgent.startFileDiff` calls `unparsedNode` and stays as it is.
[ ] `FileAgent` and `DbAgent` keep calling `PersistThread.start`.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Persist thread write** and §4 flow 1 **Accept a persisted change**.
[ ] [71 — Persist thread takes from the stack](plan/core-refinement/issues/71-persist-thread-takes-from-the-stack.md). The Unparsed check was dropped when that ticket moved the thread onto the stack.
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/NodeLockPool.fs` stays as it is. This skip is separate from the node lock.
[ ] `src/Server/DocumentPersistWrite.fs` stays as it is.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[ ] `src/Server/Core/CoreEventDispatch.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `runRequest` in `src/Server/Core/PersistThread.fs` so an Unparsed node returns before `persistNode` and before any persist notice, as Working specifies.
3. [ ] Expand `tests/Server.Tests/PersistThreadTests.fs` with the two tests in Outputs. Reuse the `PersistThread.start` setup in ``persist thread writes from the live graph``.

## Outputs

[ ] `src/Server/Core/PersistThread.fs` — `runRequest` returns when `unparsedNode` is true.
[ ] Distinct source files: 1. `src/Server/Core/PersistThread.fs`.
[ ] `tests/Server.Tests/PersistThreadTests.fs` — the two tests below.
[ ] ``persist thread drops an unparsed request with no write and no notice`` — the node is `ParseState.Unparsed` and `PersistState.Unpersisted`. The counting `persistNode` is called zero times. The notice list has no `Notice.NodePersisting`, no `Notice.NodePersisted`, and no `Notice.PersistFailed`.
[ ] ``persist thread drops an unparsed persisted node with no write`` — the node is `ParseState.Unparsed` and `PersistState.Persisted`. `persistNode` is called zero times and no persist notice is posted.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/10-reject-an-unparsed-persist-request/10-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `unparsedNode`, `DbAgent.startFileDiff`, `writeDocument`, or `NodeLockPool`.
[ ] Stop when the edit contradicts story 1 Derived-path comparison in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``persist thread drops an unparsed request with no write and no notice``
  - `tests/Server.Tests`: ``persist thread drops an unparsed persisted node with no write``
  - `tests/Server.Tests`: ``repeated pushes write once``
  - `tests/Server.Tests`: ``persist thread posts NodePersisted on success only``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/issues/10-reject-an-unparsed-persist-request/10-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
