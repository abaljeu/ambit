# 113 — Actors return their ActorResult

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md)

## 3. Look at

### Owner

[ ] [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) owns `ActorFn`, `FunctionActor`, and `runSchedule`

### Source

[ ] [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) the `ActorFn` it returns
[ ] [RunAgentActor.fs](src/Server/RunAgentActor.fs) `actorFn`, `stop`
[ ] [SearchActor.fs](src/Server/SearchActor.fs) `functionStart`
[ ] [TestActor.fs](src/Server/TestActor.fs) `dispatch`, `functionActor`

### Match sites and constructors

[ ] `ActorFn` is built in `GithubTransportActor`, `RunAgentActor.actorFn`, and `TestActor.dispatch`
[ ] `FunctionActor` is built in `SearchActor.functionStart` and `TestActor.functionActor`
[ ] `runSchedule` is the only caller of an `ActorFn` or a `FunctionActor`
[ ] Test Actors built as lambdas: `CoreActorPoolDeliverTests.hangActor`, `AskCancelHarness.postChildThenWait`, `ActorCoreChangesDoorTests` (3 bodies passed to `startRootActor`), `QueryActorTests` and `OrderedEventStreamTests` (the `gate` Actor), `AgentAskTests` (`probe`), `CancelByFocusTests` (`probe`), `CoreMsgActorCasesTests` (3 `root` bodies), `CoreMailboxDoorTests` (the `root` body)

### Tests this ticket names

[ ] [CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) `StartActor reply is startActor bookkeeping without waiting for an Actor body`, `GetState stamps lockPresent from the live table after startActor`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) the tests that register the `root` Actor

## 4. Context

### Today

[ ] `ActorFn` is `ActorInput -> CoreChangesDoor -> Async<unit>`; `FunctionActor` is `FunctionRun -> CoreChangesDoor -> Async<unit>`
[ ] Each of the four Actors works out an `ActorResult`, then calls `CoreDoor.actorStop` with it
[ ] `runSchedule` starts the work with `Async.Start` on the row's cancellation token and does not see it end
[ ] A test Actor of `fun _ _ -> async.Return ()` ends at once and its pool row stays live

### Decisions

[ ] Alan, 2026-10-24: an Actor does not send a stop; it just stops. The pool sees the Actor end and posts the stop Notice. [project.md](../project.md) decision of 2026-10-24
[ ] This ticket is the refactor that gives the pool the result; [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) uses it

## 5. Changes

### Change

[ ] One refactor: `ActorFn` and `FunctionActor` return `Async<ActorResult>`. Behavior does not change

### CoreActorPool.fs

[ ] `ActorFn` is `ActorInput -> CoreChangesDoor -> Async<ActorResult>`
[ ] `FunctionActor` is `FunctionRun -> CoreChangesDoor -> Async<ActorResult>`
[ ] `runSchedule` starts `work |> Async.Ignore`; the pool does not read the result yet

### Actors

[ ] Each of the four Actors still calls `CoreDoor.actorStop` with its result, then returns that result
[ ] `SearchActor.functionStart` returns its `ActorQuery`

### Tests

[ ] Each test Actor named under Match sites returns an `ActorResult`
[ ] A test Actor that ends at once and expects its row to stay live waits on its cancellation token instead, so it stays live after 117: the `root` bodies in `CoreMsgActorCasesTests.fs` and `CoreMailboxDoorTests.fs`
[ ] No test's assertions change

### Size

[ ] 5 source files: `CoreActorPool.fs`, `GithubTransportActor.fs`, `RunAgentActor.fs`, `SearchActor.fs`, `TestActor.fs`
[ ] 9 test files, each only a test Actor's body: `CoreActorPoolDeliverTests.fs`, `AskCancelHarness.fs`, `ActorCoreChangesDoorTests.fs`, `QueryActorTests.fs`, `OrderedEventStreamTests.fs`, `AgentAskTests.fs`, `CancelByFocusTests.fs`, `CoreMsgActorCasesTests.fs`, `CoreMailboxDoorTests.fs`
[ ] Over the 4-test limit (2026-10-25). Alan accepted it over the size limit on 2026-10-24 ([core-refinement](../project.md)). A split is his call; this ticket is not split here
[ ] 0 tests change what they check; the type change cannot build in smaller batches

## 6. Tests

### Kept

[ ] Keep every test in the nine files
[ ] Filter: `CoreActorPool`
[ ] Filter: `CoreMsgActorCasesTests`
[ ] Filter: `CoreMailboxDoorTests`
[ ] Filter: `QueryActorTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
