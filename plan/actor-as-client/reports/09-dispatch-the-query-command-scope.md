# 09 — Start the query command — scope report

Mode: implement. Ticket: [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md). Blocker [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) is landed per [Sequence](../sequence.md).

### Signatures

[ ] `postSearchRequest` in `src/Server/RouteRegistration.fs`: `PersistenceContext -> HttpRequest -> Task<IResult>`.

[ ] `searchDoor` in `src/Server/RouteRegistration.fs`: `PersistenceContext -> Caller -> CoreChangesDoor -> Api.SearchActorDoor`.

[ ] `Api.postSearch` in `src/Server/Api.fs`: `SearchActorDoor -> string -> Async<IResult>`.

[ ] `Api.searchReply` in `src/Server/Api.fs`: `SearchActorDoor -> SearchPicture.Request -> GraphAtEvent -> Async<IResult>`.

[ ] `CoreMailboxApi.recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs`: `MailboxHost -> Caller -> ActorStart -> Async<Result<unit, string>>`.

[ ] `CoreMailboxApi.recordSearchStop` in `src/Server/Core/CoreMailboxApi.fs`: `MailboxHost -> Caller -> NodeId -> Async<Result<unit, string>>`.

[ ] `SearchActor.functionStart` in `src/Server/SearchActor.fs`: `ActorStart -> GraphCarrier -> FunctionStart` (server pool shape).

[ ] `SearchActor.isQueryRequest` in `src/Server/SearchActor.fs`: `Graph -> ActorStart -> bool`.

[ ] `SearchActor.actorStart` in `src/Server/SearchActor.fs`: `Graph -> EventId -> ActorStart`.

[ ] `SearchActor.reply` in `src/Server/SearchActor.fs`: `(unit -> Graph) -> SearchPicture.Request -> SearchPicture.Reply`.

[ ] Server `FunctionStart` in `src/Server/Core/CoreActorPool.fs`: `{ actor: ActorFunction; getGraph: GraphCarrier; actorNodeId: NodeId }` where server `ActorFunction` is `FunctionRun -> CoreChangesDoor -> Async<unit>`.

[ ] Shared `ActorOrder` in `src/Shared/History.fs`: `{ actor: ActorFunction; actorNodeId: NodeId; actorName: string }` where shared `ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>`.

[ ] `MailboxHost.postActorOrder` in `src/Server/Core/MailboxHost.fs`: `MailboxHost -> Caller -> ActorOrder -> Async<Result<EventId, string>>`.

[ ] `CoreMailboxActors.postBuiltOrder` (private) in `src/Server/Core/CoreMailboxActors.fs`: `MailboxContext -> Caller -> PostedReply -> ActorOrder -> unit`.

[ ] `CoreEventDispatch.actorStart` in `src/Server/Core/CoreEventDispatch.fs`: `Context -> Caller -> ActorStart -> Result<unit, string>`.

[ ] `CoreActorPool.startFunction` on `CoreActorPool`: `FunctionStart -> Result<Credential, string>`.

[ ] `CoreActorPool.startActor` on `CoreActorPool`: `ActorStart -> (unit -> Graph) -> Result<Credential, StartError>`.

[ ] `CoreMsg.RecordSearchStart` in `src/Server/Core/CoreMsg.fs`: `caller: Caller * request: ActorStart * AsyncReplyChannel<Result<unit, string>>`.

### Match sites

[ ] `RouteRegistration.searchDoor` `recordStart` calls `CoreMailboxApi.recordSearchStart host caller start` (`src/Server/RouteRegistration.fs`, `searchDoor`).

[ ] `RouteRegistration.postSearchRequest` calls `Api.postSearch (searchDoor persistence caller changes) body` inside `withBrowserCaller` (`src/Server/RouteRegistration.fs`, `postSearchRequest`).

[ ] `Api.searchReply` builds `SearchActor.actorStart state.graph state.eventId`, then `door.recordStart start`, then `SearchActor.reply`, then `door.recordStop start.zoomId` (`src/Server/Api.fs`, `searchReply`).

[ ] `CoreMailboxApi.recordSearchStart` is the only constructor of `RecordSearchStart`: `reply host (fun channel -> RecordSearchStart(caller, request, channel))` (`src/Server/Core/CoreMailboxApi.fs`, `recordSearchStart`).

[ ] `CoreMailboxLoop.runMsg` arm `RecordSearchStart (caller, request, reply)` calls `CoreEventDispatch.actorStart dispatch caller request` and replies (`src/Server/Core/CoreMailboxLoop.fs`, `runMsg`).

[ ] `CoreMailboxLoop.messageCaller` arm `RecordSearchStart (caller, _, _)` returns `caller` (`src/Server/Core/CoreMailboxLoop.fs`, `messageCaller`).

