# Issue 41 — Core postChange through postEvent

Date: 2026-09-15. Ticket: [[../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]. Review: [[code-review-issue-41.md]].

## Change

Core [[../../src/Server/Core/CoreMailbox.fs|CoreMailbox]].`postChange` (and `coreChanges.postChange`) builds one Event per Change at the door and posts each via `PostEvent` / `postEvent`. Accepted replies merge across the loop so multi-Change acks still list every confirmation. Graph Changes through that door append to EventLog / `eventHistory`.

`postGraphOnlyChange` uses the same Event flow as `postChange` (Module **CoreMailbox**), appending to EventLog. It passes `graphOnly=true` to `CoreEventDispatch.postEvent`, which calls `persist.postGraphOnlyChange` instead of `persist.postChange` to skip file writes.

Empty `postChange []` still reaches `persist.postChange []` via CoreMsg `PostChange` with an empty list (unchanged-submission / closed-persist rejection). Non-empty lists do not use that case.

## PostEvents removed

`PostEvents` CoreMsg and its batch reply shape were scope creep (review Spec (b)1). Removed. Spec names the seam **`postEvent` door** only; Change lists loop single `postEvent` calls.

## Out of scope kept

HTTP Adapter stay until [[../issues/43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]]. PersistHandlers restore until [[../issues/42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]]. Contract deletes until [[../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]].

## Tests

[[../../tests/Server.Tests/Issue41CoreMailboxTests.fs|Issue41CoreMailboxTests]]: `CoreMailbox.postChange` and `postGraphOnlyChange` both append Change Events to EventLog; graph-only skips file persistence only.
