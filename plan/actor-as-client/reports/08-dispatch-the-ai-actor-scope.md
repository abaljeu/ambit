# 08 — Start the AI Actor — scope (implement)

Branch: `staging`. Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md). Model: [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md).

### Signatures

[ ] `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs`: `AiKeySet -> AiRepo list -> GrokBotBinding -> ActorFn` where `ActorFn` is `ActorInput -> CoreChangesDoor -> Async<unit>` (`src/Server/Core/CoreActorPool.fs`).
[ ] Ticket Reference target (not in code today): `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction` with pool `ActorFunction` = `FunctionRun -> CoreChangesDoor -> Async<unit>`.
[ ] Shared `ActorFunction` in `src/Shared/History.fs`: `FunctionRun -> ActorChanges -> Async<unit>`.
[ ] Shared `FunctionRun` in `src/Shared/History.fs`: `{ getGraph: GraphCarrier; secret: Credential }`.
[ ] Shared `ActorOrder` in `src/Shared/History.fs`: `{ actor: ActorFunction; actorNodeId: NodeId; actorName: string }`.
[ ] Shared `Order` cases used here in `src/Shared/History.fs`: `StartActor of ActorOrder`, `StartActorDeprecated of ActorStart`, others unchanged.
[ ] Pool `ActorFn` / `ActorFunction` / `ActorInput` / `FunctionStart` in `src/Server/Core/CoreActorPool.fs`; `ActorInput` includes `sessionId: string` and does not appear on `FunctionRun`.
[ ] `MailboxHost.postActorOrder` in `src/Server/Core/MailboxHost.fs`: `MailboxHost -> Caller -> ActorOrder -> Async<Result<EventId, string>>`.
[ ] `CoreMailboxActors.postActorStart` in `src/Server/Core/CoreMailboxActors.fs`: private; `MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit`.
[ ] `CoreMailboxActors.queueTestOrder` in `src/Server/Core/CoreMailboxActors.fs`: private; same arity pattern as `postActorStart` minus `event`.
[ ] `CoreMailboxActors.queueAdmittedTest` in `src/Server/Core/CoreMailboxActors.fs`: private; `MailboxContext -> Caller -> ActorStart -> PostedReply -> unit`.
[ ] `CoreMailboxActors.startChosen` in `src/Server/Core/CoreMailboxActors.fs`: private; `MailboxContext -> ActorStart -> (unit -> Graph) -> Result<Credential option, StartError>`.
[ ] `CoreActorPool.startActor` on the pool record in `src/Server/Core/CoreActorPool.fs`: `ActorStart -> (unit -> Graph) -> Result<Credential, StartError>`.
[ ] `CoreMailboxApi.coreMailboxStartActor` in `src/Server/Core/CoreMailboxApi.fs`: `MailboxHost -> Caller -> ActorStart -> Async<Result<unit, string>>` (places `Order.StartActorDeprecated`).
[ ] `CoreMailboxApi.coreMailboxStartActorFromFunction` in `src/Server/Core/CoreMailboxApi.fs`: posts `Order.StartActor` via `MailboxHost.postActorOrder`.
[ ] `TestActor.functionActorAt` in `src/Server/TestActor.fs`: `NodeId -> NodeId -> ActorFunction`.
[ ] `ActorOrderFromStart.orderFromStart` in `src/Server/ActorOrderFromStart.fs`: `(NodeId -> NodeId -> ActorFunction) -> string -> (ActorStart -> NodeId) -> ActorStart -> ActorOrder`.
[ ] `GrokBotBinding` in `src/Server/GrokBotSettings.fs`: `{ Config: GrokBotConfig; ResponseUrl: string }`; built by `GrokBotSettings.fromConfigBinding` in the same file and in `RouteRegistration.CreateBoot` / `AskCancelHarness.createHost`.
[ ] `tests/Server.Tests/AskCancelHarness.fs` `startAiRun`: `MailboxHost -> ActorStart -> Task<Result<unit, string>>` (body calls `CoreMailboxApi.coreMailboxStartActor`).
[ ] `CoreMsg` in `src/Server/Core/CoreMsg.fs`: `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, `PostEvent` (no `StartPeerActor` case today).
[ ] `ActorDispatch.fs`: absent under `src/Server/`.

### Match sites

[ ] `RouteRegistration.CreateBoot` in `src/Server/RouteRegistration.fs` builds `CoreBoot.Actors` with `ActorName "ai"` paired to curried `RunAgentActor.actorFn`.
[ ] `CoreRuntime.createPublishing` in `src/Server/Core/CoreRuntime.fs` calls `pool.register` for each `CoreBoot.Actors` pair.
[ ] `CoreActorPool.runStartActor` in `src/Server/Core/CoreActorPool.fs` matches command text to `actorName`, looks up `ActorFn` in `model.defs`, calls `putActorStart` with `BagPending(actorFn, input)`.
[ ] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` invokes `actorFn input coreChanges` for `BagPending`.
[ ] `RunAgentActor.completeGrok` in `src/Server/RunAgentActor.fs` reads `input.sessionId` for Grok wake and cancel.
[ ] `CoreMailboxActors.dispatchPostEvent` in `src/Server/Core/CoreMailboxActors.fs` matches `EventBody.ActorStart` → `postActorStart`.
[ ] `CoreMailboxActors.postActorStart` matches `actorNameOn` → `Some "test"` → `queueTestOrder`; any other name → `postChosenStart` → `startChosen` → `pool.startActor` (includes `?ai` today).
[ ] `CoreMailboxActors.queueAdmittedTest` builds `ActorOrder` and calls `MailboxHost.postActorOrder` (copy-from path for `?test`).
[ ] `CoreMailboxLoop.placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` matches `Order.StartActorDeprecated` → `dispatchStartActor`; `Order.StartActor` → function-shaped schedule (separate branch).
[ ] `CoreMailboxApi.coreMailboxStartActor` constructs `Order.StartActorDeprecated request` and `postPlacedOrder`.
[ ] `AskCancelHarness.createHost` in `tests/Server.Tests/AskCancelHarness.fs` `pool.register (ActorName "ai")` with `RunAgentActor.actorFn keys repos grok`.
[ ] `AskCancelHarness.startAsk`, `startLiveAsk`, `startBrowserAsk`, `launchBrowserAsk` call `startAiRun` (not `pool.register` lookup for a live `?ai` beyond harness registration).
[ ] `AskCancelHarness.cancelFocus` calls `CoreMailboxApi.cancelByFocus` (cancel path for `CancelByFocusTests` in `tests/Server.Tests/CancelByFocusTests.fs`).

