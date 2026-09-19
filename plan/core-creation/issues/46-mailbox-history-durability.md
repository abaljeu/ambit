# 46 — Mailbox History durability

**Status:** coded
**Blocked by:** [35b — Browser Run hello](35b-browser-run-hello.md).
Actual: 5h45m

## Context

Split from [35b — Browser Run hello](35b-browser-run-hello.md) [§6 History durability](35b-browser-run-hello.md). The Browser Run hello path can prove Owned child `hello` without EventLog surviving process restart. This ticket makes the mailbox EventLog durable and authoritative: persist and load the audit sequence (Change and Actor lifecycle `Ev` records), re-execute Ops `Ev` records onto Graph when documents or projection lagged the log, and keep one EventId — `getEventId` / `State.eventId` / HTTP `latestId` / Poll. That number is the EventLog tip when the log is non-empty and at or ahead of Graph. When EventLog is empty/missing or Graph is ahead by eventId, Graph is the sole available authority and that number is the Graph checkpoint.

Follow modules named in [Core creation architecture](../arch.md) for EventLog persist and recover. Process-lifetime EventLog from [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md) / [35b — Browser Run hello](35b-browser-run-hello.md) stays until this lands. Grounded plan: [46 mailbox History durability explore](../reports/46-mailbox-history-durability-explore.md).

## What to build

### 1. Persist and load mailbox EventLog

1. [x] Persist Change and Actor Events — write Change / ActorStart / ActorStop bodies (post-SES `Ev`) so the audit sequence survives restart.
2. [x] Load EventLog on startup — restore the mailbox EventLog sequence (newest-head; not a past/future stack).
3. [x] Undo stays Change-only — Actor lifecycle Events are not Undo targets.

### 2. EventLog-authoritative Graph catch-up

1. [x] EventLog is the authority — File `SYSTEM/gambol.events` and Db `events` win when Graph or projection disagree.
2. [x] Re-execute Ops Evs — on restart/recover, apply Change/Undo/Redo `Ev` records that are in the log and not yet on Graph.
3. [x] Actor bodies stay log-only — ActorStart / ActorStop restore into EventLog; they have no Ops and do not apply to Graph.

### 3. One serial

1. [x] One EventId — after hello and after restart, `getEventId` / `State.eventId` / HTTP `latestId` / Poll equal the EventLog tip when the log is non-empty and at or ahead of Graph (not an Action-only lag). When EventLog is empty/missing or Graph is ahead by eventId, they equal the Graph checkpoint.
2. [x] Checkpoint for replay and empty/ahead tip — Graph checkpoint (`gambol.meta` / projection `revision`) chooses which Ops `Ev` ids to replay. It is also the tip when the log is empty/missing or Graph is ahead.

## Out of scope

1. Browser Run `?` path and HTTP Adapter Command door — [35b — Browser Run hello](35b-browser-run-hello.md).
2. Outside-Core lifecycle proof — [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md).
3. Reopening SES Event-only contract tickets.
4. Same-transaction event-plus-Graph write — optional later Db hardening; not this ticket (brittle for File and for lifecycle `appendEvent`). Replay-from-log is the recover road for this ticket.

## Comments

- 2026-09-18 — Alan widened: EventLog authoritative; replay Ops Evs when Graph lagged; one serial (`getEventId` / `latestId` = EventLog tip); same-txn not in this ticket. Plan: [46 mailbox History durability explore](../reports/46-mailbox-history-durability-explore.md). Status stays `defined`.
- 2026-09-18 — Explore plan: [46 mailbox History durability explore](../reports/46-mailbox-history-durability-explore.md). Persist write/load already exists from [42 — Migrate PersistHandlers restore and getEventsSince](42-migrate-persisthandlers-restore-and-geteventssince.md); implement should prove the mixed audit sequence across restart and fix mailbox seed order. Status stays `defined`.
- 2026-09-18 — Implement: seed adopt-newest-head, File/Db recover via `Ev.apply`, `appendEvent` bumps `State.eventId`. Status `coded`. Report: [46 mailbox History durability](../reports/46-mailbox-history-durability.md).
- 2026-09-18 — Split from 35b §6. Alan: §7 Browser proof can test without this ticket.
- 2026-09-19 — Alan overruled empty EventLog → `EventId.zero`. Graph is the tip when the log is empty/missing or Graph is ahead. EventLog remains authority for Ops replay when the log has those Evs. Status stays `coded`. Note: [46 mailbox History durability empty-log tip](../reports/46-mailbox-history-durability-empty-log-tip.md).

## Time

- 2026-09-18 1h — Widen plan: EventLog authority, Ops replay, one serial (from chat)
- 2026-09-18 1h30m — Explore EventLog / persist / seed; write report (from chat)
- 2026-09-18 2h — Implement seed, recover, one serial (from chat)
- 2026-09-19 45m — Empty EventLog tip is EventId.zero (from chat)
- 2026-09-19 30m — Alan: Graph tip when EventLog empty or Graph ahead (from chat)
