# 10 — Last 1024 events at startup (second-build scope)

Ticket: [10 — Last 1024 events at startup](../issues/10-last-1024-events-at-startup.md)
Review: [10 — Last 1024 events at startup review](../issues/10-last-1024-events-at-startup/10-review.md) on branch `cursor/review-last-1024-startup-2f93`
First build: commit `3852b35e` on `staging` (squash of PR 648)
Working branch: `cursor/stale-past-window-49ab`

Alan, 2026-11-26: a client more than 1024 events behind the server cannot continue. Tell it to set its existing Stale state (`SyncState.DataOutdated`, `amb-stale`). Do not add a new `SyncState` case. Rewire the existing hookup (`SyncLogic.getPollOutcome`, `Update` `PollDone` / `LoadDone`), which likely unwired because it previously fired at one event behind.

## Signatures

[ ] `Database.getNewestEvents (connectionString: string) (count: int) : Task<Database.EventRow list>` in `src/Server/Database.fs`
[ ] `DbAgent.startupEventWindow` — `[<Literal>]` `1024` in `src/Server/Core/DbAgent.fs`
[ ] `DbAgent.floorOfRows (rows: Database.EventRow list) : int option` — `None` when `List.length rows < startupEventWindow`, else `Some (minimum event_id)`
[ ] `DbAgent.olderThanWindow (floor: int option) (state: GraphAtEvent) : bool` — `EventId.value state.eventId < floorId` when floor is `Some`
[ ] `DbAgent.loadReconciled (loaded: LoadedPersist) : unit` — calls `Database.getNewestEvents`, `floorOfRows`, `olderThanWindow`, optional `recoverLogging`
[ ] `DbAgent.eventsSince (loaded: LoadedPersist) (after: EventId) : Ev list` — `EventLog.since after loaded.eventLog.Value` then `.events`
[ ] `DbAgent.coreMailboxAccess` exposes `getEventsSince = fun after -> Ok (eventsSince loaded after)` on `CoreMailboxAccess`
[ ] `Api.eventsThroughSnapshot (handle: CoreChangesDoor) (after: EventId) (snapshotId: EventId) : Async<Ev list>` — `CoreDoor.getEventsSince handle after`, then `List.filter (fun event -> event.id <= snapshotId)`
[ ] `Api.postPoll` and `Api.postLoad` call `eventsThroughSnapshot handle request.eventId answer.state.eventId`
[ ] `CoreDoor.getEventsSince (door: CoreChangesDoor) (after: EventId) : Async<Ev list>` — `CoreMailboxApi.getEventsSince door.host after`
[ ] `CoreMailboxLoop.runQuery` — `Query.EventsSince (after, reply)` runs `context.eventLog.Value |> EventLog.since after |> Ok` (mailbox `EventLog`, not `persist.getEventsSince`)
[ ] `CoreMailboxLoop.seedEventLog` — `match persist.getEventsSince EventId.zero` on `CoreMailboxAccess` (Db `eventsSince` path)
[ ] `EventLog.since (after: EventId) (log: EventLog) : EventLog` — `List.filter (EventId.value event.id > EventId.value after)`
[ ] `SyncLogic.getPollOutcome (poll: ChangeSuccessResponse) (clientEventId: EventId) : SyncState option` — `if poll.eventId > clientEventId then Some DataOutdated else None`
[ ] `Update.applyPollDataOutdated (events: Ev list) (syncResponse: SyncResponse) (readyModel: VM) (si: SyncInfo) : VM * Effect list` — empty pending and non-empty `events` call `SyncLogic.applySyncResponse` then `withAppliedResult` into `Idle`
[ ] `Update.update` arms `SysMsg (PollDone ...)` and `SysMsg (LoadDone ...)` dispatch `Some DataOutdated` into `applyPollDataOutdated` (Poll) or `SyncLogic.applyLoadResponse` (Load)
[ ] `LoadedPersist.windowFloor: int option ref` — set in `loadReconciled`; read in `reloadRecovered` and `olderThanWindow`; not on wire or `CoreDoor` today
[ ] `ChangeSuccessResponse` and `LoadResponse` in `src/Shared/ApiResponses.fs` — no field for “cursor older than window”; `message: string option` exists only on Poll (`ChangeSuccessResponse`)

