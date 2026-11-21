# 75 — A failed write does not fail the edit

**Status:** `done`
**Type:** coding
**Blocked by:** [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
**Trackers:** architecture

## Context

Split from [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md). Today a failed document write, `PersistOutcome.Failed`, becomes an Error on the order. `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do that. Alan, 2026-10-20: a failed persist does not fail the graph edit. The edit stands. The node stays Unpersisted. The Edit submit still waits in this ticket; 69 removes the wait.

## What to build

1. [x] **Case** — Add `Notice.PersistFailed` carrying one `nodeId` and the message.
2. [x] **Post** — `PersistThread.afterWrite` posts one `Notice.PersistFailed` per node in `submit.nodeIds` through `CoreMailboxApi.postNotice` when a write finishes as `PersistOutcome.Failed`.
3. [x] **Handler** — `CoreMailboxLoad.applyNotice` does not change the persist state.
4. [x] **Order** — `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do not turn a failed write into an Error. The order reply is success. Its `message` field carries the write error.

### Acceptance Criteria

1. [x] **Test** — `Failed write keeps the edit`. After a failed write, the edit is in the graph, the order is success and its `message` carries the write error, and the node is Unpersisted.
2. [x] **Test** — `Failed write posts PersistFailed`. A poll returns `Notice.PersistFailed` with the message.
3. [x] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## Amendments

1. **Order message** — `PersistOutcome.Failed` stays the collector result. The order reply is success. `CoreChangesAccepted.message` carries the write error. The browser shows that field. Rebuilding the command result into an Event view is future work.
2. **Change batch** — `DbAgent.changeFailed` still drops the in-progress file-diff batch on a failed Change. Change fails are rare. Accepted.
3. **User docs** — [doc/user](../../../doc/user) has no page for a failed document write. Save there is git Save. No user page changed.

## Time

- 2026-10-21 2h — failed write posts PersistFailed (from chat)
- 2026-10-21 1h — one PersistFailed per node; reply message keeps the error (from chat)

## See also

- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