[ ] `CoreMailboxEvents.operationContext` arm `RecordSearchStart _` is `("RecordSearchStart", "")` (`src/Server/Core/CoreMailboxEvents.fs`, `operationContext`).

[ ] `CoreMailboxEvents.replyFailure` arm `RecordSearchStart (_, _, reply)` calls `reply.Reply(Error error)` (`src/Server/Core/CoreMailboxEvents.fs`, `replyFailure`).

[ ] `CoreEventDispatch.actorStart` commits `EventBody.ActorStart request`, which is `Order.StartActorDeprecated` on the wire (`src/Shared/History.fs`, `EventBody.ActorStart`).

[ ] `CoreEventDispatch.actorStart` has one call site: the `RecordSearchStart` arm of `CoreMailboxLoop.runMsg`.

[ ] `CoreMailboxLoop.scheduleActorOrder` builds a pool `FunctionStart` from `ActorOrder` and calls `context.pool.startFunction start` (`src/Server/Core/CoreMailboxLoop.fs`, `scheduleActorOrder`).

[ ] `CoreMailboxLoop.placeOrder` arm `Order.StartActor order` calls `replyActorOrder` (`src/Server/Core/CoreMailboxLoop.fs`, `placeOrder`).

[ ] `CoreMailboxActors.postActorStart` arms `Some "test"` and `Some "ai"` call `queueAdmittedTest` and `queueAdmittedAi`; all other names call `postChosenStart` (`src/Server/Core/CoreMailboxActors.fs`, `postActorStart`).

[ ] `CoreMailboxActors.startChosen` calls `context.pool.startActor request _getState` only (`src/Server/Core/CoreMailboxActors.fs`, `startChosen`).

[ ] `CoreMailboxActors.queueAdmittedTest` builds `ActorOrder` with `ActorOrderFromStart.orderFromStart TestActor.functionActorAt` and calls `postBuiltOrder` (`src/Server/Core/CoreMailboxActors.fs`, `queueAdmittedTest`).

[ ] `CoreMailboxActors.queueAdmittedAi` builds order via `RunAgentActor.aiActorOrder` and calls `postBuiltOrder` (`src/Server/Core/CoreMailboxActors.fs`, `queueAdmittedAi`).

[ ] `CoreMailboxActors.whenAdmitted` calls `CoreMailboxEvents.admitCaller` before the queue thunk (`src/Server/Core/CoreMailboxActors.fs`, `whenAdmitted`).

[ ] `MailboxHost.postActorOrder` wraps `Order.StartActor order` in `PlacedOrder` and posts `Mailbox.PlaceOrder` (`src/Server/Core/MailboxHost.fs`, `postActorOrder`).

### Existing tests

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``ActorStop reads the carrier graph once``: `stopFor` calls `SearchActor.functionStart` then `pool.startFunction`, then waits on `CoreMailboxApi.eventHistory` for `EventBody.ActorStop` with `ActorQuery`.

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``ActorStop ids are not the find walk``: same `stopFor` seam; compares `ActorQuery` ids to `SearchActor.reply` find walk.

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``question text is not a query request``: asserts `SearchActor.isQueryRequest` is false for `?test` text.

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``empty equals line stops with no node ids``: `stopFor` on `=` line expects `ActorQuery []`.

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``equals-line ActorStart does not run the query function``: `CoreMailboxApi.postEvents` with `EventBody.ActorStart` from `queryStart`; expects `ActorFailed` with message `unknown actor` and no `ActorQuery` stop.

[ ] `tests/Server.Tests/QueryActorTests.fs` — ``query start error stores ActorStart and ActorFailed and keeps the edit``: `postEvents` with `?gate` then query `ActorStart`; expects focus live conflict message from pool admission.