## Match sites

[ ] `DbAgent.createLoaded` calls `loadReconciled loaded` before the mailbox starts (`src/Server/Core/DbAgent.fs`)
[ ] `loadReconciled` calls `Database.getNewestEvents connectionString startupEventWindow`, `decodeRows`, `advancePastRowIds`, `EventLog.restorePersisted`, `floorOfRows`, then branches `olderThanWindow` before `recoverLogging`
[ ] `reloadRecovered` repeats the `olderThanWindow loaded.windowFloor.Value` branch before `recoverLogging` after projection repair
[ ] `DbAgent.persist` returns `PersistFilling` whose `handlers.getEventsSince` is `eventsSince loaded` (`src/Server/Core/CoreMsg.fs` `CoreMailboxAccess`)
[ ] `CoreMailboxLoop.startWithPrelude` seeds the mailbox `EventLog` from `seedEventLog` → `persist.getEventsSince EventId.zero`
[ ] `CoreMailboxLoop.runQuery` answers live `Query.EventsSince` from `context.eventLog.Value`, not from `persist.getEventsSince`
[ ] `Api.postPoll` / `Api.postLoad` (`src/Server/Api.fs`) → `wantAnswerFromHandle` / `loadWantAnswer` → `eventsThroughSnapshot` → `CoreDoor.getEventsSince`
[ ] `App.runPollServer` / `App.runLoadServer` (`src/Client/App.fs`) decode the response, call `SyncLogic.getPollOutcome`, dispatch `PollDone` / `LoadDone` with `outcome`
[ ] `Update.update` `PollDone`: `Some DataOutdated` → `applyPollDataOutdated events syncResponse readyModel si`
[ ] `Update.update` `LoadDone`: `Some DataOutdated` with payload → `SyncLogic.applyLoadResponse` then `withAppliedResult` (no `applyPollDataOutdated`)
[ ] `StatusView.renderSyncStatus` (`src/Client/StatusView.fs`) — `DataOutdated` renders `amb-stale` and “Data changed on server — click to reload”

## Existing tests

[ ] ``startup load keeps the newest 1024 events newest-first`` — `tests/Server.Tests/DbAgentTests.fs` — `CoreMailboxApi.getEventsSince mailbox EventId.zero` returns 1024 ids `keptTipId .. -1 .. keptFloorId`, `eventHistory.nextId` past tip, graph stays `EventId.zero`
[ ] ``startup load leaves event rows older than the window in the table`` — `tests/Server.Tests/DbAgentTests.fs` — `Database.getEvents` still lists ids before the floor plus the kept window
[ ] ``startup recover applies a kept event after a checkpoint inside the window`` — `tests/Server.Tests/DbAgentTests.fs` — projection revision 1500 inside window; graph shows tip op; `getEventId` is tip
[ ] ``startup leaves a projection older than the window as loaded`` — `tests/Server.Tests/DbAgentTests.fs` — checkpoint 5 below floor 1000; graph without tip node; log still holds 1024 events; `nextId` past tip
[ ] ``startup leaves a LOAD-FALLBACK graph and still logs the fallback`` — `tests/Server.Tests/DbAgentTests.fs` — unloadable graph row; empty graph at `EventId.zero`; `LOAD-FALLBACK` in error log; `getEventsSince EventId.zero` returns 1024 events
[ ] ``getPollOutcome returns DataOutdated when server revision is ahead`` — `tests/Shared.Tests/SyncLogicTests.fs` — server event id 6, client 5 → `Some DataOutdated`
[ ] ``getPollOutcome returns None when server revision equals client`` — `tests/Shared.Tests/SyncLogicTests.fs`
[ ] ``a foreign Change during a Poll stays DataOutdated`` — `tests/Server.Tests/BrowserResidencyClientTests.fs` — `applyPoll` leaves `syncState` `DataOutdated` and client `eventId` unchanged
[ ] ``own persist notices during a Poll submit the pending queue`` — `tests/Server.Tests/BrowserResidencyClientTests.fs` — `DataOutdated` Poll with notice tail advances client and submits pending
[ ] `tests/Server.Tests/StateEndpointTests.fs` — Poll helpers `postPoll`, `decodePoll`, `installPollAnswer`; no test seeds more than 1024 table rows for Poll or Load

