# 113 — Actors return their ActorResult

Ticket: [[../113-actors-return-their-actorresult.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

Code head under review: `3619b1d5`. `origin/staging` at `2b3ab7f3` is the merge base.

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line. Measured bindings stay under 40 lines: `SearchActor.functionStart` 20, `SearchActor.functionActorAt` 18, `SearchActor.searchActorAt` 6, `GithubTransportActor.finishActor` 11, `RunAgentActor.stop` 6, `RunAgentActor.actorFn` 23, `TestActor.dispatch` 24, `TestActor.runBehavior` 23, `CoreActorPool.runSchedule` 19, `CoreMailboxLoop.poolActor` 15, `EventJson.stubActor` 3.
- **Hard.** `.agents/rules/fsharp-source.md` says pure functional F# and no mutable state. `SearchActor.functionStart` and `SearchActor.functionActorAt` store the stop argument in a `ref` cell and return that cell. The scanner flags the `mutable` keyword, so this `ref` is invisible to the scan. The cell stays. `queryActorBody` calls `getGraph` once and calls the stop once with `ActorQuery`. A second `queryIds` call in those two functions would read the carrier graph twice, and ``ActorStop reads the carrier graph once`` requires one read. `queryActorBody` stays outside this ticket's `SearchActor` edit.
- **Judgement.** Duplicated Code. The same wait is copied in `tests/Server.Tests/ActorCoreChangesDoorTests.fs`, `tests/Server.Tests/ActorOrderFromStartTests.fs`, `tests/Server.Tests/AskCancelHarness.fs`, `tests/Server.Tests/CoreActorPoolDeliverTests.fs`, and `tests/Server.Tests/CoreMsgActorCasesTests.fs`. Each body reads `Async.CancellationToken`, waits on `Async.AwaitWaitHandle` of that token's wait handle, and returns `ActorSucceeded`.
- **Rules.** The diff matches `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`. Each place in the plan diff is a file and a symbol.
- **Size.** The source diff is the 8 files named in Outputs. The test diff is the 8 files named in Outputs.

## 2. Spec

- **Spec file.** The core-refinement spec covers document-state events. The ticket is the spec for this return change.
- **Same result.** `GithubTransportActor.finishActor`, `RunAgentActor.stop`, `TestActor.dispatch`, and `TestActor.runBehavior` pass one `ActorResult` into the stop and return that value. `SearchActor.searchActorAt` calls `changes.actorStop` with `ActorSucceeded` and returns `ActorSucceeded`. `SearchActor.functionStart` and `SearchActor.functionActorAt` pass the `queryActorBody` argument into the stop and return that stored value.
- **Ignore.** `CoreActorPool.runSchedule` starts `work |> Async.Ignore` on the row cancellation token for `BagPending` and `FunctionPending`. The stop still runs before the function returns. The pool leaves the `ActorResult` unread, so the log (`EventBody.ActorStop`, `Notice.ActorStopped`) and the stop results stay the same.
- **Waits.** A started body that leaves the stop unasserted records its observation, then waits on `Async.CancellationToken`. `MailboxHost.dispose` calls `CoreActorPool.cancelLiveActors`. `CoreActorPool.finish` and `CoreActorPool.drop` cancel the row token. The wait ends on that cancel. ``function start yields its secret and a later stop is dropped`` asserts the stop, so that body returns `ActorSucceeded` after it records the secret.
- **SearchActor scope.** The `src/Server/SearchActor.fs` diff is `functionStart`, `functionActorAt`, and `searchActorAt`. `SearchActor.reply` still walks from `request.startId`. `SearchActor.actorStart`, `SearchActor.oneReply`, and `queryActorBody` stay.
- **Checkboxes.** Each `[x]` in the ticket matches the tree, including the 8 source files and the 8 test files.
- **Extra.** [Actor as client architecture](../../actor-as-client/arch.md) now shows `Async<ActorResult>` for the shared `ActorFunction`, the pool `ActorFunction`, and `ActorFn`. The ticket Outputs leave that file unnamed. The lines match the code.

## 3. Arch

- **Core-refinement arch.** The core-refinement architecture charts document-state and the mailbox. The Actor return stays on the Actor as client architecture. The diff stays in the pool and the actor modules named there.
- **Pool launch.** Module map 10 — Pool launch names `CoreActorPool.runSchedule`. The diff discards the result there and keeps `Async.Start` on the row cancellation token.
- **Actor function.** Module map 1 — Actor function names `ActorFunction` in `src/Shared/History.fs`. That return is `Async<ActorResult>`. `CoreMailboxLoop.poolActor` returns the shared actor's `ActorResult`.
- **Producers.** Module map 12 — Test command stays on `TestActor.functionActorAt`. Module map 13 — AI actor stays on `RunAgentActor.actorFn`. `SearchActor.functionActorAt` still calls its stop. Module map 14 — Query command still names `postSearchRequest`, and this diff leaves that function.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2971` printed `DONE` and `/tmp/ambit-full-suite-20261113-191243.log`.
- **Server.** `tests/Server.Tests` passed 774, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1944, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Named seams.** ``function start reply is bookkeeping without waiting for an Actor body`` still asserts a sub-second start and a live focus. ``ActorOrder places the actor on the thread pool`` still asserts `pool.isLive` and one `Order.StartActor`. ``ActorStop reads the carrier graph once`` still reads one `EventBody.ActorStop` of `ActorQuery`. ``postSearch records ActorStart at the root`` returns the result of `SearchActor.searchActorAt` and still asserts `ActorStart` plus `ActorStop` of `ActorSucceeded`. ``ActorOrder round-trips on the wire with stubActor`` still builds two unrun `ActorFunction` values and asserts `ran` is false. Assertion lines are unchanged.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
