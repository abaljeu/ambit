# Spec review: [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md)

Tree: current staging tip (not a git range). Spec: ticket What-to-build plus Story **Caller, persist, and Poll** items **10. persist ActorStart / ActorStop** and **11. PersistHandlers load/restore**. Later [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md), [44 — Migrate Browser Poll, History, pending, and EventId cursor](../issues/44-migrate-browser-poll-history-pending-and-eventid.md), [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md), and [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md) three-way drop/apply are not 42 gaps.

## (a) Missing or partial

None. Spec: "persist ActorStart / ActorStop. Persistence is this same EventLog on file/DB." [CoreEventDispatch](../../../src/Server/Core/CoreEventDispatch.fs) `actorStart` / `actorStop` call `PersistHandlers.appendEvent`. [FileAgent](../../../src/Server/Core/FileAgent.fs) writes `gambol.events`; [DbAgent](../../../src/Server/Core/DbAgent.fs) writes the `events` table. Spec: "File/Db call `EventLog.restore`. Restore merges persisted Events and dedupes by `submissionId`." Load uses `EventLog.restorePersisted` (which calls `restore`); live append uses `EventLog.restore`. Spec: "`getEventsSince` returns an Ev tail." PersistHandlers, [CoreMailbox](../../../src/Server/Core/CoreMailbox.fs), and CoreChanges return `Ev list` from `EventLog.since`. HTTP Poll already Ev-shaped is not a 42 fail.

## (b) Scope creep

None for this ticket. `EventLog.recover`, mailbox `adoptNewestHead` / `afterCheckpoint` seed, Poll consume, and ChangeLog name drop belong to later tickets and are ignored here.

## (c) Implemented but wrong

None. Spec: "Restore merges persisted Events and dedupes by `submissionId`." [EventLog.restore](../../../src/Shared/EventLog.fs) cons-folds persisted Events and skips a known `submissionId`. Lifecycle Events have no Ops and persist on `appendEvent`, not Graph `applyEvent`. Actor cases stay off the PersistHandlers parameter.

## Tests that speak to the checklist

1. [PersistHandlersRestoreTests](../../../tests/Server.Tests/PersistHandlersRestoreTests.fs) (compiled): `getEventsSince` Ev after post, File/Db restore seed, ActorStart File restart.
2. [Issue42PersistHandlersTests](../../../tests/Server.Tests/Issue42PersistHandlersTests.fs) duplicates that module and is not in the Server.Tests fsproj.
3. [EventTests](../../../tests/Shared.Tests/EventTests.fs) `restore dedupe`.

No ActorStop restart fact; persist is the same `appendEvent` path as ActorStart. Full suite not run.

**PASS**