[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — ``function start does not invoke Graph carrier``: direct `pool.startFunction` without schedule.

[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — ``TestActor function start posts hello from Graph carrier``: `startFunction` + `schedule` + event history.

[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — ``TestActor function start routes ping to pong``: same pool function-start seam.

[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — ``ID-bag startActor still starts TestActor hello``: `pool.startActor` with registered `?test` actor.

[ ] `tests/Server.Tests/SearchActorTests.fs` — ``postSearch returns one reply from the server graph``: `Api.postSearch` with stub `recordStart`/`recordStop` doors.

[ ] `tests/Server.Tests/SearchActorTests.fs` — ``postSearch records ActorStart at the root``: `recordStart` calls `CoreMailboxApi.recordSearchStart`; event history expects `EventBody.ActorStart` at graph root and `ActorStop` succeeded at root.

[ ] `tests/Server.Tests/SearchActorTests.fs` — ``postSearch rejects a body without search text``: `Api.postSearch` bad body.

### Copy-from

[ ] [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md): `queueAdmittedTest` admits with `whenAdmitted` → `admitCaller`, builds `ActorOrder` via `ActorOrderFromStart.orderFromStart` and `TestActor.functionActorAt`, posts through `postBuiltOrder` → `MailboxHost.postActorOrder` (`src/Server/Core/CoreMailboxActors.fs`).

[ ] [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md): `queueAdmittedAi` uses the same `whenAdmitted`, `postBuiltOrder`, and `MailboxHost.postActorOrder` pattern with `RunAgentActor.aiActorOrder` (`src/Server/Core/CoreMailboxActors.fs`).

[ ] `ActorOrderFromStart.orderFromStart` and `orderFromPlacement` in `src/Server/ActorOrderFromStart.fs` build `ActorOrder` from a shared `ActorFunction` and node ids.

[ ] `CoreMailboxLoop.replyActorOrder` logs `EventBody.Order(Order.StartActor order)` then `scheduleActorOrder` (`src/Server/Core/CoreMailboxLoop.fs`).

### implement

[ ] Working input `postSearchRequest` is in `src/Server/RouteRegistration.fs` and calls `Api.postSearch`: matches code.

[ ] Working input `searchDoor` calls `CoreMailboxApi.recordSearchStart`: matches code.

[ ] Working input `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true: does not match code. `startChosen` in `src/Server/Core/CoreMailboxActors.fs` only calls `context.pool.startActor request _getState`. Comment on `startChosen` states `startFunction` stays unused on this path. No reference to `SearchActor.functionStart` or `isQueryRequest` in `CoreMailboxActors.fs`.

[ ] Working input `SearchActor.functionStart` builds `FunctionStart` with `getGraph` and `actorNodeId = request.focusId`: matches `src/Server/SearchActor.fs` (`functionStart`).

[ ] Working input `FunctionStart` is in `src/Server/Core/CoreActorPool.fs`: matches code.

[ ] Working input `recordSearchStart` posts `CoreMsg.RecordSearchStart` and is the only constructor: matches code.

[ ] Working input `CoreMailboxLoop.runMsg` calls `CoreEventDispatch.actorStart` for `RecordSearchStart` as the only `actorStart` caller: matches code.

[ ] Working input `messageCaller`, `operationContext`, and `replyFailure` match `RecordSearchStart`: matches code in `src/Server/Core/CoreMailboxEvents.fs` and `src/Server/Core/CoreMailboxLoop.fs`.

[ ] Working input `tests/Server.Tests/SearchActorTests.fs` calls `recordSearchStart`: matches ``postSearch records ActorStart at the root`` (`CoreMailboxApi.recordSearchStart`).

[ ] One-job target `postSearchRequest` builds a query `ActorFunction` and posts `Order.StartActor` via `postActorOrder`: not in code today. `postSearchRequest` only forwards to `Api.postSearch` through `searchDoor`; it does not call `MailboxHost.postActorOrder`.

[ ] Process target `recordSearchStart` posts `Order.StartActor` of a Find and Move `ActorFunction`: not in code today. `recordSearchStart` only enqueues `CoreMsg.RecordSearchStart`, which logs deprecated `ActorStart` and does not run the Search Actor through the pool.

[ ] After [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md), `QueryActorTests` ``equals-line ActorStart does not run the query function`` and related `postEvents` / `queryStart` facts may need a new admitted query path on `postActorStart` if client Exec query must run; Process step 2 names only `postSearchRequest`, while step 3 removes query from `startChosen` without naming a replacement for client-posted `EventBody.ActorStart` on `=` lines.

[ ] `QueryActorTests` direct `stopFor` helper: after change, query behavior should be observable through `MailboxHost.postActorOrder` or `recordSearchStart` posting `Order.StartActor`, with `CoreMailboxApi.eventHistory` showing `EventBody.Order(Order.StartActor _)` and `ActorQuery` on `ActorStop`, instead of only private `pool.startFunction` in the test helper.

[ ] `QueryActorTests` ``equals-line ActorStart does not run the query function`` and ``query start error stores ActorStart and ActorFailed and keeps the edit``: public seam is `CoreMailboxApi.postEvents`; implement should flip expectation from `unknown actor` failure to successful query start when the mailbox posts `Order.StartActor` for query lines (if in scope for this ticket).

[ ] `SearchActorTests` ``postSearch records ActorStart at the root``: public seam is `Api.postSearch` → `recordSearchStart`; implement should observe Find and Move through `Order.StartActor` and pool schedule, not deprecated `EventBody.ActorStart` from `CoreEventDispatch.actorStart`.

[ ] `CoreActorPoolFunctionStartTests`: keep observing `pool.startFunction` directly; ticket states `startFunction` stays; no file change expected except remaining green after deletions.

[ ] Delete targets `CoreMsg.RecordSearchStart`, all four match arms, and `CoreEventDispatch.actorStart`: present in code at sites listed under Match sites; implement removes them after callers move to `postActorOrder`.
