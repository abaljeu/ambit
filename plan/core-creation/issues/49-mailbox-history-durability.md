# 49 — Mailbox History durability

**Status:** done
**Blocked by:** [35b — Browser Run hello](35b-browser-run-hello.md).
Actual: 7h30m

## Context

Split from [35b — Browser Run hello](35b-browser-run-hello.md) [§6 History durability](35b-browser-run-hello.md). The Browser Run hello path can prove Owned child `hello` without EventLog surviving process restart. This ticket makes the mailbox EventLog durable: persist and load the audit sequence (Change and Actor lifecycle `Ev` records), then on load reconcile Graph load EventId (meta / projection) with EventLog tip. Graph id > Log id drops the invalid log. Equal is noop. Log id > Graph id applies events until Graph is concurrent with the log. EventId moves only via apply, not by assigning a tip. Empty or missing log with a Graph checkpoint leaves Graph as sole authority (empty is fine).

Follow modules named in [Core creation architecture](../arch.md) for EventLog persist and recover. Process-lifetime EventLog from [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md) / [35b — Browser Run hello](35b-browser-run-hello.md) stays until this lands. Grounded plan: [49 mailbox History durability explore](../reports/49-mailbox-history-durability-explore.md).

## What to build

### 1. Persist and load mailbox EventLog

1. [x] Persist Change and Actor Events — write Change / ActorStart / ActorStop bodies (post-SES `Ev`) so the audit sequence survives restart.
2. [x] Load EventLog on startup — restore the mailbox EventLog sequence (newest-head; not a past/future stack).
3. [x] Undo stays Change-only — Actor lifecycle Events are not Undo targets.

### 2. EventLog-authoritative Graph catch-up

1. [x] Three-way load compare — Graph load EventId versus EventLog tip. Graph id > Log id drops the lagging log. Equal is noop. Log id > Graph id applies events until concurrent.
2. [x] Re-execute Ops Evs — when Log id > Graph id, apply Change/Undo/Redo `Ev` records via `Ev.apply` until Graph is concurrent. Applying updates EventId; do not assign a tip.
3. [x] Actor bodies stay log-only for Ops — ActorStart / ActorStop have no Ops and do not change Graph; they still participate in log tip ordering as the apply walk advances EventId.

### 3. One serial

1. [x] One EventId — `getEventId` / `State.eventId` / HTTP `latestId` / Poll read `State.eventId`. Load reconcile does not assign that field from Graph or from EventLog tip. Apply moves it. Live `appendEvent` writes the stored `Ev.id` after mailbox mint.
2. [x] Graph load EventId for compare — `gambol.meta` / projection `revision` is the Graph side of the three-way compare, not a max-tip patch and not an empty→zero door.

## Out of scope

1. Browser Run `?` path and HTTP Adapter Command door — [35b — Browser Run hello](35b-browser-run-hello.md).
2. Outside-Core lifecycle proof — [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md).
3. Reopening SES Event-only contract tickets.
4. Same-transaction event-plus-Graph write — optional later Db hardening; not this ticket (brittle for File and for lifecycle `appendEvent`). Replay-from-log is the recover road for this ticket.

## Comments

- 2026-09-18 — Independent review approve → Status `done`. Report: [[../reports/code-review-49-mailbox-history-durability-reconcile.md|code-review-49-mailbox-history-durability-reconcile]].
- 2026-09-18 — Alan widened: EventLog authoritative; replay Ops Evs when Graph lagged; one serial (`getEventId` / `latestId` = EventLog tip); same-txn not in this ticket. Plan: [46 mailbox History durability explore](../reports/49-mailbox-history-durability-explore.md). Status stays `defined`.
- 2026-09-18 — Explore plan: [46 mailbox History durability explore](../reports/49-mailbox-history-durability-explore.md). Persist write/load already exists from [42 — Migrate PersistHandlers restore and getEventsSince](42-migrate-persisthandlers-restore-and-geteventssince.md); implement should prove the mixed audit sequence across restart and fix mailbox seed order. Status stays `defined`.
- 2026-09-18 — Implement: seed adopt-newest-head, File/Db recover via `Ev.apply`, `appendEvent` bumps `State.eventId`. Status `coded`. Report: [46 mailbox History durability](../reports/49-mailbox-history-durability.md).
- 2026-09-18 — Split from 35b §6. Alan: §7 Browser proof can test without this ticket.
- 2026-09-19 — Alan overruled empty EventLog → `EventId.zero`. Graph is the tip when the log is empty/missing or Graph is ahead. EventLog remains authority for Ops replay when the log has those Evs. Status stays `coded`. Note: [46 mailbox History durability empty-log tip](../reports/49-mailbox-history-durability-empty-log-tip.md).
- 2026-09-19 — Alan + [code-review-49-mailbox-history-durability-rereview](../reports/code-review-49-mailbox-history-durability-rereview.md) Spec (c): live `appendEvent` must not drop a Graph-ahead serial. File/Db use `EventId.max` of Graph / `State.eventId` and `Ev.id`. Status stays `coded`. Note: [46 mailbox History durability live-append tip](../reports/49-mailbox-history-durability-live-append-tip.md).
- 2026-09-19 — Alan overruled max-tip and empty→zero door patches. Load reconcile is Graph id vs EventLog tip: drop lagging log, equal noop, apply until concurrent. EventId moves only via apply. Status stays `coded`. Note: [46 mailbox History durability reconcile](../reports/49-mailbox-history-durability-reconcile.md).

## Time

- 2026-09-18 1h — Widen plan: EventLog authority, Ops replay, one serial (from chat)
- 2026-09-18 1h30m — Explore EventLog / persist / seed; write report (from chat)
- 2026-09-18 2h — Implement seed, recover, one serial (from chat)
- 2026-09-19 45m — Empty EventLog tip is EventId.zero (from chat)
- 2026-09-19 30m — Alan: Graph tip when EventLog empty or Graph ahead (from chat)
- 2026-09-19 45m — Live appendEvent keeps Graph-ahead tip via EventId.max (from chat)
- 2026-09-19 1h — Load reconcile: drop / noop / apply until concurrent (from chat)
