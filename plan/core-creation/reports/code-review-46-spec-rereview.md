# Spec axis — [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md)

Spec-axis re-review. Range: three-dot `origin/staging...origin/cursor/46-mailbox-history-durability-9b3d`. Tip `44a25eaf` Keep Graph checkpoint as tip when EventLog is empty. Ticket Status stays `coded`. Alan locks beat [46 mailbox History durability explore](46-mailbox-history-durability-explore.md) and [Core creation architecture](plan/core-creation/arch.md) EventLog / PersistHandlers wording when they conflict. Superseded: [code-review-46 mailbox History durability](code-review-46-mailbox-history-durability.md) Spec (c)1 empty EventLog → `EventId.zero`. That empty→zero door is not required.

## (a) Missing or partial

None. Persist and load of Change / ActorStart / ActorStop `Ev` records, newest-head `seedEventLog` via `adoptNewestHead`, Ops `Ev` replay when the Graph checkpoint lags, Actor bodies log-only, Undo Change-only, and same-txn left out all match [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md) What to build and Out of scope.

## (b) Scope creep

None. [EventLog.tip](src/Shared/EventLog.fs), `adoptNewestHead`, and `recoverState` are the asked helpers. File/Db create-time `recoverState` and the live `appendEvent` serial bump are the recover/serial cut. [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs) and [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) now expect replay because EventLog persist is the authority.

## (c) Looks implemented but wrong

1. Live append can drop a Graph-ahead serial. Alan lock: "Graph ahead of log by eventId → Graph keeps tip until log catches up." [46 mailbox History durability empty-log tip](46-mailbox-history-durability-empty-log-tip.md) §1.2: "Graph supplies the tip until the log catches up." Ticket [§3.1 One EventId](plan/core-creation/issues/46-mailbox-history-durability.md): "When EventLog is empty/missing or Graph is ahead by eventId, they equal the Graph checkpoint." [EventLog.recoverState](src/Shared/EventLog.fs) uses `EventId.max (tip log) state.eventId`. [FileAgent.appendEvent](src/Server/Core/FileAgent.fs) and [DbAgent.recordPersistedEvent](src/Server/Core/DbAgent.fs) set `State.eventId = event.id` with no max. After Graph-ahead recover (`eventId` 5, log tip 2), the next persist append (id 3, including ActorStart / ActorStop) sets `getEventId` to 3. The log has not caught up.

## Graph-tip verdict

Graph-tip holds on recover: empty or missing EventLog keeps the Graph checkpoint as `getEventId` / `State.eventId` (not `EventId.zero`); Graph-ahead keeps that checkpoint and does not rewrite EventLog from Graph; a non-empty log at or ahead of Graph uses the EventLog tip including ActorStop; recover replays Ops `Ev` only (`Ev.isAction`). Graph-tip does not hold after a later live append while Graph is still ahead.

Counts: (a) 0, (b) 0, (c) 1.
