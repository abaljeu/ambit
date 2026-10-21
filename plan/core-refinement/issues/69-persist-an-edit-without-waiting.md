# 69 — Persist an Edit without waiting

**Status:** `defined`
**Type:** coding
**Blocked by:** [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md); [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md); [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md)
**Trackers:** architecture

## Context

An Edit submits its document write with `wait = true`. `FileAgent.collectPersist` and `DbAgent.submitLiveOps` build that submit with `PersistKind.Ops`. `PersistCollectors.waitReply` then blocks the Core mailbox until the persist thread replies. That wait breaks [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

The mailbox waits for two results:

1. The stamped graph. `PersistStamp.opsBetween` turns that graph into stamp ops. `FileAgent.preparePostChange` and `DbAgent.preparePostChange` pass those ops to `CoreMailboxEvents.overlayFreshEvents`. The stored Change and the ack both carry those ops.
2. The persist message. That message becomes `CoreChangesAccepted.message`.

Rules:

1. Stamps and persist results reach the core through Notices.
2. The mailbox does not wait on a document write.
3. A failed write does not fail the edit. `PersistThread.afterWrite` posts `Notice.PersistFailed` for each node, and the handler leaves the persist field unchanged. The browser shows the write error in `CmdLastResult`.

This ticket does not change `Notice.NodePersisted`. It comes before [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md).

## What to build

An Edit document write returns before the persist thread finishes. Stamp ops and the persist message arrive on `Notice.NodeStamped`. The ack has no stamp ops and no persist message from the write. The wait removal and the ack stamp ops go in the same commit, so no stamp is lost or applied twice. Upload, import, parse, Load, undo, and persist stay working.

### 1. Edit write

1. [ ] **Remove the wait** — Delete `PersistSubmit.wait`, `PersistCollectors.waitReply`, `PersistWork.reply`, `PersistThread.reply` and `PersistOutcome`. `PersistCollectors.collect` pushes the submit and returns `unit`. `FileAgent.collectPersist` and `DbAgent.submitLiveOps` no longer read an outcome, so `acceptWrote` and `failedKept` go too.
2. [ ] **Stamp Notice** — Add `Notice.NodeStamped`. `PersistThread.afterWrite` posts it through `CoreMailboxApi.postNotice` when that Edit write finishes with a stamped graph. The Notice carries the stamp ops from `PersistStamp.opsBetween` and the persist message. `CoreMailboxLoad.applyNotice` applies those ops and logs the Notice. This Notice is separate from `Notice.NodePersisted`.
3. [ ] **Ack** — For an Edit, `FileAgent.preparePostChange` and `DbAgent.preparePostChange` build the ack and the stored Change from the Edit's own events. `CoreChangesAccepted.message` carries the Edit's own result.
4. [ ] **Failed write** — When a poll brings `Notice.PersistFailed`, the browser's `PollDone` handler in `Update.fs` sets `lastCmdResult` to `CmdLastResult.Error (None, message)`. The Notice starts no catch-up.

### Acceptance Criteria

1. [ ] **No wait** — An Edit reply returns while the persist thread still holds the write. The reply is success. No code path waits on the persist thread.
2. [ ] **Stamp** — After `CoreMailboxLoad.applyNotice` applies `Notice.NodeStamped`, the graph has those stamp ops. A later poll returns that Notice.
3. [ ] **Ack** — The ack events are the Edit's own events, and the ack message is the Edit's own result.
4. [ ] **Failed write** — After a poll with `Notice.PersistFailed`, `lastCmdResult` holds that write error and the graph is unchanged.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
- [75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [70 — Add the persist request stack](70-add-the-persist-request-stack.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
- [GLOSSARY](../../../GLOSSARY.md)
