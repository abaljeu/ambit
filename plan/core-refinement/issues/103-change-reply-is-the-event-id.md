# 103 — Change reply is the event id

**Status:** `defined`
**Type:** coding
**Blocked by:** none
**Trackers:** architecture

## Context

`Mailbox.PlaceOrder` replies `Result<EventId, string>` for every order. `CoreMsg.PostEvent` replies `Result<Ev * CoreChangesAccepted option, string>`, and `CoreMsg.PostGraphOnly` replies `Result<CoreChangesAccepted, string>`. [64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md) moves Change onto `PlaceOrder`. This ticket makes the Change reply fit the `PlaceOrder` reply first. Behavior does not change.

`CoreChangesAccepted` is `{ eventId; events; externalChanges; message; isReady }`. Each field can come from the `EventId`:

- **eventId** — the reply.
- **events** — the stored Ev with that id, read from the log. It holds the confirmed ops, including amended ops.
- **externalChanges** — today, true when `ChangeAmendment.applyPosted` amended the posted ops. Computed by comparing the posted ops with the stored Ev's ops.
- **message** — every producer passes `None` (`FileAgent`, `DbAgent`, `CoreMailboxEvents`). The field leaves.
- **isReady** — `CoreMailboxApi.mergeReplies` already sets it from `MailboxHost.isReady`.

`CoreMailboxApi.postEvents` and `postGraphOnly` keep returning `CoreChangesAccepted` to their callers. `Api`, `CoreDoor`, `RunAgentActor`, `TestActor`, and their tests do not change.

## What to build

1. [x] **Amended ops** — Before the reply change, confirm that every case where `applyPosted` returns `amended` true gives stored ops that differ from the posted ops. If this fails, stop and report; the reply then needs an amended flag beside the id.
2. [ ] **Resubmission id** — Confirm the resubmission case: a post whose `submissionId` is already stored replies with that stored Ev's id. If this fails, stop and report; the reply then needs an amended flag beside the id. Report: [103 — Resubmission id](../reports/103-resubmission-id.md).
3. [ ] **Core reply** — `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly` reply `Result<EventId, string>`. `CoreMailboxEvents` and `CoreMailboxActors` reply the stored Ev's id.
4. [ ] **Rebuild at the API** — `CoreMailboxApi` reads the stored Ev for each replied id through `Query.EventsSince`, and builds `CoreChangesAccepted` from the id, the stored Evs, and the ops comparison. `isReady` comes from `MailboxHost.isReady`.
5. [ ] **Message leaves** — `message` leaves `CoreChangesAccepted` and `CoreChanges.accepted`. The HTTP ack writes `None` for its message field.

### Acceptance Criteria

1. [ ] **Same ack** — `postEvents returns the same CoreChangesAccepted from the event id`. For a plain Change, an amended Change, and a batch of two, `postEvents` returns the same `eventId`, `events`, and `externalChanges` as before.
2. [ ] **Resubmission** — `a resubmitted Change replies the stored id`. A second post with the same `submissionId` returns the stored Ev and `externalChanges` false.
3. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md)
- [GLOSSARY](../../../GLOSSARY.md)
