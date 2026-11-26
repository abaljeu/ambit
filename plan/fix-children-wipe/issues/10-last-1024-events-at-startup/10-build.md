# 10 — Last 1024 events at startup build

Ticket: [10 — Last 1024 events at startup](../10-last-1024-events-at-startup.md)
Scope: [10 — Last 1024 events at startup scope](../../reports/10-last-1024-events-at-startup-scope.md)

Second build after [Review 10 — Last 1024 events at startup](https://github.com/abaljeu/ambit/pull/649). Alan: a client more than 1024 events behind the server cannot continue, and is told to set the existing Stale state `DataOutdated`.

## Window floor

`DbAgent.keptWindow` asks `Database.getNewestEvents` for `startupEventWindow + 1` rows. `startupEventWindow` is `SyncLogic.catchUpWindow` (1024).

- 1024 rows or fewer: the read is the full table. The floor stays unset. `recoverOnInvalid` still runs, including from `EventId.zero`.
- 1025 rows: the oldest row is discarded. The floor is the oldest id of the 1024 kept rows.

## Stale

`DbAgent.eventsSince` returns no events when the cursor is positive and more than one id below the floor. `EventId.zero` still returns the kept window, so mailbox seed succeeds. A cursor one id below the floor still receives the window, because that suffix does not skip a truncated row.

`CoreMailboxLoop.runQuery` answers `Query.EventsSince` from `persist.getEventsSince`, so Poll and Load see that empty list. The response `eventId` stays the server tip.

`SyncLogic.getPollOutcome` still returns `DataOutdated` when the server event id is ahead by one. That path still applies a short tail, including a pending queue. `Update.stopPastCatchUp` stops Poll, including catch-up and Uploading, when `SyncLogic.pastCatchUpWindow` is true: the client stays `DataOutdated` and does not apply the tail. Load does the same.

## Tests

- ``startup recover applies a full table of exactly 1024 events`` in `tests/Server.Tests/DbAgentTests.fs`
- ``poll and load withhold a cursor below the startup window`` in `tests/Server.Tests/StateEndpointTests.fs`
- ``a poll more than 1024 events behind stays DataOutdated`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`
- ``a load more than 1024 events behind stays DataOutdated`` in `tests/Server.Tests/BrowserResidencyClientTests.fs`

## Check

`scripts/agent-test.sh --filter` for those four tests, plus the existing startup-window tests, the one-event-behind Poll tests, and `getPollOutcome`: Server passed 10, Shared passed 8, failed 0.

Check: pass
