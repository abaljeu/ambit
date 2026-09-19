# 46 — Mailbox History durability live-append tip

Date: 2026-09-19. Ticket: [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md). Status stays `coded`.

This is a correction note, not authority. Alan and [code-review-46-mailbox-history-durability-rereview](code-review-46-mailbox-history-durability-rereview.md) Spec (c) require live `appendEvent` to keep a Graph-ahead tip.

## 1. Ruling

1. Empty or missing EventLog — Graph is the sole available authority. Recover already keeps that checkpoint.
2. Non-empty EventLog at or ahead of Graph — EventLog tip plus Ops replay. Recover already uses `EventId.max` of log tip and Graph checkpoint.
3. Live `appendEvent` — do not drop a Graph-ahead serial. `State.eventId` is `EventId.max` of the current Graph / `State.eventId` tip and the stored `Ev.id`. Same max as recover. Graph stays the door tip until the log catches up.

## 2. Code

[EventLog.advanceEventId](../../../src/Shared/EventLog.fs) is the shared helper. [FileAgent.appendEvent](../../../src/Server/Core/FileAgent.fs) and [DbAgent.recordPersistedEvent](../../../src/Server/Core/DbAgent.fs) call it after a successful persist write.

## 3. Proof

1. [advanceEventId Graph-ahead keeps Graph EventId](../../../tests/Shared.Tests/EventTests.fs) — Graph 5, `Ev.id` 3; tip stays 5.
2. [File Graph-ahead append keeps getEventId Graph tip](../../../tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) — `gambol.meta` 5, append ActorStart `Ev.id` 2; `getEventId` stays 5.
3. [Db Graph-ahead append keeps getEventId Graph tip](../../../tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) — projection revision 5, append ActorStart `Ev.id` 2; `getEventId` stays 5.
