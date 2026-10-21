# 77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up

**Status:** `coded`
**Type:** coding
**Blocked by:** [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
**Trackers:** architecture

## Context

The server writes one document file per node. It does not write a snapshot. `Notice.SnapshotDone` is the persist-finished signal under the wrong name. Its handler in `CoreMailboxLoad.applyNotice` is the only core-loop writer of Persisted and runs the file-diff count hook. [Message shape](../cqrs-messages.md) decision 13 and §6 **SnapshotDone** name the target `Notice.NodePersisted`.

After [76](76-log-notices-on-the-event-log.md), each Notice is on the event log. `PersistThread` posts the persist-finished Notice before the Edit reply is built, so `Api.acceptedChangeResult` puts it in `laterEvents` and sets `externalChanges`. The browser then runs `SyncLogic.reconcileExternalAck` and a catch-up poll (rewind to the baseline, replay, re-apply pending) after every Edit. A Notice is not a Change. It must not start a rewind or replay on the browser.

## What to build

1. [x] **Rename** — `Notice.SnapshotDone of nodeId` becomes `Notice.NodePersisted of nodeId`. Code names, comments and tests change together. The encoder writes `nodePersisted`. The decoder reads `nodePersisted` and also reads `snapshotDone`, because event logs written since 76 hold that word. The version from [46 — Record the persist version](46-record-the-persist-version.md) is not added here.
2. [x] **Reply count** — In `Api.acceptedChangeResult`, Notice events in `laterEvents` do not set `externalChanges`. They still ride on `events`.
3. [x] **Browser** — A Notice in a reply or a poll does not start a catch-up, a rewind, or a replay. `SyncLogic.isConfirmationEcho` and the `SyncLogic.applyLoadResponse` race guard ignore Notice events.

### Acceptance Criteria

1. [x] **Test** — `Edit reply with only a persist Notice has no external changes`. The assertion in `StateEndpointTests` that 76 flipped to `True` is `False` again.
2. [x] **Test** — `Notice in the reply keeps the confirmation echo`. The browser takes `SyncLogic.reconcileAck`, not `reconcileExternalAck`.
3. [x] **Test** — `Stored snapshotDone decodes as NodePersisted`.
4. [x] **Still working** — Upload, import, parse, Load, undo, and persist still complete. Another user's Change still starts a catch-up.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## Amendments

1. **Poll flag** — `Api.postPoll` and `SyncLogic.loadResponseToPoll` use the same Notice rule as `Api.acceptedChangeResult`. A poll whose events are only Notices does not set `externalChanges`.
2. **Ack path** — `SyncLogic.identityError` and `SyncLogic.collectSuffixes` ignore Notice events, so `SyncLogic.reconcileAck` accepts a reply that also carries a Notice. The choice uses `EventBody.Notice`, not `commandName` or `Authority`.
3. **Poll apply** — `SyncLogic.foldProjectedEvents` advances the event id for a Notice and does not apply ops. A Notice does not rewind or replay.
4. **User docs** — [doc/user](../../../doc/user) has no page for this reply flag. No user page changed.

## Time

- 2026-10-21 2h — rename NodePersisted and stop Notice catch-up (from chat)

## See also

- [76 — Log Notices on the event log](76-log-notices-on-the-event-log.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
