# 26 — Move the id-record starts onto function starts (scope)

## Signatures

[ ] `dispatchStartActor` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) is `let dispatchStartActor context caller request (reply: Result<unit, string> -> unit) : unit`.
[ ] `startChosen` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) is `let private startChosen (context: MailboxContext) (request: Gambol.Shared.ActorStart) (_getState: unit -> Graph) : Result<Credential option, StartError>`.
[ ] `postChosenStart` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) is `let private postChosenStart context caller event request reply`.
[ ] `finishReadyStart` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) is `let private finishReadyStart (context: MailboxContext) (caller: Caller) (request: Gambol.Shared.ActorStart) (secret: Credential) (make: Caller -> CoreChangesDoor) (submissionId: System.Guid)` and returns `Result<Ev, string>`.
[ ] `dispatchActorStartResult` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) is `let dispatchActorStartResult (context: MailboxContext) (caller: Caller) (request: Gambol.Shared.ActorStart) start : Result<unit, string>`.
[ ] `postActorOrder` in [src/Server/Core/MailboxHost.fs](src/Server/Core/MailboxHost.fs) is `let postActorOrder (host: MailboxHost) (caller: Caller) (order: ActorOrder) : Async<Result<EventId, string>>`.
[ ] `replyActorOrder` in [src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) is `let private replyActorOrder (context: MailboxContext) (placed: PlacedOrder) (caller: Caller) (reply: AsyncReplyChannel<Result<EventId, string>>) (order: ActorOrder) : unit`.
[ ] `TestActor.functionActorAt` in [src/Server/TestActor.fs](src/Server/TestActor.fs) is `let functionActorAt (runDefinitionId: NodeId) (focusId: NodeId) : Gambol.Shared.ActorFunction`.
[ ] `SearchActor.functionActorAt` in [src/Server/SearchActor.fs](src/Server/SearchActor.fs) is `let functionActorAt (runDefinitionId: NodeId) (_focusId: NodeId) : ActorFunction`.
[ ] `RunAgentActor.actorFn` in [src/Server/RunAgentActor.fs](src/Server/RunAgentActor.fs) is `let actorFn (install: BootActors) (sessionIdFor: Credential -> string option) (start: ActorStart) : Gambol.Shared.ActorFunction`.
[ ] `CoreActorPool.startActor` in [src/Server/Core/CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) is `startActor: Gambol.Shared.ActorStart -> (unit -> Graph) -> Result<Credential, StartError>` on type `CoreActorPool`.

## Match sites

[ ] `startChosen` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) calls `context.pool.startActor request _getState` and maps the result with `Result.map Some`.
[ ] `dispatchStartActor` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) calls `dispatchActorStartResult` with a start closure that calls `context.pool.startActor start getState |> Result.map Some`.
[ ] `postChosenStart` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) passes `startChosen` into `prepareActorStart`, then calls `finishReadyStart` or `recordFailedStart`.
[ ] `postActorStart` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) calls `postChosenStart` when `admittedQueue` returns `None`.
[ ] `dispatchPostEvent` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) matches `EventBody.ActorStart request` to `postActorStart`.
[ ] `runMsg` in [src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) matches `CoreMsg.PostEvent` to `CoreMailboxActors.dispatchPostEvent`.
[ ] `placeOrder` in [src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) matches `Order.StartActorDeprecated request` to `CoreMailboxActors.dispatchStartActor`.
[ ] `placeOrder` in [src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) matches `Order.StartActor order` to `replyActorOrder`.
[ ] `CoreMailboxApi.coreMailboxStartActor` in [src/Server/Core/CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) places `Order.StartActorDeprecated request` through `MailboxHost.postPlacedOrder`.
[ ] `dispatchStartLoadSaveCommand` in [src/Server/Core/CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) calls `CoreMailboxActors.dispatchActorStartResult` with a start closure that runs `DeskLoadPush.runLoadSave` and does not call `pool.startActor`.
[ ] `queueAdmittedTest`, `queueAdmittedQuery`, and `queueAdmittedAi` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) build an `ActorOrder` and call `postBuiltOrder`, which calls `MailboxHost.postActorOrder`.

## Existing tests