### Existing tests

[ ] `tests/Server.Tests/AgentAskTests.fs` — ``Ask with fake completion replaces Focus Children through Core Change`` — fake Agent completes and Focus children update via `startAsk`.
[ ] `tests/Server.Tests/AgentAskTests.fs` — ``Ask ignores Command args and still replaces Focus Children`` — `?ai later please` still replaces children.
[ ] `tests/Server.Tests/AgentAskTests.fs` — ``Ask empty success clears every Focus Child`` — empty fake reply clears Focus.
[ ] `tests/Server.Tests/AgentAskTests.fs` — ``Ask extract carries Graph.focus from launch`` — probe `ActorFn` via `startOnCommand`, not `?ai`.
[ ] `tests/Server.Tests/AgentAskTests.fs` — ``second launch on the same Focus is refused`` — two `startAiRun` on same Focus; second errors with `"focus"`.
[ ] `tests/Server.Tests/AgentAskTests.fs` — ``credentialed Change under Focus is accepted while Ask runs`` — concurrent Change while Ask runs.
[ ] `tests/Server.Tests/AgentActorStreamTests.fs` — ``fake stream deltas add Focus children without text-edit`` — streaming adds children.
[ ] `tests/Server.Tests/AgentActorStreamTests.fs` — ``next open angle commits Focus child before next tag closes`` — partial XML streaming.
[ ] `tests/Server.Tests/AgentActorStreamTests.fs` — ``cancel mid-stream drops live and keeps streamed children`` — cancel mid-stream.
[ ] `tests/Server.Tests/AgentAuthErrorTests.fs` — ``setFake unauthorized names Cursor on ActorStop`` — auth error surfaces on stop.
[ ] `tests/Server.Tests/AgentAuthErrorTests.fs` — ``empty AiKeys names Cursor missing key on ActorStop`` — missing key on stop.
[ ] `tests/Server.Tests/AgentFailurePreserveTests.fs` — ``Ask Failed preserves Focus Children and posts no Change`` — failure leaves Focus unchanged.
[ ] `tests/Server.Tests/AgentGrokBotStreamTests.fs` — eight facts — wake, deliver, stream, cancel, config errors, and the other `AgentRunner` path; all use `startAsk` / `startLiveAsk`.
[ ] `tests/Server.Tests/AiKeysTests.fs` — three ``Ask …`` facts — API key resolution via `startAsk`.
[ ] `tests/Server.Tests/AiReposTests.fs` — three ``AI …`` facts — repo resolution via `startAsk`.
[ ] `tests/Server.Tests/BrowserAskProofTests.fs` — ``Browser Ask from what I see finishes with Poll Focus Children and drops live`` — `launchBrowserAsk` for `?ai`.
[ ] `tests/Server.Tests/CancelByFocusTests.fs` — nine facts — `startLiveAsk` then `cancelFocus` / posted Cancel; live row and lifecycle.
[ ] `tests/Server.Tests/TestActorHelloTests.fs` / `TestActorCommandErrorTests.fs` — hello and error paths through `postActorOrder` (model for [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md), not Outputs of this ticket).
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — ``CoreMailboxApi.startActor …`` facts — name in comments; body calls `coreMailboxStartActor` with harness `test`/`root` actors, not `?ai`.