## Copy-from

[ ] `DbAgentTests.fillWindow`, `insertEventRows`, `keptRows`, `withAgent`, `whenReady` — `tests/Server.Tests/DbAgentTests.fs` — insert more than 1024 rows and start `DbAgent` (StateEndpoint Poll tests need the same table shape or a shared helper)
[ ] `StateEndpointTests.postPoll` / `decodePoll` — `tests/Server.Tests/StateEndpointTests.fs` — HTTP `POST /ambit/poll` and `decodeChangeSuccessResponseDecoder`
[ ] `BrowserResidencyClientTests.applyPoll` — `tests/Server.Tests/BrowserResidencyClientTests.fs` — `SyncLogic.getPollOutcome` + `Update.update` `PollDone` without HTTP
[ ] `SyncLogicTests.mkPoll` — `tests/Shared.Tests/SyncLogicTests.fs` — synthetic `ChangeSuccessResponse` for `getPollOutcome`

## implement

### Ticket claims that differ from the code (first build)

[ ] [10 — Last 1024 events at startup](../issues/10-last-1024-events-at-startup.md) Done: `Api.eventsThroughSnapshot` (Poll and Load) reports Stale when the client event id is older than the window floor — today `eventsThroughSnapshot` always returns `CoreDoor.getEventsSince` events up to the snapshot; a cursor below the floor (for example id 5 when floor is 1000) gets the full kept list from id 1001 upward with no Stale signal
[ ] Ticket Done: client surfaces Stale as `DataOutdated` — today `getPollOutcome` can return `Some DataOutdated` when `poll.eventId > clientEventId`, but `Update.applyPollDataOutdated` with a non-empty `events` list applies `SyncLogic.applySyncResponse` and lands in `Idle` with a jumped client event id across the gap (review Miss: client applies the list across the gap)
[ ] Ticket Done: Poll or Load test for a cursor older than the floor is Stale — no test in `StateEndpointTests.fs` or `SyncLogicTests.fs`
[ ] Ticket Done: Poll or Load test for a cursor inside the window catches up — no dedicated test with a truncated startup log
[ ] Review Miss Partial: a table of exactly 1024 rows sets a window floor, so a checkpoint below the oldest kept id skips `recoverOnInvalid` on a full log — `floorOfRows` sets `Some` when `List.length rows = 1024`, but every fixture inserts 1024 kept rows plus older ids 1–3 (1027 rows total); no test uses exactly 1024 rows only
[ ] Ticket Files: `windowFloor` is not visible to `Api.fs` — only `LoadedPersist.windowFloor` in `DbAgent.fs`; Poll or Load cannot compare `request.eventId` to the floor without a new pass-through on `CoreDoor` / `CoreMailboxAccess` or equivalent
[ ] Ticket Files: `ApiResponses.fs` / `ApiResponseSerialization.fs` need a wire field for Stale when the body cannot express it — no such field exists yet (distinct from a new `SyncState` case; Alan forbids a second client overlay)

### Ticket claims that match the code (first build — no second-build edit)

[ ] `loadReconciled` loads at most `startupEventWindow` rows via `Database.getNewestEvents`
[ ] `advancePastRowIds` runs on SQL rows including decode drops; `nextId` sits past the true table tip
[ ] `olderThanWindow` skips `recoverLogging` for `LOAD-FALLBACK` / checkpoint below floor (covered by DbAgent tests above)
[ ] `getEventsSince EventId.zero` succeeds with the kept window for mailbox seed
[ ] `Database.getEvents` unchanged; older table rows remain after startup

### How each named new test observes the fix at a public seam

