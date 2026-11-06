# 08 — Start the AI Actor

Ticket: [[08-dispatch-the-ai-actor-ticket.md]]
Spec: `plan/actor-as-client/spec.md` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

- **Scan.** `python` is absent. `python3 scripts/standards-scan.py --diff origin/staging` exited 1. The three-dot merge-base with `origin/staging` is `9e230332`, so that range also contains the parent commit already squashed on `staging` as [544 — Remove unused Order.StartPeerActor and Order.LoadSave](https://github.com/abaljeu/ambit/pull/544). `python3 scripts/standards-scan.py --diff f32290ca` is the pull-request base. Both scans print the same two `MUTABLE` lines. No `LONG`, `TAB`, `BARE_ID`, `PHRASE`, or `FILE` line. Touched bindings in the pull-request diff are under 40 lines. The longest is `queueAdmittedOrder` at 29 lines.
- **Out parameter.** **Hard.** Breaks [fsharp-source](.agents/rules/fsharp-source.md): Don't use mutable. `src/Server/RunAgentActor.fs` `tryBound` is `let mutable found = Unchecked.defaultof<AiMaterials>`.
- **Harness out parameter.** **Hard.** Same rule: Don't use mutable. `tests/Server.Tests/AskCancelHarness.fs` `readHostAi` is `let mutable found = Unchecked.defaultof<HostAi>`.
- **Two admit copies.** **Judgement.** Duplicated Code. `queueAiOrder` and `queueTestOrder` in `src/Server/Core/CoreMailboxActors.fs` repeat the `admitCaller` match. Source: [[plan/context/SMELLS.md]]. The hunk is `match CoreMailboxEvents.admitCaller context caller with`.
- **Two material records.** **Judgement.** Data Clumps. `AiMaterials` in `src/Server/RunAgentActor.fs` and `HostAi` in `tests/Server.Tests/AskCancelHarness.fs` are the same three fields. The hunk is `keys: AiKeySet` plus `repos: AiRepo list` plus `grok: GrokBotBinding`.
- **Start name.** **Judgement.** Mysterious Name. `hasActorStart` now matches `EventBody.Order (Order.ActorOrder order)`. The hunk is `| EventBody.Order (Order.ActorOrder order) ->`.
- **Order helper arity.** **Judgement.** Argument Count. `queueAdmittedOrder` takes six positional arguments. The hunk is `let private queueAdmittedOrder`.
- **Shotgun Surgery.** Suppressed. The source edits are the five Output files. 08 — Start the AI Actor names those files.

## 2. Spec

- **One job.** A Run whose command text is `ai` does not call `startActor`. `postActorStart` matches that name, `queueAiOrder` calls `admitCaller`, and `queueAdmittedAi` builds `ActorOrder` and `queueAdmittedOrder` calls `MailboxHost.postActorOrder`. `actorForNode` applies `actorFn` to the bound keys, repos, `GrokBotBinding`, and that Run's actor node id. Names other than `ai` and `test` still reach `postChosenStart`, which calls `startActor`.
- **Session id.** `FunctionRun` gains `sessionId`. `putFunctionStart` still mints `Guid.NewGuid` and stores it on the live row. `runSchedule` copies `row.sessionId`. `deliver` and `takeInbox` still key the row by that id. `completeGrok` passes `ask.sessionId` to `GrokBotRunner.wake` and to `requestGrokCancel`. `askFrom` reads `run.sessionId`. The actor closure holds the keys, the repos, `GrokBotBinding`, and the actor node id.
- **Registers.** `RouteRegistration` passes `Actors = []` and calls `bindActorMaterials` with the keys, repos, and `GrokBotBinding`. `createHost` no longer registers `ActorName "ai"`.
- **Seam.** `startAiRun` applies `actorFn` to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id, then calls `MailboxHost.postActorOrder` with `testCaller`. It does not cancel. `cancelByFocus` still posts `Order.CancelActor` with the focus node id.
- **Bounds.** The diff adds no route, no `ActorDispatch.fs`, and no `Order.StartActor` or `Order.RunActor` case. The function on the order is the applied `actorFn`, not `Map.tryFind` on the actor name.
- **Focus wrapper.** **Extra.** `runSchedule` for every `FunctionPending` sets `getGraph` to `getGraph () |> Graph.withFocus (Some row.focusId)`. `packExtract` then passes `ask.focusId` as both zoom and focus. Ask facts set zoom and focus to the same node, so the suite does not show a Run whose zoom is a different node.
- **Door ack.** **Extra.** `queueAdmittedOrder`, shared with `?test`, appends `EventBody.ActorStart` for the client submission, then posts `ActorOrder`. `startAiRun` does not take this path.
- **Proof file.** **Extra.** `tests/Server.Tests/BrowserAskProofTests.fs` is outside Outputs. `lifecycleIndexes` now treats `Order.ActorOrder` as the start row. The test-file count in the diff is 2. The ticket limit is 4.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2983` printed `DONE` and `/tmp/ambit-full-suite-20261106-212932.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 752, skipped 1, failed 0. `tests/Shared.Tests` passed 1937, skipped 3, failed 0. `scripts/check-test.sh` is not executable. The bare path prints `Permission denied`.
- **Seam.** `startAiRun` posts `ActorOrder`. `createHost` does not register `ActorName "ai"`. ``second launch on the same Focus is refused`` calls `startAiRun` for both starts.
- **Proof.** ``Browser Ask from what I see finishes with Poll Focus Children and drops live`` looks for `ActorOrder` before `ActorStop`, then asserts the owned text `from-agent`.
- **Product door.** No fact posts `EventBody.ActorStart` for command text `ai`. The ticket says the ask-start seam is the only test start for `?ai`.
- **Check file.** `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md` starts with `pass`. This review did not re-run that file's filters. The full suite above includes those assemblies and failed 0.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
