# 81 — Log Actor and search stops as Notices — scope (implement)

Scope for [81 — Log Actor and search stops as Notices](../issues/81-log-actor-and-search-stops-as-notices.md). Code state verified on branch `dev`. No source edits in this pass.

### Signatures

[ ] `Notice` in `src/Shared/History.fs` is a DU of ten cases from `NodeParsed` through `ParseFailed`; it has no `ActorStopped` or `SearchStopped`.
[ ] `EventBody` in `src/Shared/History.fs` is `ActorStop of NodeId * ActorResult | Order of Order | Notice of Notice`.
[ ] `ActorResult` in `src/Shared/History.fs` is `ActorSucceeded | ActorFailed of string | ActorCancelled | ActorQuery of NodeId list`.
[ ] `EventBody` module in `src/Shared/History.fs` exposes smart constructors and active patterns for `Change`, `Undo`, `Redo`, `Load`, `ActorStart`, and `Cancel`; it has no `ActorStop` constructor or `(|ActorStop|_|)`.
[ ] `Ev` in `src/Shared/History.fs` is `{ id; submissionId; authority; commandName; body: EventBody }`.
[ ] `Ev.ops`, `Ev.withOps`, and `Ev.target` in `src/Shared/History.fs` group `EventBody.ActorStop _` with `EventBody.Notice _` (no ops, no target).
[ ] `EventJson.encode` and `EventJson.decode` in `src/Shared/EventJson.fs` round-trip `Ev` through private `encodeBody` and `decodeBody`.
[ ] Private `EventJson.encodeNotice` in `src/Shared/EventJson.fs` has signature `Notice -> IEncodable` and matches all ten current `Notice` cases with no wildcard.
[ ] Private `EventJson.decodeNoticeBody` in `src/Shared/EventJson.fs` has signature `Decoder<EventBody>`.
[ ] Private `EventJson.encodeActorStop` and `decodeActorStopBody` in `src/Shared/EventJson.fs` use body `kind` `"actorStop"`.
[ ] `Database.writeNoticeRow` in `src/Server/Database.fs` has signature `string -> Notice -> Graph -> Task` and exhaustively matches the ten current `Notice` cases.
[ ] `CoreMailboxLoop.applyNotice` in `src/Server/Core/CoreMailboxLoop.fs` has signature `PersistHandlers -> Notice -> Result<unit, string>` and exhaustively matches the ten current `Notice` cases.
[ ] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` has signature `Context -> Caller -> NodeId -> Gambol.Server.ActorResult -> Result<unit, string>` and commits `EventBody.ActorStop(focusId, sharedResult)`.
[ ] `CoreEventDispatch.appendLifecycle` in `src/Server/Core/CoreEventDispatch.fs` has signature `Context -> Caller -> Guid -> Gambol.Shared.EventBody -> Result<Ev, string>`.
[ ] `CoreEventDispatch.completeAction` in `src/Server/Core/CoreEventDispatch.fs` delegates to `completeOther` for non-undo/redo bodies.
[ ] `ActorLive.applyEvent` in `src/Shared/ActorLive.fs` has signature `Ev -> Set<NodeId> -> Set<NodeId>`.
[ ] `ActorLive.resultOf` in `src/Shared/ActorLive.fs` has signature `Graph -> NodeId -> Ev -> CmdLastResult option`.
[ ] `SyncLogic.applyLoggedNotice` in `src/Shared/SyncLogic.fs` has signature `Ev -> ClientSyncState -> bool -> Result<ClientSyncState * bool, string>` and calls `projectLoggedNotice` only.
[ ] `SyncLogic.isNoticeEvent` in `src/Shared/SyncLogic.fs` has signature `Ev -> bool` and is true only for `EventBody.Notice _`.
[ ] `SyncLogic.loadResponseToPoll` in `src/Shared/SyncLogic.fs` has signature `LoadResponse -> ChangeSuccessResponse`.
[ ] `Api.countsAsExternal` in `src/Server/Api.fs` has signature `Ev -> bool` and returns false only for `EventBody.Notice _`.
[ ] `CoreMsg.RecordSearchStop` in `src/Server/Core/CoreMsg.fs` is `RecordSearchStop of Caller * NodeId * AsyncReplyChannel<Result<unit, string>>`.

### Match sites

[ ] Constructor `EventBody.ActorStop(focusId, result)` in `CoreEventDispatch.actorStop` (`src/Server/Core/CoreEventDispatch.fs`).
[ ] Constructor `EventBody.ActorStop(focusId, ActorCancelled)` in `CoreMailboxActors.runCancel` (`src/Server/Core/CoreMailboxActors.fs`).
[ ] Constructor `EventBody.ActorStop(request.focusId, ActorFailed message)` in `CoreMailboxActors.recordFailedStart` (`src/Server/Core/CoreMailboxActors.fs`).
[ ] Constructor `EventBody.ActorStop` from `EventJson.decodeActorStopBody` when `decodeBody` sees `kind` `"actorStop"` (`src/Shared/EventJson.fs`).
[ ] Pattern `EventBody.ActorStop _` in `MailboxLog.orderOfBody` (`src/Shared/MailboxLog.fs`).
[ ] Pattern `EventBody.ActorStop _` in `CoreMailboxActors.dispatchPostEvent` (`src/Server/Core/CoreMailboxActors.fs`).
[ ] Pattern `EventBody.ActorStop _` in `CoreMailboxApi.isClientActorStop` (`src/Server/Core/CoreMailboxApi.fs`).
[ ] Pattern `EventBody.ActorStop(id, _) when id = request.focusId` in `CoreMailboxApi.expandStored` (`src/Server/Core/CoreMailboxApi.fs`).
[ ] Pattern `EventBody.ActorStop(focusId, _)` in `ActorLive.applyEvent` (`src/Shared/ActorLive.fs`).
[ ] Pattern `EventBody.ActorStop(focusId, result)` in `ActorLive.resultOf` (`src/Shared/ActorLive.fs`).
[ ] Patterns `EventBody.ActorStop _` in `Ev.ops`, `Ev.withOps`, and `Ev.target` (`src/Shared/History.fs`).
[ ] Pattern `EventBody.ActorStop(focusId, result)` in `EventJson.encodeBody` (`src/Shared/EventJson.fs`).
[ ] Patterns `EventBody.Notice _` and `EventBody.ActorStop _` in `CoreEventDispatch.completeOther` (`src/Server/Core/CoreEventDispatch.fs`); `Notice` arm is first, then `Load`, then `ActorStart | ActorStop | Cancel`.
[ ] Pattern `EventBody.Notice _` in `SyncLogic.isNoticeEvent` (`src/Shared/SyncLogic.fs`).
[ ] Pattern `EventBody.Notice _` in `Api.countsAsExternal` (`src/Server/Api.fs`).
[ ] Pattern `EventBody.Notice notice` in `SyncLogic.projectLoggedNotice` and `SyncLogic.applyNoticeGraph` via `noticeNodeId` (`src/Shared/SyncLogic.fs`); `noticeNodeId` matches ten current `Notice` cases only.
[ ] Pattern `EventBody.Notice _` in `ActorLive.applyEvent` and `ActorLive.resultOf` (live set and cmd result unchanged).
[ ] Pattern `EventBody.Notice notice` in `Ev.apply` (`src/Shared/History.fs`).
[ ] Exhaustive `Notice` match in `EventJson.encodeNotice` (`src/Shared/EventJson.fs`).
[ ] Exhaustive `Notice` match in `Database.writeNoticeRow` (`src/Server/Database.fs`).
[ ] Exhaustive `Notice` match in `CoreMailboxLoop.applyNotice` (`src/Server/Core/CoreMailboxLoop.fs`).
[ ] Caller `CoreEventDispatch.actorStop` from `CoreMailboxLoop.runMsg` arm `RecordSearchStop` (`src/Server/Core/CoreMailboxLoop.fs`).
[ ] Caller `CoreEventDispatch.actorStop` from `CoreMailboxActors.dispatchActorStop` (`src/Server/Core/CoreMailboxActors.fs`).
[ ] Caller `CoreMailboxActors.dispatchActorStop` from `CoreMailboxLoop.runMsg` arm `CoreMsg.ActorStop` (`src/Server/Core/CoreMailboxLoop.fs`); this ticket does not remove `CoreMsg.ActorStop` ([114 — Remove CoreMsg.ActorStop](../issues/114-remove-coremsg-actorstop.md) is out of scope here).
[ ] `SyncLogic.withProjectedGraph` calls `ActorLive.applyEvent` for non-notice events on the poll path (`src/Shared/SyncLogic.fs`); `foldProjectedEvents` sends notices to `applyLoggedNotice`, which does not call `withProjectedGraph`.

### Existing tests

[ ] `ActorStop failed without message decodes as empty` in `tests/Shared.Tests/EventJsonTests.fs` decodes legacy JSON with `body.kind` `"actorStop"` and expects `EventBody.ActorStop(_, ActorFailed "")`.
[ ] `Notice JSON round trip` in `tests/Shared.Tests/EventJsonTests.fs` round-trips the ten current `Notice` cases through `EventJson.encode` / `EventJson.decode`.
[ ] `Ev JSON round-trips ActorStart and ActorStop` in `tests/Shared.Tests/EventJsonTests.fs` round-trips several `EventBody.ActorStop` variants with `EventJson.encode` / `decode`.
[ ] `An old change row is skipped on read` in `tests/Shared.Tests/EventJsonTests.fs` expects `EventJson.decode` to fail on legacy top-level `kind` `"change"` (pattern for future `actorStop` skip test).
[ ] `ActorStart adds and ActorStop removes a live Focus` in `tests/Shared.Tests/ActorLiveTests.fs` checks `ActorLive.applyEvents` adds on `EventBody.ActorStart` and removes on `EventBody.ActorStop`.
[ ] `postSearch records ActorStart at the root` in `tests/Server.Tests/SearchActorTests.fs` asserts history contains `EventBody.ActorStart` at the graph root and an `EventBody.ActorStop(root, ActorSucceeded)` after search via `CoreMailboxApi.eventHistory`.
[ ] `applyServerTail ActorStart adds and ActorStop removes a live Focus` in `tests/Shared.Tests/SyncLogicTests.fs` applies tail events through `SyncLogic.applyServerTail` and checks `actorLiveFocusIds` after `EventBody.ActorStop`.

### Copy-from

[ ] `EventBody.Cancel` and `(|Cancel|_|)` in `src/Shared/History.fs` `EventBody` module — wrapper constructor and active pattern over an `Order` case (target shape for post-change `EventBody.ActorStop` over `Notice.ActorStopped`).
[ ] `encodeActorStop` in `src/Shared/EventJson.fs` — field layout for `focusId`, `result`, optional `message`, and `ids` (ticket moves this into `encodeNotice` as `actorStopped`).
[ ] `encodeNodePersisted` in `src/Shared/EventJson.fs` — notice object with nested fields beyond `nodeId` only.
[ ] `Database.writeNoticeRow` arms for `Notice.NodeParsing`, `Notice.NodePersisting`, and similar in `src/Server/Database.fs` — `task { return () }` no-op row write (target for `ActorStopped` and `SearchStopped`).
[ ] `CoreMailboxLoop.applyNotice` arms for `Notice.NodeParsing`, `Notice.ParseFailed`, and `Notice.NodePersisting` in `src/Server/Core/CoreMailboxLoop.fs` — immediate `Ok ()` (target for stop notices).
[ ] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` — lifecycle append via `lifecycleEvent` and `commit` (today’s `RecordSearchStop` path; ticket replaces body with `Notice.SearchStopped` append).
[ ] `An old change row is skipped on read` in `tests/Shared.Tests/EventJsonTests.fs` — decode-fails-on-legacy-row pattern for `An old actorStop row is skipped on read`.

