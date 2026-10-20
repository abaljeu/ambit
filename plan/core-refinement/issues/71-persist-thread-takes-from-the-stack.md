# 71 — Persist thread takes from the stack

**Status:** `defined`
**Type:** coding
**Blocked by:** [70 — Add the persist request stack](70-add-the-persist-request-stack.md); [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
**Trackers:** architecture

## Context

Part 2 of [51 — Stand the persist stack](51-stand-the-persist-stack.md). The persist thread in [PersistThread.fs](../../../src/Server/Core/PersistThread.fs) already exists. It takes `PersistWork` from the collector queue in [PersistCollectors.fs](../../../src/Server/Core/PersistCollectors.fs). This ticket switches that same thread to the persist request stack. It adds no thread. After [69](69-persist-an-edit-without-waiting.md), no submit waits for a reply, so every submit can become a push.

## What to build

1. [ ] **Write one node** — Add `DocumentPersistChange.persistNode dataDir graph nodeId`. It writes that node's document file from the given graph.
2. [ ] **Push** — Each place that submits to the collector pushes one `PersistRequest` per node instead, with the graph at that moment.
3. [ ] **Take** — The existing persist thread `take`s a request and calls `persistNode` with the request's graph. It does not query the mailbox and does not edit the graph.
4. [ ] **Finish** — On success it posts `Notice.SnapshotDone nodeId`. On failure it posts `Notice.PersistFailed` from [75](75-a-failed-write-does-not-fail-the-edit.md) and fails no edit.
5. [ ] **Remove** — Delete the collector queue, `PersistWork` and `PersistSubmit`.

### Acceptance Criteria

1. [ ] **Test** — `persist thread writes from the request graph`.
2. [ ] **Test** — `persist thread posts SnapshotDone on success only`.
3. [ ] **Still working** — Upload, import, parse, Load, undo and persist still complete. The server starts no extra thread.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [72 — Push parsed nodes to the persist stack](72-push-parsed-nodes-to-the-persist-stack.md)