[ ] ``Poll from a cursor older than the window floor is Stale`` (new, `tests/Server.Tests/StateEndpointTests.fs`) — seed the DB with the same shape as ``startup load keeps the newest 1024 events newest-first`` (via copied `fillWindow` helpers), start the test host (`TestBackend.createClientForDir` or equivalent), `POST /ambit/poll` with `eventId` below `keptFloorId` (for example `EventId.fromJson 5`); decode `ChangeSuccessResponse`; assert the new Stale wire field is set, `events` is empty (no gap-skipping tail), and `eventId` on the response is still the server tip — public seam: HTTP Poll body decode, not `DbAgent` internals
[ ] ``Poll from a cursor inside the window returns later kept events`` (new, `tests/Server.Tests/StateEndpointTests.fs`) — same seeded server; `POST /ambit/poll` with `eventId` at or above `keptFloorId` and below tip; assert `events` ids are a contiguous suffix within the kept window and the client could catch up without Stale — public seam: HTTP Poll response `events` list
[ ] ``Load from a cursor older than the window floor is Stale`` (new, `tests/Server.Tests/StateEndpointTests.fs`) — same seed; `POST /ambit/load` with targets and `eventId` below floor; decode `LoadResponse`; assert Stale signal and no partial event tail — public seam: HTTP Load body
[ ] ``getPollOutcome returns DataOutdated when Poll reports window Stale`` (new, `tests/Shared.Tests/SyncLogicTests.fs`) — build `ChangeSuccessResponse` with the Stale wire field set (and optionally server tip ahead of client); call `SyncLogic.getPollOutcome`; assert `Some DataOutdated` even when the stale path must not depend on applying `events` — public seam: `SyncLogic.getPollOutcome`
[ ] ``PollDone keeps DataOutdated when Poll is window Stale with a would-have-been tail`` (new, `tests/Server.Tests/BrowserResidencyClientTests.fs` or `SyncLogicTests`) — model at client id below floor; synthetic Poll with Stale set and non-empty `events` (simulating old server behavior); `Update.update` `PollDone`; assert `syncState` stays `DataOutdated`, client `eventId` unchanged, graph unchanged — public seam: `Update.update` `PollDone` / `applyPollDataOutdated` (rewire per Alan: do not apply across the gap)
[ ] ``LoadDone keeps DataOutdated when Load is window Stale`` (new, beside Poll client test) — same pattern for `LoadDone` and `SyncLogic.applyLoadResponse` — public seam: `Update.update` `LoadDone`
[ ] ``startup with exactly 1024 rows sets a floor and skips recover below the oldest id`` (new, `tests/Server.Tests/DbAgentTests.fs`) — insert exactly 1024 decodable rows (ids `1..1024`), no extra older rows; save projection at `EventId.zero` or below `1`; start agent; `CoreMailboxApi.getState` / `getEventId` shows checkpoint unchanged and `floorOfRows` behavior via empty graph at zero while `getEventsSince EventId.zero` returns 1024 events — public seam: `CoreMailboxApi.getEventId` and `getEventsSince`, not private `floorOfRows`

### Second-build edit surface (from ticket + review Miss; no source edit in this pass)

[ ] `src/Server/Api.fs` — `eventsThroughSnapshot` (or callers `postPoll` / `postLoad`): compare `request.eventId` to window floor; emit Stale on the response instead of a partial list
[ ] `src/Server/Core/DbAgent.fs` and/or `CoreDoor` — expose window floor to Api (minimal pass-through; `windowFloor` already on `LoadedPersist`)
[ ] `src/Shared/ApiResponses.fs` and `src/Shared/ApiResponseSerialization.fs` — encode/decode Stale on Poll and Load success bodies
[ ] `src/Shared/SyncLogic.fs` — `getPollOutcome` reads Stale from the response (Poll and Load via `loadResponseToPoll`)
[ ] `src/Client/Update.fs` — `PollDone` and `LoadDone`: when Stale is set, set `SyncState.DataOutdated` and do not apply a non-empty `events` tail (fix `applyPollDataOutdated` and the Load `DataOutdated` branch)
[ ] `tests/Server.Tests/StateEndpointTests.fs`, `tests/Shared.Tests/SyncLogicTests.fs`, optional `BrowserResidencyClientTests.fs`, `tests/Server.Tests/DbAgentTests.fs` — tests named above