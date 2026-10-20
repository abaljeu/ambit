# 63 — Move queries to Ask

**Status:** `defined`
**Type:** coding
**Actual:** 1h15m
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg` carries `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, and `EventsSince`. `CoreMailboxLoad.runRead` answers them. `GetEventsSince` reads `persist.getEventsSince` and replies `Result<Ev list, string>`. `GetEventHistory` replies the in-memory `EventLog`. `EventsSince` replies `EventLog.since`. Decision 7 **Ask reply** in [Message shape](../cqrs-messages.md): the three event reads are one `Query.EventsSince`, and history is events since zero. Decision 3 **Ask waits**: an `Ask` stays on the mailbox until prior orders have completed. [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md) adds `Ask` and `Query`. This ticket moves the callers. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Move the five read cases to `Ask`. Each `Query` case keeps its own reply. `GetEventsSince`, `GetEventHistory`, and `EventsSince` become `Query.EventsSince`.

### 1. Change

1. [x] **Cases** — Remove `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, and `EventsSince` from `CoreMsg`. `CoreMailboxLoad` answers `Mailbox.Ask`.
2. [x] **State** — `CoreMailboxApi.tryGetState`, `getState`, and `CoreDoor.getState` post `Ask` of `Query.State`. The reply is `Result<State, string>`. The graph still includes `GraphSpan.withLockPresent` of the live focus ids.
3. [x] **LastEventId** — `CoreMailboxApi.getEventId` and `CoreDoor.getEventId` post `Ask` of `Query.LastEventId`. The reply is `Result<EventId, string>`. `getEventId` may unwrap that result at the door, as it unwraps `GetEventId` today.
4. [x] **EventsSince** — `CoreMailboxApi.getEventsSince`, `eventHistory`, and `eventsSince` post `Ask` of `Query.EventsSince`. The handler reads `EventLog.since` on the mailbox event log and replies `Result<EventLog, string>`. `eventHistory` passes `EventId.zero`. `CoreDoor.getEventsSince` and `Api` still return `Ev list` by reading the events out of that `EventLog`. `persist.getEventsSince` stays the seed reader in `CoreMailboxLoad.seedEventLog`. It is not a mailbox query.
5. [x] **Startup** — The prelude scan serves `Ask` of `State`, `LastEventId`, and `EventsSince`. It serves no other `Mailbox` case before the prelude finishes.

### Acceptance Criteria

1. [ ] **Cases** — `CoreMsg` has none of the five read cases. `CoreMailboxApi.getState` posts `Query.State`. `getEventId` posts `Query.LastEventId`.
2. [ ] **One events query** — `GetEventsSince`, `GetEventHistory`, and `EventsSince` are gone. `Query.EventsSince` of `EventId.zero` returns the full log. `Query.EventsSince` of a later id returns the tail after that id. The reply type is `Result<EventLog, string>`.
3. [ ] **Waits** — Post an order, then post `Ask` of `Query.LastEventId`. The `Ask` reply is the id assigned to that order, so the order finished first.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Status** — Status stays `defined`. Acceptance stays unchecked.
2. **Seed error** — `Query.EventsSince` replies `Error` when the EventLog seed fails. `CoreMailboxApi.getEventsSince` unwraps that result, so the door throws. The query does not reply an empty log.
3. **Docs** — Achieved read claims in [Mailbox](../../../doc/current/mailbox.md) and [Persistence model](../../../doc/current/persistence-model.md) are plain statements. The old `[o]` read lines are gone. Orders and Session stay `[o]` or `[ ]`.
4. **Names** — Ask failure labels are `State`, `LastEventId`, and `EventsSince`. `eventHistory` calls `eventsSince` of `EventId.zero`.

## Time

- 2026-10-20 45m — moved Core reads to `Mailbox.Ask`, added seam tests, and completed verification (from chat)
- 2026-10-20 30m — propagated EventLog seed failures through `Query.EventsSince` and verified the remediation (from chat)

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md)
- [GLOSSARY](../../../GLOSSARY.md)
