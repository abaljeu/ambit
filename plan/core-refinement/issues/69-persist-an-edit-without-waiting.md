# 69 — Persist an Edit without waiting

**Status:** `coded`
**Actual:** 1h
**Type:** coding
**Blocked by:** None
**Trackers:** architecture

## Context

An Edit submits its document write with `wait = true`. `FileAgent.collectPersist` and `DbAgent.submitLiveOps` build that submit. Both use `PersistKind.Ops`. `PersistCollectors.waitReply` then blocks the Core mailbox until the persist thread replies. That wait breaks [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

The mailbox waits for three results:

1. The stamped graph. `PersistStamp.opsBetween` turns that graph into stamp ops. `FileAgent.preparePostChange` and `DbAgent.preparePostChange` pass those ops to `CoreMailboxEvents.overlayFreshEvents`. The stored Change and the ack both carry those ops.
2. The persist message. That message becomes `CoreChangesAccepted.message`.
3. A failed write. `PersistOutcome.Failed` becomes an Error on the order. `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do that.

Alan, 2026-10-20:

1. Stamps and persist results reach the core through the Notice channel.
2. The mailbox does not wait on a document write.
3. A failed persist does not fail the graph edit. The edit stands. The node stays Unpersisted.

[71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md) posts `Notice.NodePersisted` after a successful write and does not edit the graph. [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md) handles that Notice in `CoreMailboxLoad.applyNotice`. [46 — Record the persist version](46-record-the-persist-version.md) adds the version gate on `Notice.NodePersisted` of `nodeId * version`. Decision 5 **SnapshotDone** in [Message shape](../cqrs-messages.md) keeps that shape. That case carries no stamp ops and no persist message. This ticket does not post `Notice.NodePersisted`, does not handle it, and does not add the version gate.

This ticket is meant to come before [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md).

## What to build

An Edit document write returns before the persist thread finishes. Stamp ops and the persist message arrive on a Notice. A failed write leaves the node Unpersisted. The order still succeeds. The ack has no stamp ops and no persist message from the write. Upload, import, parse, Load, undo, and persist stay working.

### 1. Edit write

1. [x] **Submit** — `FileAgent.collectPersist` and `DbAgent.submitLiveOps` set `wait = false` on the Edit `PersistKind.Ops` submit. `PersistCollectors.collect` returns `PersistOutcome.Queued`. The mailbox reply does not call `PersistCollectors.waitReply`.
2. [x] **Stamp Notice** — `PersistThread.afterWrite` posts a Notice through `CoreMailboxApi.postNotice` when that Edit write finishes with a stamped graph. The Notice carries the stamp ops from `PersistStamp.opsBetween` and the persist message. `CoreMailboxLoad.applyNotice` applies those ops and logs the Notice. A client sees that Notice through the event log and through poll. This Notice is separate from `Notice.NodePersisted`.
3. [x] **Failed write** — `PersistThread.afterWrite` posts a Notice through `CoreMailboxApi.postNotice` when that Edit write finishes as `PersistOutcome.Failed`. The Notice carries the message. `CoreMailboxLoad.applyNotice` leaves each node on that submit Unpersisted and logs the Notice. `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do not turn that failure into an Error. The order reply stays success.
4. [x] **Ack** — `FileAgent.preparePostChange` and `DbAgent.preparePostChange` do not append stamp ops for that write and do not put the persist message on the ack. `CoreMailboxEvents.overlayFreshEvents` does not receive those stamp ops. The stored Change has no stamp ops from the write. `CoreChangesAccepted.message` is not that persist message.

### Acceptance Criteria

1. [x] **No wait** — An Edit reply returns while the persist thread still holds the write. The reply is success.
2. [x] **Stamp** — After `CoreMailboxLoad.applyNotice` applies the stamp Notice, the graph has those stamp ops. A later poll returns that Notice.
3. [x] **Failure** — A failed write leaves the node Unpersisted. The log carries the message. The order is not an Error.
4. [x] **Ack** — The ack events have no stamp ops. The ack message is not the persist message.
5. [x] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Notice names** — `Notice.NodeStamped` of `NodeStamp`. Fields: `nodeIds`, `ops`, `message`, `eventId`. `Notice.PersistFailed` of `PersistFailure`. Fields: `nodeIds`, `message`.
2. **Edit role** — `PersistRole.Edit` on the submit. `PersistRole.FollowUp` is the file-diff job. The thread does not read `commandName` or `Authority`.
3. **Poll** — The queue appends that Notice as `EventBody.Notice` after apply. `Ev.ops` of that event is empty, so undo skips it. Poll returns the Notice.

## Time

- 2026-10-20 1h — edit persist without waiting (from chat)

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [70 — Add the persist request stack](70-add-the-persist-request-stack.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
- [GLOSSARY](../../../GLOSSARY.md)
