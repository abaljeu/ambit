# 117 — The pool posts Notice.ActorStopped when an Actor ends — scope report

Tree: `staging` at `4273ef22` (113 — Actors return their ActorResult). Mode: implement.

### Signatures

[ ] `Gambol.Shared.ActorResult` in `src/Shared/History.fs` is `ActorSucceeded | ActorFailed of string | ActorCancelled | ActorQuery of ids: NodeId list`.

[ ] `Gambol.Shared.ActorFunction` in `src/Shared/History.fs` is `FunctionRun -> ActorChanges -> Async<ActorResult>`.

[ ] `Gambol.Server.ActorFunction` in `src/Server/Core/CoreActorPool.fs` is `FunctionRun -> CoreChangesDoor -> Async<ActorResult>`.

[ ] `Gambol.Server.FunctionStart` in `src/Server/Core/CoreActorPool.fs` is `{ actor: ActorFunction; getGraph: GraphCarrier; actorNodeId: NodeId }`.

[ ] `Gambol.Shared.Notice.ActorStopped` in `src/Shared/History.fs` is `ActorStopped of focusId: NodeId * result: ActorResult`.

[ ] `Gambol.Shared.EventBody.ActorStop` in `src/Shared/History.fs` is an active pattern and constructor that wraps `EventBody.Notice(Notice.ActorStopped(focusId, result))`.

[ ] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` is `takePendingBody: Credential -> (PendingBody * LiveRow) option -> secret: Credential -> coreChanges: CoreChangesDoor -> unit`.

[ ] `CoreActorPool.trySecretForActorNode` in `src/Server/Core/CoreActorPool.fs` on the pool record is `NodeId -> Credential option`; the private implementation scans `live` rows by `row.actorNodeId`.

[ ] `CoreActorPool.finish` on the pool record is `Credential -> ActorResult -> Result<unit, string>`; private `runFinish` cancels the row token via `runDrop` for every `ActorResult` case.

[ ] `CoreMailboxApi.postNotice` in `src/Server/Core/CoreMailboxApi.fs` is `internal`; signature `MailboxHost -> Notice -> unit`; it enqueues `CoreQueue.Notice` through `MailboxHost.postItem`.

[ ] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` is `MailboxContext -> CoreMsg -> unit`.

[ ] `CoreMailboxLoop.dispatchItem` in `src/Server/Core/CoreMailboxLoop.fs` is `MailboxContext -> CoreQueue -> unit`.

[ ] `CoreMailboxLoop.applyNotice` in `src/Server/Core/CoreMailboxLoop.fs` is `CoreMailboxAccess -> Notice -> Result<unit, string>`; `Notice.ActorStopped _` returns `Ok ()`.

[ ] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` is `Context -> Caller -> focusId: NodeId -> ActorResult -> Result<unit, string>`.

[ ] `CoreEventDispatch.logNotice` in `src/Server/Core/CoreEventDispatch.fs` is `Context -> Notice -> Result<Ev, string>`.

[ ] `CoreDoor.actorCaller` in `src/Server/Core/CoreDoor.fs` is `Credential -> Caller` with `Authority "Actor"`.

[ ] `CoreDoor.actorStop` in `src/Server/Core/CoreDoor.fs` is `CoreChangesDoor -> ActorResult -> Async<Result<unit, string>>`.

### Match sites

[ ] `GithubTransportActor.finishActor` in `src/Server/GithubTransportActor.fs` calls `changes.actorStop result` after work completes.

[ ] `RunAgentActor.stop` in `src/Server/RunAgentActor.fs` calls `changes.actorStop result`.

[ ] `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` calls `stop changes` on the not-live path and after `runBody`.

[ ] `SearchActor.queryActorBody` in `src/Server/SearchActor.fs` calls the supplied `stop` and returns that `ActorResult`.

[ ] `SearchActor.functionStart` in `src/Server/SearchActor.fs` passes `CoreDoor.actorStop door` into `queryActorBody`.

[ ] `SearchActor.functionActorAt` in `src/Server/SearchActor.fs` passes `changes.actorStop` into `queryActorBody`.

[ ] `SearchActor.searchActorAt` in `src/Server/SearchActor.fs` calls `changes.actorStop ActorSucceeded` and returns `ActorSucceeded`.

[ ] `TestActor.dispatch` in `src/Server/TestActor.fs` calls `CoreDoor.actorStop` after behavior work.

[ ] `TestActor.runBehavior` in `src/Server/TestActor.fs` calls `changes.actorStop result`.

[ ] `CoreMailboxActors.dispatchActorStop` in `src/Server/Core/CoreMailboxActors.fs` matches `Authority "Actor"`, appends via `CoreEventDispatch.actorStop` using `pool.getActorNodeId caller.secret`, then calls `pool.finish`.

[ ] `CoreMailboxActors.runCancel` in `src/Server/Core/CoreMailboxActors.fs` on a live secret appends `EventBody.ActorStop(focusId, ActorCancelled)` through `CoreEventDispatch.appendLifecycle`, then `pool.finish secret ActorCancelled`; on no secret it returns `Ok None`.

[ ] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` matches `CoreMsg.ActorStop` and calls `CoreMailboxActors.dispatchActorStop`.

