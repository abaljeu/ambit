# 69 — Persist an Edit without waiting

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

An Edit Change submits its document write on the Core mailbox. `FileAgent.collectPersist` and `DbAgent.submitLiveOps` set `wait = true`. `PersistCollectors.waitReply` blocks that mailbox until the persist thread replies. The mailbox waits for three results.

- The stamped graph. `PersistStamp.opsBetween` turns it into stamp ops. `CoreMailboxEvents.overlayFreshEvents` appends those ops to the stored Change. The reply echoes them.
- The persist message. `PersistGraphOk.message` becomes `CoreChangesAccepted.message`.
- Failure. `PersistOutcome.Failed` becomes an Error in `FileAgent.fromOutcome` and `DbAgent.liveOutcome`. The graph edit does not stand.

That wait breaks [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

Alan, 2026-10-20:

- Stamps and persist results reach the core through the Notice channel.
- The mailbox does not wait on a document write.
- A failed persist leaves the graph edit in place. The node stays Unpersisted.

`Notice.NodePersisted` of `nodeId * version` is the success finish. Decision 5 **SnapshotDone** in [Message shape](../cqrs-messages.md) and [46 — Record the persist version](46-record-the-persist-version.md) own that pair and the version gate. [51 — Stand the persist stack](51-stand-the-persist-stack.md) owns the persist-stack post of that Notice. A failed write does not post `NodePersisted`. This ticket does not rebuild those parts. The pair stays `nodeId * version`. This ticket adds `stampOps` on that Notice. A persist message is `Notice.PersistFailed`.

This ticket comes before [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md). The mailbox reply then has no stamp ops and no persist message. 64's reply is the assigned `EventId`.

## What to build

`FileAgent` and `DbAgent` submit the Edit write with `wait = false`. `PersistThread` posts the finish through the notice function. `CoreMailboxLoad.applyNotice` applies the stamp. The order succeeds. Upload, import, parse, Load, undo, and persist still complete.

### 1. Submit

1. [ ] **Wait** — `FileAgent.collectPersist` and `DbAgent.submitLiveOps` set `wait = false` on the Edit Ops write. `PersistCollectors.waitReply` does not run for that write. The mailbox reply returns while the persist thread holds the job. `DbAgent.enqueueFileDiff` and `FileAgent.scheduleCatchUp` stay `wait = false`.
2. [ ] **Order** — The graph edit stands. `FileAgent.fromOutcome` and `DbAgent.liveOutcome` do not turn `PersistOutcome.Failed` into an Error on that order. The order path stores the edit graph. It does not store the stamped graph. The node stays `PersistState.Unpersisted`.
3. [ ] **Ack** — `FileAgent.preparePostChange` and `DbAgent.preparePostChange` do not call `PersistStamp.opsBetween` for this write. `CoreMailboxEvents.overlayFreshEvents` does not append those stamp ops to the stored Change. `CoreChangesAccepted.message` does not carry a persist message from this write.

### 2. Notice

1. [ ] **Finish** — `PersistThread` posts one Notice through `CoreMailboxApi.postNotice` when the write finishes. The stamp effect is the `SetUpdateTime` ops from `PersistStamp.opsBetween` of the submitted graph and the stamped graph. `CoreMailboxLoad.applyNotice` applies that stamp and logs the Notice. Clients see the stamp and any persist message through the ordinary event and poll path.
2. [ ] **Success** — A `Wrote` result whose message is None posts `Notice.NodePersisted` of `nodeId: NodeId * version: EventId * stampOps: Op list`. [46 — Record the persist version](46-record-the-persist-version.md) and decision 5 **SnapshotDone** own the `nodeId * version` pair and the version gate. `stampOps` is the field this ticket adds. This ticket does not add the version gate.
3. [ ] **Failure** — A `Failed` result, or a `Wrote` result whose message is Some, posts `Notice.PersistFailed` of `nodeId: NodeId * message: string * stampOps: Op list`. `stampOps` is empty when the writer returned no graph. The case is not one of the seven Node cases in decision 12 **NodeUnpersisted**. `CoreMailboxLoad.applyNotice` applies `stampOps`, leaves the node `PersistState.Unpersisted`, and the logged Notice reports the message. The order already succeeded. [51 — Stand the persist stack](51-stand-the-persist-stack.md) still does not post `NodePersisted` for that failure.

### Acceptance Criteria

1. [ ] **No wait** — An Edit through `FileAgent.processPostEvents` or `DbAgent.processPostEvents` returns while the persist thread still holds the job. `PersistCollectors.waitReply` does not run for that job.
2. [ ] **Ack** — The mailbox reply has no stamp ops from the write and no persist `message` from the write. The stored Change has no stamp ops from the write.
3. [ ] **Stamp** — A clean write logs `Notice.NodePersisted` of that node and version. The core graph has the stamp. A later poll returns that Notice.
4. [ ] **Failure** — A failed write leaves the graph edit in place, leaves the node Unpersisted, and logs `Notice.PersistFailed` with the message. The order result is success.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete. Parse keeps the graph-only skip in `FileAgent.persistPostChange` and `DbAgent.persistLiveChange`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
- [GLOSSARY](../../../GLOSSARY.md)