### Copy-from

[ ] `CoreMailboxActors.queueAdmittedTest` in `src/Server/Core/CoreMailboxActors.fs` — after `admitCaller`, builds `ActorOrder` with `ActorOrderFromStart.orderFromStart TestActor.functionActorAt "test" (fun start -> start.focusId) request` and awaits `MailboxHost.postActorOrder`.
[ ] `TestActor.functionActorAt` in `src/Server/TestActor.fs` — curried `NodeId -> NodeId -> Shared.ActorFunction` closed over run definition id and focus id.
[ ] Intended `?ai` mirror: same admit gate, same `postActorOrder`, replace `functionActorAt` with `RunAgentActor` closure returning pool `ActorFunction`, pick `actorNodeId` from the Run (ticket Process §2).

### implement

#### Working inputs (ticket Inputs → code on `staging`)

[ ] **RouteRegistration registers `ActorName "ai"` as `RunAgentActor.actorFn`.** Matches — `RouteRegistration.CreateBoot` in `src/Server/RouteRegistration.fs`, `Actors` list line pairs `ActorName "ai"` with `RunAgentActor.actorFn (AiKeys.fromConfig …) (AiRepos.fromConfig …) (GrokBotSettings.fromConfigBinding …)`.
[ ] **`RunAgentActor.actorFn` is an `ActorFn`; reads `input.sessionId`.** Matches — `RunAgentActor.actorFn` returns `ActorFn` in `src/Server/RunAgentActor.fs`; `completeGrok` binds `let sessionId = input.sessionId`.
[ ] **`FunctionRun` is `getGraph` and `secret`.** Matches — `src/Shared/History.fs` `FunctionRun` record fields only.
[ ] **`ActorInput.sessionId` and live-row `sessionId`.** Matches — `ActorInput` in `src/Server/Core/CoreActorPool.fs`; `LiveRow.sessionId` in the same module; minted in `putActorStart` and `putFunctionStart`.
[ ] **`postActorOrder` on `MailboxHost`.** Matches — `MailboxHost.postActorOrder` in `src/Server/Core/MailboxHost.fs`.
[ ] **`postActorStart` for `?test` admits caller, posts `Order.StartActor`; `queueAdmittedTest` uses `TestActor.functionActorAt`.** Matches — `postActorStart` → `queueTestOrder` → `admitCaller` then `queueAdmittedTest` in `src/Server/Core/CoreMailboxActors.fs`; order built with `TestActor.functionActorAt` via `ActorOrderFromStart.orderFromStart`.
[ ] **`AskCancelHarness.createHost` registers `ActorName "ai"` as `RunAgentActor.actorFn`.** Matches — `createHost` in `tests/Server.Tests/AskCancelHarness.fs`.
[ ] **`startAsk`, `startLiveAsk`, `startBrowserAsk` call `CoreMailboxApi.startActor`; `launchBrowserAsk` calls `startBrowserAsk`.** Differs — no `CoreMailboxApi.startActor` symbol; harness uses `startAiRun` → `CoreMailboxApi.coreMailboxStartActor` in `tests/Server.Tests/AskCancelHarness.fs`. `launchBrowserAsk` → `startBrowserAsk` → `startAiRun` matches call chain names after that substitution.
[ ] **``second launch on the same Focus is refused`` calls `CoreMailboxApi.startActor` for `?ai`.** Differs — `AgentAskTests` calls `startAiRun` twice in `tests/Server.Tests/AgentAskTests.fs` (same deprecated path as `coreMailboxStartActor`, not direct `startActor` name).