[ ] [tests/Server.Tests/CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) fact ``StartActor with live credentials calls startActor with ActorStart`` overrides `pool.startActor` and calls `CoreMailboxApi.coreMailboxStartActor`.
[ ] [tests/Server.Tests/CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) facts ``StartActor reply is startActor bookkeeping without waiting for an Actor body`` and ``live Actor stop succeeds and a later stop is dropped`` register `ActorName "root"` and call `CoreMailboxApi.coreMailboxStartActor`.
[ ] [tests/Server.Tests/CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) fact ``ActorOrder places the actor on the thread pool`` calls `MailboxHost.postActorOrder` and asserts `EventBody.Order (Order.StartActor _)`.
[ ] [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) fact ``edit then ActorStart classifies the line after the edit`` posts `EventBody.ActorStart` and asserts `Order.StartActor` with `actorName = "test"`.
[ ] [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) fact ``ActorStart body does not run inside the mailbox turn`` registers `ActorName "gate"` and posts `EventBody.ActorStart` for `?gate`.
[ ] [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) fact ``edit then unknown ActorStart stores a failed ActorStop`` posts `?nope` via `EventBody.ActorStart` and asserts `EventBody.ActorStart` plus `ActorFailed "unknown actor"`.
[ ] [tests/Server.Tests/QueryActorTests.fs](tests/Server.Tests/QueryActorTests.fs) fact ``equals-line ActorStart does not run the query function`` posts a query line via `EventBody.ActorStart` and asserts `hasStartActor "query"`.
[ ] [tests/Server.Tests/QueryActorTests.fs](tests/Server.Tests/QueryActorTests.fs) helper `stopFor` calls `pool.startFunction` with `SearchActor.functionStart`, not `pool.startActor`.
[ ] [tests/Server.Tests/CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) fact ``CoreMailboxApi.startActor calls pool.startActor and returns bookkeeping result`` is outside ticket 26 Outputs.
[ ] [tests/Server.Tests/TestActorCommandErrorTests.fs](tests/Server.Tests/TestActorCommandErrorTests.fs) theory ``unregistered Actor name fails start`` calls `CoreMailboxApi.coreMailboxStartActor` for `?unknown` and `?nope` and is outside ticket 26 Outputs.

## Copy-from

[ ] `queueAdmittedFocus` in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) builds `ActorOrder` with `ActorOrderFromStart.orderFromStart` and places it through `postBuiltOrder` and `MailboxHost.postActorOrder`.
[ ] `replyActorOrder` in [src/Server/Core/CoreMailboxLoop.fs](src/Server/Core/CoreMailboxLoop.fs) admits the caller, calls `scheduleActorOrder`, then `replyAffirmedStart` to append `EventBody.Order (Order.StartActor order)` before reply.
[ ] `RunAgentActor.aiActorOrder` in [src/Server/RunAgentActor.fs](src/Server/RunAgentActor.fs) supplies a curried `ActorFunction` through `ActorOrderFromStart.orderFromStart` and `RunAgentActor.actorFn`.

## implement

