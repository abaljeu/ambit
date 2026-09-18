# Fix 40-45 issue link format

Date: 2026-09-15

User correction: `[display](path)` works. `[[path|alias]]` does not. Convert aliased wikilinks in issues 40 through 45. Keep bare `[[path]]`. Do not change source. Do not commit.

## Range

Glob of `plan/core-creation/issues/4[0-5]*.md` found six files. No `40b` (or other lettered suffix) in that range.

## Path convention

No working `[display](path)` links existed in this project before this change. Sibling issues use the same-directory file name. Parent files use `../arch.md` and `../reports/event-abstraction.md`. Display text is the old alias (number and name for issues).

## Files changed

| File | Aliased links converted | Bare `[[path]]` kept |
| --- | ---: | ---: |
| [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) | 10 | 4 |
| [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) | 11 | 2 |
| [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) | 11 | 1 |
| [43 — Migrate HTTP Adapter onto postEvent and Event Poll](../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md) | 13 | 2 |
| [44 — Migrate Browser Poll, History, pending, and EventId basis](../issues/44-migrate-browser-poll-history-pending-and-eventid.md) | 10 | 1 |
| [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md) | 9 | 4 |
| Total | 64 | 14 |

After the change, those six files have no remaining `[[path|alias]]` links.

## Example

Line 34 of [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) is now:

`[43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md)`
