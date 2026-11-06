# 08 — Start the AI Actor

Ticket: [[08-dispatch-the-ai-actor-ticket.md]]
PR: https://github.com/abaljeu/ambit/pull/545
Second review, after the prior must-fix.
Diff base: `f32290ca` (`cursor/remove-order-peer-and-loadsave-ff54`). That commit later squash-landed onto `staging` as `bff40213`. `git diff f32290ca bff40213` is empty. This review did not rebase.

## Verdict

notes only

The one job holds. The prior must-fix is closed. No new must-fix.

## Prior must-fix

Closed. `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs` builds `RunAgentActor.actorFn` from the harness keys, repos, `GrokBotBinding`, and that Run's actor node id, then calls `MailboxHost.postActorOrder` with that function, the FocusId, and actor name `ai`. The caller is `testCaller`, the caller in `admittedCredentials`. `offerActor` is gone. `startAiRun` does not post `EventBody.ActorStart` and does not post `CoreMsg.StartActor`.

## Must-fix

None.

## Notes

1. **One job.** A `?ai` Run does not call `startActor`. `postActorStart` matches the command text `ai`, `queueAiOrder` calls `admitCaller`, then `queueAdmittedAi` builds `ActorOrder` and `queueAdmittedOrder` calls `postActorOrder`. Names other than `ai` and `test` still reach `postChosenStart`, which calls `startActor`. `startOnCommand` still calls `startActor` for a probe `ActorFn`.
2. **Session id.** `History.fs` adds only `FunctionRun.sessionId`. `putFunctionStart` still mints `Guid.NewGuid` and stores it on the live row. `runSchedule` copies `row.sessionId`. `deliver` and `takeInbox` are unchanged and still key the row by that id. `completeGrok` passes `ask.sessionId` to `GrokBotRunner.wake`. `actorFn` closes over the keys, the repos, `GrokBotBinding`, and the actor node id. The session id is read from `run` when the function runs. `requestGrokCancel` still receives that string inside the run, as the working input describes. `startAiRun` does not cancel. `cancelFocus` still calls `cancelByFocus` with the focus node id.
3. **Registers.** `RouteRegistration` passes `Actors = []` and binds the keys, repos, and `GrokBotBinding` on the pool. `createHost` no longer registers `ActorName "ai"`. The product function is `actorFn` applied to the bound values and the actor node id. It is not `Map.tryFind` on the actor name.
4. **Product Run still stores `ActorStart`.** `queueAdmittedOrder`, shared with `?test`, appends `EventBody.ActorStart` for the client submission, then posts `ActorOrder`. That constructor is `Order.StartActor`. `startAiRun` does not take this path. The same append was already the `?test` door ack.
5. **`runSchedule` also sets focus.** The `FunctionPending` arm wraps `getGraph` with `Graph.withFocus (Some row.focusId)` for every function start. `packExtract` uses that focus id as both zoom and focus. Ask tests set zoom and focus to the same node. A Run whose zoom is a different node posts a smaller extract than the old zoom id.
6. **Deleted order cases stay deleted.** The `History.fs` hunk is the `sessionId` field. `Order.StartPeerActor` and `Order.LoadSave` are not in the diff. This diff does not add an `Order` case.
7. **File list.** Five source files match Outputs: `History.fs`, `CoreActorPool.fs`, `CoreMailboxActors.fs`, `RunAgentActor.fs`, `RouteRegistration.fs`. Also in the diff: the trimmed ticket, the check report, this review, and `AskCancelHarness.fs`. `BrowserAskProofTests.fs` is outside Outputs. It now looks for `ActorOrder` before `ActorStop`. That is two test files, under the limit of four. No route and no `ActorDispatch.fs`.
8. **Check report.** `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md` matches the post-fix runs: `scripts/agent-test.sh` passed `BrowserAskProofTests|CoreActorPoolTests` (2) and `EventJsonTests|HistoryTests|MailboxLogTests` (86). The ask, cancel, function-start, and test-command filter passed 76. No factual correction.
9. **Sibling.** Open PR 543 is the Outputs trim onto `staging`. This branch already contains that trim (`06475270`). Open PR 546 edits plan tickets and `map.md` only. No shared product files.
10. **Commits.** Each commit after `f32290ca` names one file.

## Stack

The PR base ref is still `cursor/remove-order-peer-and-loadsave-ff54`. Its tip `f32290ca` is not the squash commit `bff40213` on `staging`. The trees match, so this review does not need a rebase.