#### Ticket claims that differ from code (remaining Process / Outputs)

[ ] `postActorStart` for `?ai` still routes to `postChosenStart` / `pool.startActor` (`BagPending`); no `Order.StartActor` for `?ai` in `src/Server/Core/CoreMailboxActors.fs`.
[ ] `RunAgentActor.actorFn` is still pool `ActorFn`, not curried `NodeId -> ActorFunction` per ticket Reference and Process step 6.
[ ] `RouteRegistration.CreateBoot` and `AskCancelHarness.createHost` still register `ai` on the pool (`ActorFn` lookup path).
[ ] `startAiRun` exists (ticket Process §1 marked done) but still calls `coreMailboxStartActor` (`Order.StartActorDeprecated`); ticket Process step 11 requires `Order.StartActor` with applied `RunAgentActor.actorFn`.
[ ] Ticket Process step 8 lists `CoreMsg` including `StartPeerActor`; `src/Server/Core/CoreMsg.fs` has no such case (doc drift only).
[ ] Ticket One job unchecked items: `postActorStart` for `?ai`, removing `ai` from `pool.register`, and full ask suites on final seam — server behavior still on deprecated bag start for `?ai`.

#### How named Output tests observe the change at a public seam (no new test names in ticket)

[ ] All listed ask suites already enter through `startAiRun` / `startAsk` / `startLiveAsk` / `launchBrowserAsk` in `tests/Server.Tests/AskCancelHarness.fs`; after implementation they should still pass without calling `coreMailboxStartActor` for `?ai` inside those suites.
[ ] Observation seam: placed event body becomes `Order.StartActor` carrying `ActorOrder` with `actorName = "ai"` and pool `ActorFunction` from the Run closure (event history via `CoreMailboxApi.eventHistory` / poll helpers already used in harness).
[ ] ``second launch on the same Focus is refused`` — still two harness starts; exclusivity should come from mailbox admit + pool live rows whether path is deprecated or `StartActor`.
[ ] [CancelByFocusTests](tests/Server.Tests/CancelByFocusTests.fs) — `startLiveAsk` then `cancelFocus` → `CoreMailboxApi.cancelByFocus`; cancel must not move into `startAiRun` (ticket stop line).
[ ] [BrowserAskProofTests](tests/Server.Tests/BrowserAskProofTests.fs) — `launchBrowserAsk` one-node Browser path; should observe live row drop and Focus children after function-shaped actor completes.
[ ] Probe tests using `registerActor` + `startOnCommand` remain on `coreMailboxStartActor` / bag path for non-`ai` names (ticket: `startActor` still starts other command names).

#### `?test` `ActorOrder` fields today (exact)

[ ] `actor` = `TestActor.functionActorAt request.runDefinitionId request.focusId` (Shared `ActorFunction`).
[ ] `actorNodeId` = `request.focusId` (`actorNodeIdOf` in `queueAdmittedTest`).
[ ] `actorName` = `"test"`.

#### `actorFn` invocation today (all call sites)

[ ] Registration only: `RouteRegistration.CreateBoot`, `CoreRuntime.createPublishing`, `AskCancelHarness.createHost`.
[ ] Execution: `CoreActorPool.runStartActor` → `putActorStart` → `runSchedule` → `RunAgentActor.actorFn` body when command text resolves to `"ai"`.
[ ] No production `postActorOrder` path closes over `RunAgentActor` today.

#### Tests and APIs that start `?ai`

[ ] Ask Output files call `startAsk` / `startLiveAsk` / `launchBrowserAsk` only; none call `CoreMailboxApi.coreMailboxStartActor` directly for `?ai` except `startAiRun` in harness.
[ ] `CoreMailboxDoorTests` and others use `coreMailboxStartActor` with `?test` / `root` harness actors, not production `?ai`.

#### `startAiRun`

[ ] Present on `staging` in `tests/Server.Tests/AskCancelHarness.fs`; comment says first body calls `startActor`; implementation is `CoreMailboxApi.coreMailboxStartActor`.

#### Session id fields

[ ] `FunctionRun` (Shared): no `sessionId`.
[ ] `ActorInput`: `sessionId` set in `CoreActorPool.putActorStart`.
[ ] `LiveRow`: `sessionId` stored and indexed in `bySession` in `CoreActorPool`.
