# 08 — Start the AI Actor

Ticket: [[08-dispatch-the-ai-actor-ticket.md]]
PR: https://github.com/abaljeu/ambit/pull/545
Diff base: `f32290ca` (`cursor/remove-order-peer-and-loadsave-ff54`). That commit later squash-landed onto `staging` as `bff40213`. `git diff f32290ca bff40213` is empty, so the ticket-08 diff is the same against either tree. This review did not rebase.

## Verdict

must-fix addressed

The one job holds on the running path. `postActorStart` for `?ai` admits the caller, then posts `Order.ActorOrder`. `actorFn` returns `ActorFunction`. `FunctionRun.sessionId` is the id `putFunctionStart` minted. `startAiRun` now posts that order.

## Must-fix

1. **Addressed.** `startAiRun` posts `Order.ActorOrder`. The actor is `RunAgentActor.actorFn` applied to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id. The order carries that function, the FocusId, and actor name `ai`. The caller is the admitted test caller. `offerActor` is gone. `startAiRun` does not post `EventBody.ActorStart`.

## Notes

1. **One job.** A `?ai` Run does not call `CoreMailboxApi.startActor` and does not post `CoreMsg.StartActor`. `queueAiOrder` calls `admitCaller` before `queueAdmittedAi`. A refused caller records the failed start and does not post `ActorOrder`. Names other than `ai` and `test` still reach `postChosenStart`, which calls `startActor`. `startOnCommand` in the harness still calls `startActor` for a probe `ActorFn`.
2. **Session id.** `History.fs` adds only `FunctionRun.sessionId`. `putFunctionStart` is unchanged and still mints `Guid.NewGuid`. `runSchedule` sets `sessionId` from `row.sessionId`. `deliver` and `takeInbox` are unchanged and still key the live row by that id. `completeGrok` passes `ask.sessionId` to `GrokBotRunner.wake`. `actorFn` closes over the keys, the repos, `GrokBotBinding`, and the actor node id. The session id is read from `run` when the function runs. `requestGrokCancel` still receives that same string inside the run, as the working input describes. `startAiRun` does not cancel. `cancelFocus` still calls `cancelByFocus` with the focus node id.
3. **Registers.** `RouteRegistration` passes `Actors = []` and binds keys, repos, and `GrokBotBinding` on the pool after boot. `createHost` no longer registers `ActorName "ai"`. The product `?ai` function is `actorFn` applied to those values and the actor node id. `startAiRun` applies the same function and posts it on the order. It is not `Map.tryFind` on the actor name.
4. **`runSchedule` also sets focus.** The same arm wraps `getGraph` with `Graph.withFocus (Some row.focusId)` for every `FunctionPending`, not only `?ai`. `packExtract` then uses that focus id as both zoom and focus. Ask tests set zoom and focus to the same node, so they still pass. A Run whose zoom is a different node posts a smaller extract than the old `ActorInput.zoomId` path.
5. **Deleted order cases stay deleted.** The `History.fs` hunk is the `sessionId` field. `Order.StartPeerActor` and `Order.LoadSave` are not in the diff. The pre-existing `Order.StartActor` case is still the lifecycle constructor; this diff does not add a case.
6. **File list.** Five source files match Outputs: `History.fs`, `CoreActorPool.fs`, `CoreMailboxActors.fs`, `RunAgentActor.fs`, `RouteRegistration.fs`. The harness is the Outputs test file. `BrowserAskProofTests.fs` now looks for `ActorOrder` before `ActorStop`, because `startAiRun` no longer writes `ActorStart`. No route, no `ActorDispatch.fs`.
7. **Check report.** After the fix, `scripts/agent-test.sh` with no arguments passed `BrowserAskProofTests|CoreActorPoolTests` (2) and `EventJsonTests|HistoryTests|MailboxLogTests` (86). The ask, cancel, function-start, and test-command filter passed 76.
8. **Sibling.** Open PR 543 is the Outputs trim onto `staging`. This branch already contains that trim (`06475270`). No other open PR touches these product files.
9. **Commits.** Each commit on this branch after `f32290ca` names one file.

## Stack

The PR base ref is still `cursor/remove-order-peer-and-loadsave-ff54`. Its tip `f32290ca` is not the squash commit `bff40213` now on `staging`. The trees match, so the ticket-08 review does not need a rebase.