[ ] Ticket 26 — Move the id-record starts onto function starts Input claims `finishReadyStart` logs `EventJson.stubActor`; code calls `CoreEventDispatch.appendLifecycle` with `EventBody.ActorStart request`.
[ ] Ticket 26 — Move the id-record starts onto function starts Input lists `gate` and `nope` as names that remain on the `RunLaunch.queueStart` path; [27 — Reject unknown Run commands before the queue](plan/actor-as-client/issues/27-reject-unknown-run-commands-before-the-queue/27-reject-unknown-run-commands-before-the-queue-ticket.md) coded `CommandRequest.tryStart` to return `Error "unknown actor"` for every `?` name other than `test` and `ai`.
[ ] Ticket 26 — Move the id-record starts onto function starts Process step 5 requires an empty `pool.startActor` search in `src/Server`; code has two call sites in `CoreMailboxActors.startChosen` and the `dispatchStartActor` closure.
[ ] Ticket 27 — Reject unknown Run commands before the queue claims `postChosenStart` has no production caller after that ticket; code still calls `postChosenStart` from `postActorStart` when `CoreMailboxActors.dispatchPostEvent` handles a posted `EventBody.ActorStart` that is not `test`, `ai`, or a query.
[ ] Ticket 26 — Move the id-record starts onto function starts Input says `postActorStart` calls `finishReadyStart`; code routes the default arm through `postChosenStart`, which calls `finishReadyStart`.
[ ] Ticket 26 — Move the id-record starts onto function starts names no new test facts in Outputs or Check; Check runs filters on `CoreMsgActorCasesTests`, `OrderedEventStreamTests`, and `QueryActorTests` only.
[ ] `root` reaches `dispatchStartActor` through `CoreMailboxApi.coreMailboxStartActor` and `Order.StartActorDeprecated`; tests use a ROOT-shaped `ActorStart` with `runDefinitionId = Graph.rootId` and register `ActorName "root"`.
[ ] `probe` reaches `dispatchStartActor` in tests through `CoreMailboxApi.coreMailboxStartActor` after `pool.register (ActorName "probe")` in [tests/Server.Tests/AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs).
[ ] `gate` reaches `postChosenStart` when tests post `EventBody.ActorStart` for `?gate` in [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs); the Browser Run path does not queue `?gate` after ticket 27.
[ ] `load` reaches `dispatchStartActor` in [tests/Server.Tests/GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) fact ``Peer Actor is not available through the Run actor table``; `load` has no `ActorFunction` on the pool path and no test `ActorFn` registration for `load`.
[ ] `nope` reaches `dispatchStartActor` in [tests/Server.Tests/TestActorCommandErrorTests.fs](tests/Server.Tests/TestActorCommandErrorTests.fs) via `coreMailboxStartActor`, and reaches `postChosenStart` in [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) via posted `EventBody.ActorStart`.
[ ] `root`, `gate`, and `probe` have test `ActorFn` registrations only; `load` and `nope` have no server `ActorFunction`.
[ ] `test`, `ai`, and a query use `TestActor.functionActorAt`, `RunAgentActor.actorFn`, and `SearchActor.functionActorAt` on the `postActorOrder` path through `admittedQueue`.
[ ] [RouteRegistration.CreateBoot](src/Server/RouteRegistration.fs) sets boot field `Actors` to `[]`.
[ ] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and `LoadSavePath.Git`, so `dispatchStartLoadSaveCommand` does not run `finishReadyStart`.
[ ] Every `pool.startActor` caller in `src/Server` is `startChosen` in function `startChosen` and the start closure inside function `dispatchStartActor`, both in [src/Server/Core/CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs).
[ ] Output file [tests/Server.Tests/CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) asserts overridden `pool.startActor` and `CoreMailboxApi.coreMailboxStartActor`; it does not assert `StartActorDeprecated`, `stubActor`, or `EventBody.ActorStart` for the deprecated door path.
[ ] Output file [tests/Server.Tests/OrderedEventStreamTests.fs](tests/Server.Tests/OrderedEventStreamTests.fs) asserts `EventBody.ActorStart` and `Order.StartActor`; it does not assert `pool.startActor` or `StartActorDeprecated` by name.
[ ] Output file [tests/Server.Tests/QueryActorTests.fs](tests/Server.Tests/QueryActorTests.fs) asserts `EventBody.ActorStart` and `Order.StartActor` through `hasStartActor`; it does not assert `pool.startActor`, `StartActorDeprecated`, or `stubActor`.
[ ] [tests/Server.Tests/CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) would fail if `dispatchStartActor` stopped calling `pool.startActor` without replacing the `coreMailboxStartActor` door behavior; that file is outside Outputs.
[ ] [tests/Server.Tests/CoreActorPoolDeliverTests.fs](tests/Server.Tests/CoreActorPoolDeliverTests.fs) and [tests/Server.Tests/CoreActorPoolFunctionStartTests.fs](tests/Server.Tests/CoreActorPoolFunctionStartTests.fs) call `pool.startActor` or `CoreMailboxApi.coreMailboxStartActor` directly; both are outside Outputs.
[ ] [tests/Server.Tests/AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) function `startOnCommand` calls `CoreMailboxApi.coreMailboxStartActor`; that file is outside Outputs and would fail if the door path broke without test-only updates.
