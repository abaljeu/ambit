# 75 — A failed write does not fail the edit

**Status:** `defined`
**Type:** coding
**Blocked by:** [74 — Put Notices on the event log](74-put-notices-on-the-event-log.md)
**Trackers:** architecture

## Context

Split from [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md). Today a failed document write, `PersistOutcome.Failed`, becomes an Error on the order. `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do that. Alan, 2026-10-20: a failed persist does not fail the graph edit. The edit stands. The node stays Unpersisted. The Edit submit still waits in this ticket; 69 removes the wait.

## What to build

1. [ ] **Case** — Add `Notice.PersistFailed` carrying the node ids of the submit and the message.
2. [ ] **Post** — `PersistThread.afterWrite` posts `Notice.PersistFailed` through `CoreMailboxApi.postNotice` when a write finishes as `PersistOutcome.Failed`.
3. [ ] **Handler** — `CoreMailboxLoad.applyNotice` leaves each of those nodes Unpersisted.
4. [ ] **Order** — `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do not turn a failed write into an Error. The order reply is success.

### Acceptance Criteria

1. [ ] **Test** — `Failed write keeps the edit`. After a failed write, the edit is in the graph, the order is success, and the node is Unpersisted.
2. [ ] **Test** — `Failed write posts PersistFailed`. A poll returns `Notice.PersistFailed` with the message.
3. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [74 — Put Notices on the event log](74-put-notices-on-the-event-log.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
