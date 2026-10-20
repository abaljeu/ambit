# 69 — Persist an Edit without waiting

**Status:** `defined`
**Type:** coding
**Blocked by:** [74 — Put Notices on the event log](74-put-notices-on-the-event-log.md); [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md)
**Trackers:** architecture

## Context

An Edit submits its document write with `wait = true`. `FileAgent.collectPersist` and `DbAgent.submitLiveOps` build that submit. Both use `PersistKind.Ops`. `PersistCollectors.waitReply` then blocks the Core mailbox until the persist thread replies. That wait breaks [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

The mailbox waits for two results:

1. The stamped graph. `PersistStamp.opsBetween` turns that graph into stamp ops. `FileAgent.preparePostChange` and `DbAgent.preparePostChange` pass those ops to `CoreMailboxEvents.overlayFreshEvents`. The stored Change and the ack both carry those ops.
2. The persist message. That message becomes `CoreChangesAccepted.message`.

Split 2026-10-20: [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md) handles a failed write, and [74 — Put Notices on the event log](74-put-notices-on-the-event-log.md) puts Notices on the poll.

Alan, 2026-10-20:

1. Stamps and persist results reach the core through the Notice channel.
2. The mailbox does not wait on a document write.
3. A failed persist does not fail the graph edit. The edit stands. The node stays Unpersisted.

[71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md) posts `Notice.NodePersisted` after a successful write and does not edit the graph. The `Notice.SnapshotDone` handler in `CoreMailboxLoad.applyNotice` already sets Persisted. [46 — Record the persist version](46-record-the-persist-version.md) adds the version gate on `Notice.NodePersisted` of `nodeId * version`. Decision 5 **SnapshotDone** in [Message shape](../cqrs-messages.md) keeps that shape. That case carries no stamp ops and no persist message. This ticket does not post `Notice.NodePersisted`, does not handle it, and does not add the version gate.

This ticket is meant to come before [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md).

## What to build

An Edit document write returns before the persist thread finishes. Stamp ops and the persist message arrive on `Notice.NodeStamped`. The ack has no stamp ops and no persist message from the write. The wait and the ack stamp ops go in the same commit, so no stamp is lost or applied twice. Upload, import, parse, Load, undo, and persist stay working.

### 1. Edit write

1. [ ] **Submit** — `FileAgent.collectPersist` and `DbAgent.submitLiveOps` set `wait = false` on the Edit `PersistKind.Ops` submit. `PersistCollectors.collect` returns `PersistOutcome.Queued`. The mailbox reply does not call `PersistCollectors.waitReply`.
2. [ ] **Stamp Notice** — Add `Notice.NodeStamped`. `PersistThread.afterWrite` posts it through `CoreMailboxApi.postNotice` when that Edit write finishes with a stamped graph. The Notice carries the stamp ops from `PersistStamp.opsBetween` and the persist message. `CoreMailboxLoad.applyNotice` applies those ops and logs the Notice. This Notice is separate from `Notice.NodePersisted`.
3. [ ] **Ack** — `FileAgent.preparePostChange` and `DbAgent.preparePostChange` do not append stamp ops for that write and do not put the persist message on the ack. `CoreMailboxEvents.overlayFreshEvents` does not receive those stamp ops. The stored Change has no stamp ops from the write. `CoreChangesAccepted.message` is not that persist message.

### Acceptance Criteria

1. [ ] **No wait** — An Edit reply returns while the persist thread still holds the write. The reply is success.
2. [ ] **Stamp** — After `CoreMailboxLoad.applyNotice` applies `Notice.NodeStamped`, the graph has those stamp ops. A later poll returns that Notice.
3. [ ] **Ack** — The ack events have no stamp ops. The ack message is not the persist message.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [74 — Put Notices on the event log](74-put-notices-on-the-event-log.md)
- [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [70 — Add the persist request stack](70-add-the-persist-request-stack.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
- [GLOSSARY](../../../GLOSSARY.md)
