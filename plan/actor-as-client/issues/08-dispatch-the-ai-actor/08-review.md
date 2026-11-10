# 08 — Start the AI Actor

Ticket: [[08-dispatch-the-ai-actor-ticket.md]]
Spec: [[plan/actor-as-client/spec.md]] is absent. This project has no spec.md. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

1. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed one `MUTABLE` line, `tests/Server.Tests/AskCancelHarness.fs:304`, `let mutable bound =`. [fsharp-source](.agents/rules/fsharp-source.md) says "This rule does not apply to tests." The measured bindings are under 40 lines. The longest new binding is `queueAdmittedAi` at 23 lines. No `LONG`, `TAB`, `BARE_ID`, `PHRASE`, or `FILE` line.
2. **Install cell.** **Hard.** The same rule says "Don't use mutable." `src/Server/RunAgentActor.fs` stores the boot binding in `let private aiInstall = ref Option<AiInstall>.None`.
3. **Reader cell.** **Hard.** Same rule. `src/Server/Core/CoreActorPool.fs` module `LiveSession` stores pool readers in `let private readers = ref Map.empty<Guid, Credential -> string option>`.
4. **Order shape.** **Judgement.** Duplicated Code. `queueAdmittedAi` in `src/Server/Core/CoreMailboxActors.fs` and `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs` both build `ActorOrder` with `actorNodeId = request.focusId` and `actorName = "ai"`, then call `MailboxHost.postActorOrder`. Source: [[plan/context/SMELLS.md]]. The hunk is `actorNodeId = request.focusId`.
5. **Binding record.** **Judgement.** Duplicated Code. `AiInstall` in `src/Server/RunAgentActor.fs` and `AiHostBinding` in `tests/Server.Tests/AskCancelHarness.fs` are the same three fields, `keys`, `repos`, and `grok`. The hunk is `{ keys: AiKeySet; repos: AiRepo list; grok: GrokBotBinding }`.

## 2. Spec

1. **One job.** A Run whose text selects `ai` posts `Order.StartActor`. `postActorStart` calls `queueAiOrder`, which admits the caller, then `queueAdmittedAi` builds `ActorOrder` and calls `MailboxHost.postActorOrder`. `startAiRun` posts that same order.
2. **Actor function.** `RunAgentActor.actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> ActorStart -> ActorFunction`. `ActorFunction` is the one in `src/Shared/History.fs`. `inputFor` reads `ActorStart.zoomId`, `ActorStart.focusId`, and `ActorStart.runDefinitionId`. The closure holds the binding.
3. **Session id.** `CoreActorPool.sessionIdFor` reads the live-row `sessionId` that `putFunctionStart` stores. `inputFor` copies that string onto `ActorInput.sessionId`. `completeGrok` reads `input.sessionId`. `FunctionRun` in `src/Shared/History.fs` stays `getGraph` and `secret`. The `ActorFunction` closure captures `ActorStart`, the keys, the repos, and the binding. It captures no session id.
4. **Boot.** `RouteRegistration.CreateBoot` calls `RunAgentActor.installActorBinding` with `AiKeys.fromConfig`, `AiRepos.fromConfig`, and `GrokBotSettings.fromConfigBinding`. `Actors` is empty. `AskCancelHarness.createHost` drops the `ai` pair from `pool.register` and stores that host's keys, repos, and binding.
5. **Other names.** The `postActorStart` arm other than `test` and `ai` still calls `postChosenStart`. ``Ask extract carries Graph.focus from launch`` still enters through `startOnCommand`, which calls `CoreMailboxApi.coreMailboxStartActor`.
6. **Cancel.** `actorNodeId` is `ActorStart.focusId`. `cancelFocus` calls `CoreMailboxApi.cancelByFocus` with that id. `startAiRun` posts the order and does not cancel.
7. **Second launch.** ``second launch on the same Focus is refused`` calls `startAiRun` for both starts. The asserted text is `actor node already has a live Actor` from `admitFunctionStart`.
8. **Blank session.** **Extra.** `sessionFor` returns `""` when `CoreActorPool.sessionIdFor` returns `None`. The hunk is `| None -> ""`.
9. **Install refusal.** **Extra.** `queueAdmittedAi` replies `Error "ai actor is not installed"` when `installActorBinding` has not run. The hunk is `reply.Reply(Error "ai actor is not installed")`.
10. **Installed actor.** **Unnamed.** `RunAgentActor.installedActor` is a new public function. The ticket names `installActorBinding` and `actorFn`.

## 3. Arch

1. **AI actor.** `RunAgentActor.actorFn` returns `ActorFunction` for the `ActorStart` on that Run. The [x] line in **AI actor** matches `src/Server/RunAgentActor.fs`. The [o] line records the old `ActorFn` return. Not a blocker.
2. **Posted run.** `postActorStart` for `ai` applies `RunAgentActor.actorFn` to that `ActorStart` and calls **Post actor order**. The [x] story line matches `queueAiOrder` in `src/Server/Core/CoreMailboxActors.fs`. Module map item 11, **Posted run**, still marks Post actor order as [ ]. The code follows the story line. Not a blocker.

## 4. Tests

1. **Suite.** `scripts/check-test.sh 2848` printed `Permission denied`. The script is not executable. `bash scripts/check-test.sh 2848` printed `DONE` and `/tmp/ambit-full-suite-20261110-135411.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 759, failed 0, skipped 1. The skip is `DocumentPersistenceTests.Change.upload structure persistence preserves existing file mtime`. `tests/Shared.Tests` passed 1937, failed 0, skipped 3. The skips are `WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`, and `WorkspaceLocalInventoryTests.listForPush without git still returns walk`. `scripts/agent-test.sh` with no arguments runs the mapped subset for the diff. `scripts/agent-test.sh --full` runs `dotnet test tests/Server.Tests` and `dotnet test tests/Shared.Tests` with no filter.
2. **Ask seam.** `startAiRun` posts `Order.StartActor` with `actorNodeId = request.focusId` and `actorName = "ai"`. `startAsk` and `startBrowserAsk` call `startAiRun`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`.
3. **Second launch.** ``second launch on the same Focus is refused`` calls `startAiRun` twice and asserts `actor node already has a live Actor`.
4. **Ask suites.** `AgentActorStreamTests`, `AgentAuthErrorTests`, `AgentFailurePreserveTests`, `AgentGrokBotStreamTests`, `AiKeysTests`, and `AiReposTests` call `startAsk` or `startLiveAsk`.
5. **Browser proof.** `BrowserAskProofTests` calls `launchBrowserAsk`. The start index matches `Order.StartActor` for that focus id and actor name `ai`.
6. **Cancel.** `CancelByFocusTests` starts `?ai` through `startLiveAsk`, then `cancelFocus`. That cancel is `cancelByFocus` with the focus node id.
7. **Probe.** ``Ask extract carries Graph.focus from launch`` and ``Change before Cancel keeps the accepted children`` call `startOnCommand`. That path calls `CoreMailboxApi.coreMailboxStartActor` for the registered probe.

## 5. Miss

1. No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. No findings.
