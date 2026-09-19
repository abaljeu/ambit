# 49 — Mailbox History durability reconcile

Date: 2026-09-19. Ticket: [49 — Mailbox History durability](../issues/49-mailbox-history-durability.md). Status stays `coded`.

This is a correction note, not authority. Alan’s load reconcile supersedes empty→zero and max-tip patches ([46 mailbox History durability empty-log tip](49-mailbox-history-durability-empty-log-tip.md), [46 mailbox History durability live-append tip](49-mailbox-history-durability-live-append-tip.md), [code-review-49-mailbox-history-durability-rereview](code-review-49-mailbox-history-durability-rereview.md) Spec (c)).

## 1. Ruling

On load, compare Graph load EventId (documents / `gambol.meta` / projection) to EventLog tip:

1. Graph id > Log id — the log is invalid and is dropped. Do not keep a lagging log. Do not max tips.
2. Graph id == Log id — noop.
3. Log id > Graph id — apply events until Graph is concurrent with the log. Applying updates EventId. Do not assign a tip from Graph or from the log.

Empty or missing log with a Graph checkpoint: Graph is sole authority; empty is fine. ActorStart / ActorStop have no Ops and do not change Graph; they still participate in log tip ordering as the apply walk. Live `appendEvent` writes the minted `Ev.id`. Graph-ahead at mint time means recover failed to drop; seed of an empty log continues `nextId` past the Graph checkpoint so a later append cannot recreate that skew.

## 2. Code

[EventLog.recover](../../../src/Shared/EventLog.fs) is the three-way compare. [FileAgent](../../../src/Server/Core/FileAgent.fs) and [DbAgent](../../../src/Server/Core/DbAgent.fs) persist a drop. [CoreMailboxBackend.seedEventLog](../../../src/Server/Core/CoreMailboxBackend.fs) uses [EventLog.afterCheckpoint](../../../src/Shared/EventLog.fs) when persist events are empty.

## 3. Proof

1. [recover Graph greater than Log drops log and keeps Graph EventId](../../../tests/Shared.Tests/EventTests.fs) — log dropped; Graph id unchanged.
2. [recover equal Log and Graph is noop](../../../tests/Shared.Tests/EventTests.fs) — state and log stay.
3. [recover Log greater than Graph applies until concurrent](../../../tests/Shared.Tests/EventTests.fs) — Ops apply; EventId is the last applied `Ev`.
4. [recover moves EventId only by apply](../../../tests/Shared.Tests/EventTests.fs) — Graph-ahead does not assign; catch-up id moves with the applied Change.
5. [File Graph greater than Log drops persist EventLog](../../../tests/Server.Tests/MailboxHistoryDurabilityTests.fs) and the Db twin — persist log gone; `getEventId` is Graph.
6. [File equal Log and Graph keeps EventLog](../../../tests/Server.Tests/MailboxHistoryDurabilityTests.fs) — log stays.
7. [File empty Graph checkpoint live append continues past Graph](../../../tests/Server.Tests/MailboxHistoryDurabilityTests.fs) — next mint is past Graph; no max bandage.