[ ] `CoreMailboxLoop.dispatchItem` in `src/Server/Core/CoreMailboxLoop.fs` on every `CoreQueue.Notice` calls `CoreEventDispatch.logNotice` then `applyNotice` with no case split for `Notice.ActorStopped`.

### Existing tests

[ ] ``mailbox appends StartActor and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` places `TestActor.functionActorAt` via `CoreMailboxApi.coreMailboxStartActorFromFunction`, waits with `waitForActorStop`, and asserts start then stop order, `Authority "Actor"` on the stop row, and `EventBody.ActorStop` body with `ActorFailed ""`.

[ ] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` cancels a live `?ai` run, then calls `CoreMailboxApi.actorStop` with the live secret; asserts one stop on the focus and none on the graph root.

[ ] ``ActorStop reads the carrier graph once`` in `tests/Server.Tests/QueryActorTests.fs` starts `SearchActor.functionStart`, runs `pool.schedule`, and reads `ActorQuery` from the log via `queryStop` and `CoreMailboxApi.eventHistory`.

### Copy-from

[ ] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` is the lifecycle append that stores the caller `authority` on the stop row.

[ ] `CoreEventDispatch.logNotice` in `src/Server/Core/CoreEventDispatch.fs` is the notice append with `Authority ""`.

[ ] `CoreMailboxLoop.dispatchItem` in `src/Server/Core/CoreMailboxLoop.fs` is today's notice queue arm (`logNotice` then `applyNotice`).

[ ] `CoreActorPool.trySecretForActorNode` in `src/Server/Core/CoreActorPool.fs` is the live-row secret lookup `dispatchActorStop` parallels with `getActorNodeId`.

[ ] `CoreDoor.actorCaller` in `src/Server/Core/CoreDoor.fs` builds the Actor `Caller` from a minted pool secret.

### implement

[ ] Ticket claim: `CoreActorPool.runSchedule` binds the returned `ActorResult` and posts `Notice.ActorStopped` through `CoreMailboxApi.postNotice` on `coreChanges.host`. Code today starts `Async.Start(work |> Async.Ignore, row.cancel.Token)` and posts no notice (`src/Server/Core/CoreActorPool.fs`, `runSchedule`).

[ ] Ticket claim: work that is cancelled posts nothing from the pool. Code today has no cancel-aware post in `runSchedule` (`src/Server/Core/CoreActorPool.fs`, `runSchedule`).

[ ] Ticket claim: for `Notice.ActorStopped`, `CoreMailboxLoop.dispatchItem` uses `pool.trySecretForActorNode`, logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`, then `pool.finish`. Code today runs `logNotice` for every notice including `Notice.ActorStopped` and never calls `pool.finish` from the notice arm (`src/Server/Core/CoreMailboxLoop.fs`, `dispatchItem`).

[ ] Ticket claim: Actor bodies return the result and do not call a stop. Code today still calls stop in `GithubTransportActor.finishActor`, `RunAgentActor.stop`, `SearchActor.queryActorBody`, `SearchActor.searchActorAt`, `TestActor.dispatch`, and `TestActor.runBehavior` (symbols named in the ticket).

[ ] Ticket claim: `RunAgentActor.stop` leaves. Code today defines and uses `RunAgentActor.stop` (`src/Server/RunAgentActor.fs`).

[ ] Ticket claim: `SearchActor.functionStart` and `SearchActor.functionActorAt` stop passing a stop into `queryActorBody`. Code today still passes `CoreDoor.actorStop` and `changes.actorStop` (`src/Server/SearchActor.fs`).

[ ] ``an Actor that returns logs Notice.ActorStopped`` observes the change at the mailbox door: register or place a `root` `ActorFunction` that returns `ActorSucceeded` without calling a stop; start with `CoreMailboxApi.coreMailboxStartActorFromFunction` like `createHost` / `startFromFunction` / `functionPlacement` in `tests/Server.Tests/CoreMailboxDoorTests.fs`, or `startRootActor` in `tests/Server.Tests/ActorCoreChangesDoorTests.fs`; read the log with `eventPast`, `CoreMailboxApi.eventHistory`, or `waitForNotices` from `tests/Server.Tests/TestBackend.fs`; assert `EventBody.ActorStop` / `Notice.ActorStopped` for the focus after the start row; assert the focus id is absent from `pool.liveActorNodeIds()` after the notice is handled.

[ ] ``ActorStopped for an idle focus logs nothing`` observes the change at `CoreMailboxApi.postNotice` (same seam as `tests/Server.Tests/CoreLoopNoticeTests.fs` and other server notice tests): under `withHost` or `createHost` from `tests/Server.Tests/CoreMailboxDoorTests.fs`, capture `CoreMailboxApi.eventHistory` before and after posting `Notice.ActorStopped(focusId, ActorSucceeded)` for a `NodeId` with no live row; assert the event list is unchanged.
