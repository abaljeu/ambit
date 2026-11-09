# 10 — Reject an unparsed persist request
**Status**: defined
One job: The Server drops a persist request for an Unparsed node and writes nothing for it. That skip is the unbuilt persist-thread rule behind story 1 Derived-path comparison in [Node move and disk](plan/node-move-disk/spec.md), so an Unparsed node is not written out as a second file.

Type: bug-fixing
Blockers: none

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
[ ] [10 — Reject an unparsed persist request scope](plan/node-move-disk/reports/10-reject-an-unparsed-persist-request-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/NodeEnclosureLock.fs` stays as it is. This skip is separate from the node enclosure lock.
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

[ ] Record the stop at `plan/node-move-disk/reports/10-reject-an-unparsed-persist-request-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `unparsedNode`, `DbAgent.startFileDiff`, `writeDocument`, or `NodeEnclosureLock`.
[ ] Stop when the edit contradicts story 1 Derived-path comparison in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``persist thread drops an unparsed request with no write and no notice``
  - `tests/Server.Tests`: ``persist thread drops an unparsed persisted node with no write``
  - `tests/Server.Tests`: ``repeated pushes write once``
  - `tests/Server.Tests`: ``persist thread posts NodePersisted on success only``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/10-reject-an-unparsed-persist-request-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