### implement

[ ] Ticket claim: `Notice` gains `ActorStopped` and `SearchStopped`; code still has only the ten lifecycle notices in `src/Shared/History.fs`.
[ ] Ticket claim: `ActorStop` leaves `EventBody`; code still has top-level `EventBody.ActorStop` in `src/Shared/History.fs`.
[ ] Ticket claim: `EventBody` module gains `ActorStop` smart constructor and `(|ActorStop|_|)` over `Notice.ActorStopped`; code has no such members in `src/Shared/History.fs`.
[ ] Ticket Look at names `CoreMailboxLoad.applyNotice`; `applyNotice` is `CoreMailboxLoop.applyNotice` in `src/Server/Core/CoreMailboxLoop.fs` and `CoreMailboxLoad.fs` has no `applyNotice`.
[ ] Ticket claim: `EventBody.ActorStop` appears in eight source files; ripgrep finds thirteen paths under `src/` (`History.fs`, `EventJson.fs`, `ActorLive.fs`, `MailboxLog.fs`, `SyncLogic.fs`, `CoreEventDispatch.fs`, `CoreMailboxActors.fs`, `CoreMailboxLoop.fs`, `CoreMailboxApi.fs`, `CoreMailboxEvents.fs`, `CoreMsg.fs`, `SearchActor.fs`, `ViewModel.fs`, `Api.fs`).
[ ] Ticket claim: `EventBody.ActorStop` appears in twenty test files; ripgrep finds twenty-four paths under `tests/`.
[ ] Ticket Size lists seven source files; additional match sites today include `CoreMailboxLoop.fs`, `CoreMailboxActors.fs`, `CoreMailboxApi.fs`, and `MailboxLog.fs` (and `expandStored` must pair start with stop after the change).
[ ] Ticket Context Today: `completeAction` has `EventBody.Notice` before `EventBody.ActorStop`; code matches in `CoreEventDispatch.completeOther` (`src/Server/Core/CoreEventDispatch.fs`).
[ ] Ticket Changes: `completeAction` puts `EventBody.ActorStop` before `EventBody.Notice`; that order is not in code today and conflicts with Context Today until `ActorStop` is removed from `EventBody`.
[ ] Ticket claim: old-form `actorStop` logs are dropped on read; code decodes them successfully (`ActorStop failed without message decodes as empty` in `EventJsonTests.fs` passes against `decodeBody` `"actorStop"`).
[ ] Ticket claim: `RecordSearchStop` appends `Notice.SearchStopped`; code calls `CoreEventDispatch.actorStop` with `ActorSucceeded` and logs `EventBody.ActorStop` in `CoreMailboxLoop.runMsg`.
[ ] Ticket claim: `applyLoggedNotice` updates `actorLiveFocusIds` via `ActorLive.applyEvent` for every Notice; code only updates graph and `eventId` in `projectLoggedNotice` (`src/Shared/SyncLogic.fs`).
[ ] Ticket claim: stop-only poll removes live focus and `loadResponseToPoll` gives `externalChanges` false; code today routes `EventBody.ActorStop` through `withProjectedGraph` (removes focus) while `loadResponseToPoll` sets `externalChanges` true when any event is not `isNoticeEvent` (ActorStop is not a Notice).
[ ] Ticket claim: `ActorLive.resultOf` treats `Notice.SearchStopped root` like `ActorStop` with `ActorSucceeded` for `root`; code returns `None` for all `EventBody.Notice _` in `resultOf`.
[ ] Ticket claim: `SyncLogic.isNoticeEvent` and `Api.countsAsExternal` stay as written; code already treats every `EventBody.Notice` as a notice and non-external — stop notices fit without arm changes once they are `Notice` cases.
[ ] Ticket claim: `UpdateActorLive.withActorCmdResult` does not change; code reads `ActorLive.lastCmdResult` over the event list (`src/Client/UpdateActorLive.fs`) and will pick up stop notices only if `resultOf` gains the SearchStopped mapping.
[ ] New test `ActorStop failed without message decodes as empty` (renamed behavior): observes `EventJson.decode` at the public decode seam; expects `Notice.ActorStopped` with empty failed message instead of top-level `EventBody.ActorStop`.
[ ] New test cases in `Notice JSON round trip`: observe `EventJson.encode` / `EventJson.decode` for `Notice.ActorStopped` and `Notice.SearchStopped`.
[ ] New test `An old actorStop row is skipped on read`: observes `EventJson.decode` fails on legacy `kind` `"actorStop"` (same public seam as `An old change row is skipped on read`).
[ ] New test `recordSearchStop logs SearchStopped` in `SearchActorTests.fs`: observes `CoreMailboxApi.eventHistory` after `CoreMailboxApi.recordSearchStop`; expects `EventBody.Notice (Notice.SearchStopped rootId)` after search start, not `EventBody.ActorStop`.
[ ] New test `SearchStopped removes the root` in `ActorLiveTests.fs`: observes `ActorLive.applyEvent` / `applyEvents` public API on an event whose body is `Notice.SearchStopped`.
[ ] New test `a stop-only poll removes the live focus and starts no catch-up` in `SyncLogicTests.fs`: observes `SyncLogic.applyServerTail` on `Notice.ActorStopped` for `actorLiveFocusIds` and `SyncLogic.loadResponseToPoll` on a `LoadResponse` whose events are only that notice for `externalChanges` false.
