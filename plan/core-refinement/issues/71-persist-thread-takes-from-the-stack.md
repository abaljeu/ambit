# 71 — Persist thread takes from the stack

**Status:** `defined`
**Type:** coding
**Blocked by:** [70 — Add the persist request stack](70-add-the-persist-request-stack.md); [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md); [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md)
**Trackers:** architecture

## Context

The persist thread in [PersistThread.fs](../../../src/Server/Core/PersistThread.fs) takes `PersistWork` from the collector queue in [PersistCollectors.fs](../../../src/Server/Core/PersistCollectors.fs). Each `PersistSubmit` carries a before graph, an after graph, ops and a `PersistKind`, and the thread turns them into file edits. No submit waits for a reply.

This ticket gives that same thread a simpler input: a node id from the persist request stack. The thread reads the live state when it takes the id, so one write covers every edit made before it. Repeated pushes for the same node then find it Persisted and skip. The thread adds no thread and changes no file format.

The live state is `LoadedFile.state` in `FileAgent` and `DbAgent`. Only the mailbox thread writes it, and each Change swaps in a new immutable `State`. The agent that starts the persist thread hands it a read function built over that field. Nothing else reaches the field.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Write one node** — Add `DocumentPersistChange.persistNode dataDir graph nodeId`, which writes that node's whole document file from the given graph, with a test. Files: `src/Server/DocumentPersistChange.fs`, `tests/Server.Tests/DocumentPersistenceTests.fs`. Done when the new test writes the node's file and passes.
2. [ ] **Id and live read** — Make `PersistRequest` hold `nodeId` only, and add `currentState: unit -> State` to `PersistThreadDeps`, built in `FileAgent.startPersist` and the `DbAgent` start as `fun () -> Volatile.Read(loaded.state).Value`. Files: `src/Server/Core/PersistRequestStack.fs` (where `PersistRequest` is defined), `src/Server/Core/PersistThread.fs` (where `PersistThreadDeps` is defined), `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `tests/Server.Tests/PersistRequestStackTests.fs`. Done when it compiles and `PersistRequestStackTests` pass.
3. [ ] **Push and take** — Replace each collector submit in `FileAgent` and `DbAgent` with one `PersistRequest` push per node. The persist thread `take`s a request, calls `currentState ()` once, skips a node already Persisted, otherwise calls `persistNode` with that state's graph, then posts `Notice.NodePersisted nodeId` on success or `Notice.PersistFailed nodeId message` on failure. Delete the collector queue, `PersistWork` and `PersistSubmit`, and add the three tests below. The persist request stack registers its stop with `ServerRunRegistry.registerPersistUnblock`, the way the collector does now. Files: `src/Server/Core/PersistThread.fs`, `src/Server/Core/PersistRequestStack.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `src/Server/Gambol.Server.fsproj` (drop `Core\PersistCollectors.fs`), `tests/Server.Tests/PersistThreadTests.fs` (the three tests, and its existing `PersistSubmit` uses); delete `src/Server/Core/PersistCollectors.fs`. Done when the three tests pass, `ServerTestReset` still stops the persist thread between tests, and no file under `src/` or `tests/` names `PersistSubmit` or `PersistWork`.

### Acceptance Criteria

1. [ ] **Test** — `persist thread writes from the live graph`. An edit made after the push and before the take is in the written file.
2. [ ] **Test** — `repeated pushes write once`. Four pushes for one node, then four takes, give one write.
3. [ ] **Test** — `persist thread posts NodePersisted on success only`.
4. [ ] **Still working** — Upload, import, parse, Load, undo and persist still complete. The server starts no extra thread.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## See also

- [70 — Add the persist request stack](70-add-the-persist-request-stack.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [72 — Push parsed nodes to the persist stack](72-push-parsed-nodes-to-the-persist-stack.md)
