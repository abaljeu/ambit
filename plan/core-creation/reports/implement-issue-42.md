# Implement 42 — Migrate PersistHandlers restore and getEventsSince

Date: 2026-09-15. Ticket: [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]]. Status `coded`. Seam map: [[issue-42-persist-seam-map.md]]. Db restore note: [[issue-42-db-restore-test.md]].

## 1. What landed

1. PersistHandlers gains `getEventsSince` and `appendEvent`. `getChangesSince` stays for HTTP Poll until [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md).
2. File: `SYSTEM/gambol.events` via [[src/Server/EventLogFile.fs]] (Event JSON beside ChangeLog).
3. Db: `events` table; append/get by `event_id`.
4. CoreEventDispatch dual-writes stored Events (Change + lifecycle) through `appendEvent`.
5. Mailbox seeds with `EventLog.restore` and bumps `nextId` past max restored id.
6. CoreMailbox `getEventsSince` door over persist.

## 2. Tests

1. [[tests/Server.Tests/Issue42PersistHandlersTests.fs]] — File getEventsSince, File restore seed, ActorStart File restore, Db restore.
2. Related modules (Issue41, CoreMailboxDoor, FileAgentFailure, DbAgentFailure, ActorCoreChangesDoor) passed focused run.

## 3. Non-goals left for later tickets

1. Api Poll/Load Event tail — [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]]
2. Browser cursor / pending — [[../issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]]
3. Drop ChangeLog name — [[../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]]
