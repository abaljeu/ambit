# 46 — Mailbox History durability empty-log tip

Date: 2026-09-19. Ticket: [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md). Status stays `coded`.

This is a correction note, not authority. Alan overruled [code-review-46-mailbox-history-durability](code-review-46-mailbox-history-durability.md) Spec (c)1 (empty EventLog → `EventId.zero`).

## 1. Ruling

1. Empty or missing EventLog — Graph is the sole available authority. `getEventId` / `State.eventId` take the Graph checkpoint (`gambol.meta` / projection `revision`), not `EventId.zero`.
2. Graph ahead of the log by eventId — Graph supplies the tip until the log catches up. Do not invent a zero tip. Do not rewrite EventLog from Graph.
3. Non-empty EventLog at or ahead of Graph — EventLog remains authority for audit and Ops replay. `getEventId` follows [EventLog.tip](../../../src/Shared/EventLog.fs) after append/recover.

[EventLog.tip](../../../src/Shared/EventLog.fs) on `[]` is still `EventId.zero`. That is the log fact. It is not the door value when Graph has a checkpoint.

## 2. Code

[EventLog.recoverState](../../../src/Shared/EventLog.fs) on `[]` returns Graph `State` unchanged (same direction as keep-checkpoint). A non-empty log still replays Ops `Ev` ids ahead of the checkpoint, then sets `eventId` to `EventId.max` of log tip and Graph checkpoint so Graph-ahead does not drop the Graph serial.

## 3. Proof

1. [recoverState empty EventLog keeps Graph checkpoint EventId](../../../tests/Shared.Tests/EventTests.fs) — Graph kept; `eventId` stays 4.
2. [recoverState Graph-ahead of EventLog keeps Graph EventId](../../../tests/Shared.Tests/EventTests.fs) — log tip 2, Graph 5; recover keeps 5.
3. [File empty EventLog recover exposes getEventId Graph checkpoint](../../../tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) — `gambol.meta` 4, empty EventLog, `getEventId` is 4.
