# 49 — Mailbox History durability

Date: 2026-09-18. Ticket: [49 — Mailbox History durability](../issues/49-mailbox-history-durability.md). Status `coded`. Plan: [46 mailbox History durability explore](49-mailbox-history-durability-explore.md) sequence §6. Agent: [Implement 46 Mailbox History durability](https://cursor.com/agents/bc-d2961678-a01f-5334-b735-b07ede659b3d).

This is a report, not authority.

## 1. What landed

1. Persist and load — File `SYSTEM/gambol.events` and Db `events` already wrote Ev JSON from [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md). Mailbox seed now adopts that newest-head tail. Mixed hello (ActorStart, Change, ActorStop) survives File and Db dispose/create with ActorStop as `eventHistory` head.
2. Graph catch-up — File and Db create replay Ops `Ev` ids ahead of the Graph checkpoint (`gambol.meta` / projection `revision`) through [Ev.apply](../../../src/Shared/History.fs). ActorStart / ActorStop restore into EventLog only. Recover does not call live `applyEvent` (submissionId short-circuit).
3. One serial — `appendEvent` sets `State.eventId` to the stored `Ev.id` (Actor and Action). After recover, `State.eventId` is the EventLog tip even when the last row is ActorStop. `getEventId` / HTTP `latestId` / Poll read that field.
4. Undo stays Change-only — after File restart, name-only Undo of the Change `EventId` fills inverse Ops; Undo of ActorStart keeps `no inverse Ops`.

## 2. Sequence

1. Red [Issue46MailboxHistoryDurabilityTests](../../../tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs): File+Db mixed hello dispose/create; File+Db recover with Graph/projection behind the Change; File restart Undo; live `getEventId` is ActorStop.
2. Green Shared: [EventLog.adoptNewestHead](../../../src/Shared/EventLog.fs), `tip`, `recoverState`.
3. Green mailbox: [seedEventLog](../../../src/Server/Core/CoreMailboxBackend.fs) uses `adoptNewestHead` (no second cons-fold on `restore`).
4. Green persist: [FileAgent.create](../../../src/Server/Core/FileAgent.fs) and [DbAgent.create](../../../src/Server/Core/DbAgent.fs) call `recoverState`; `appendEvent` bumps `State.eventId`.

## 3. Seams and proof

1. Server door — `CoreMailbox.eventHistory`, `getEventsSince`, `getEventId`, `getState`, `postEvent`. Not HTTP Browser restart (35b §7 already runs without 46).
2. File hello restart does not assert the Graph child. `persistGraphOps` does not write a non-artifact root child, and a successful Change still checkpoints `gambol.meta`, so recover will not replay that id. Graph catch-up is the File/Db recover facts (Change in the log, checkpoint behind).
3. Shared facts — `adoptNewestHead keeps newest-head and nextId past max`; `recoverState applies Ops ahead and sets tip past ActorStop`; empty log keeps the Graph checkpoint EventId (`createForTest` / file import). Existing `restore dedupe`, `restore keeps source nextId`, and ClientHistory Actor skip stay.
4. Old “do not replay” facts now match EventLog authority: [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs) soft-fail restart replays; [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs) cleared projection replays the `events` table.

## 4. Out of scope (held)

1. Same-transaction event-plus-Graph write.
2. Graph-ahead-of-log repair.
3. ClientHistory persist.
4. Live Actor table / Interrupted resume.
