# Spec axis — [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md)

Independent Spec review. Status stays `coded`. Tip `ea1d1af2361347f437512f42127a9ff40c31da4a`. Diff `origin/staging...HEAD`. Authority is Alan's three-way load compare in the ticket What to build and [46 mailbox History durability reconcile](46-mailbox-history-durability-reconcile.md). Empty-log tip, max-tip, and prior code-review reports are not Spec.

## Findings

### (a) Missing or partial

No missing or partial requirement: persist/load, three-way compare, Ops `Ev.apply` catch-up, ActorStart / ActorStop log-only for Graph, Undo Change-only, one EventId, live minted `Ev.id`, empty-seed `nextId` past Graph, and same-transaction write still out of scope.

### (b) Scope creep

The extra EventLog helpers (`tip`, `adoptNewestHead`, `afterCheckpoint`) and persist drop (`EventLogFile.truncate`, `Database.clearEvents`) are the drop and empty-seed machinery the Spec names. Inverted restart tests in [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs) and [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) follow EventLog authority for Ops replay when the log is ahead. [CoreMailboxBackend.seedEventLog](src/Server/Core/CoreMailboxBackend.fs) maps `getEventId` Error to [EventLog.empty](src/Shared/EventLog.fs). Spec: "seed of an empty log continues `nextId` past the Graph checkpoint so a later append cannot recreate that skew." That Error door starts `nextId` at 1. FileAgent and DbAgent `getEventId` do not return Error.

### (c) Implemented but wrong

The three-way paths in [EventLog.recover](src/Shared/EventLog.fs) are correct: Graph id > Log id drops the log and keeps Graph EventId; equal is noop; Log id > Graph id applies `Ev` records until concurrent and moves EventId on that walk, not by a bulk tip assign.

The tip matches Alan's three-way Spec. No major correction.
