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

1. [ ] **Id only** — `PersistRequest` holds `nodeId` only. Drop its `graph` field.
2. [ ] **Read** — Add `currentState: unit -> State` to `PersistThreadDeps`. `FileAgent.startPersist` and the `DbAgent` start build it as `fun () -> Volatile.Read(loaded.state).Value`.
3. [ ] **Write one node** — Add `DocumentPersistChange.persistNode dataDir graph nodeId`. It writes that node's whole document file from the given graph.
4. [ ] **Push** — Each place that submits to the collector pushes one `PersistRequest` per node instead.
5. [ ] **Take** — The persist thread `take`s a request and calls `currentState ()` once. When the node is already Persisted, it skips. Otherwise it calls `persistNode` with that state's graph. It does not edit the graph.
6. [ ] **Finish** — On success it posts `Notice.NodePersisted nodeId`. On failure it posts `Notice.PersistFailed nodeId message` and fails no edit.
7. [ ] **Remove** — Delete the collector queue, `PersistWork` and `PersistSubmit`.

### Acceptance Criteria

1. [ ] **Test** — `persist thread writes from the live graph`. An edit made after the push and before the take is in the written file.
2. [ ] **Test** — `repeated pushes write once`. Four pushes for one node, then four takes, give one write.
3. [ ] **Test** — `persist thread posts NodePersisted on success only`.
4. [ ] **Still working** — Upload, import, parse, Load, undo and persist still complete. The server starts no extra thread.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [70 — Add the persist request stack](70-add-the-persist-request-stack.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [72 — Push parsed nodes to the persist stack](72-push-parsed-nodes-to-the-persist-stack.md)
