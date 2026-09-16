# Ev rename — arch and dependents

Date: 2026-09-16. Docs only. No commit.

Landed code: Event record and helpers are `Ev` in `Gambol.Shared` ([[src/Shared/Event.fs]]). `EventId`, `EventBody`, `EventLog`, `EventJson`, `Authority`, `ActorStart`, and `ActorResult` live in `Gambol.Shared`. There is no `Gambol.Shared.Events` namespace. Change stays the command-builder product; `Ev.ofChange` / `Ev.asChange` are the bridge. Change-list tails are Ev lists.

Reports under [[plan/core-creation/reports/]] are not dependents for this rename. Historical reports stay as written.

## 1. Files kept (non-report)

1. [[plan/core-creation/arch.md]] — Event type/module → Ev; namespace `Gambol.Shared`; EventJson path [[src/Shared/EventJson.fs]]; Change list tails → Ev list; story titles stay Event.
2. [[plan/core-creation/project.md]] — `Updated:` 2026-09-16; log line for the rename; 2026-09-15 issue-37 line no longer presents `Gambol.Shared.Events` as current. Stage stays `build`.
3. [[plan/core-creation/issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] — Shared types are Ev in `Gambol.Shared`; `Ev.apply`; comment that Events namespace is gone.
4. [[plan/core-creation/issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] — `postEvent` payload is Ev; EventBatch in later hops.
5. [[plan/core-creation/issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] — Ev built at the door; stored Ev; EventBatch.
6. [[plan/core-creation/issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] — `getEventsSince` returns an Ev list / Ev tail; EventBatch.
7. [[plan/core-creation/issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]] — Poll/Load tails are Ev lists, not Change lists.
8. [[plan/core-creation/issues/44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]] — PendingChange / EventBatch wrap Ev.
9. [[plan/core-creation/issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]] — payload is Ev; modules Ev and EventLog.

## 2. Report edits reverted

These were edited in the first pass and restored to HEAD so historical reports stay unchanged:

1. [[event-abstraction.md]]
2. [[implement-issue-37.md]]
3. [[implement-issue-40.md]]
4. [[review-fix-eventid-clienthistory.md]]
5. [[issue-41-seam-audit.md]]
6. [[corechangesaccepted-events-only.md]]

This file is the only new report for the rename.

## 3. Not in scope

1. Other reports under [[plan/core-creation/reports/]] — left as history.
2. Code, tests, commit.
