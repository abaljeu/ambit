Check: pass

# 24 — Remove CoreMsg.RecordSearchStop

Ticket: [[24-remove-coremsg-recordsearchstop.md]]

Spec: [[../spec.md]]

Arch: [[../arch.md]]

The source under review is commit `7255428a`. `origin/staging` is `254099fb`. The merge base is `561a3c9f`. This file is that review.

## 1. Standards

- `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0.
- The added F# lines are the `recordStop` closures in `RouteRegistration.searchDoor` and in ``postSearch records ActorStart at the root``. Each line is under 100 characters. Those lines use spaces, bind no `mutable` value, and add no public name.
- The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/refer-by-name.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/core-api.md`.

## 2. Spec

- **One job.** `CoreMsg` is `StartLoadSaveCommand` and `PostEvent`. A finished search is `Notice.ActorStopped` of the workspace root and `ActorSucceeded`. `CoreMailboxApi.recordSearchStart` places `Order.StartActor` for the actor name `search`.
- **Notice cases.** `Notice` in `src/Shared/History.fs` keeps `Notice.ActorStopped` as the stop case.
- **One stop.** `Api.searchReply` calls `door.recordStart` and returns `SearchActor.reply`. `scheduleActorOrder` admits that function once and schedules it once. `SearchActor.searchActorAt` returns `ActorSucceeded`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` of `row.actorNodeId` when the row token is open. `CoreMailboxLoop.logActorStopped` stores that notice through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`. That caller has `Authority "Actor"`. `SearchActor.actorStart` sets `focusId` to `graph.root`, and `actorNodeIdOf` is `ActorOrderFromStart.focusIdOf`.
- **Door.** `RouteRegistration.searchDoor.recordStop` is `fun _ -> async.Return ()`. `Api.SearchActorDoor` keeps the field `recordStop`.

## 3. Arch

- **Actor stop.** Module 19 **Actor stop** names `CoreActorPool.postStopUnlessCancelled` and `CoreMailboxLoop.logActorStopped`. The running search uses that path. `SearchActor.searchActorAt` returns `ActorSucceeded`, and the pool posts the notice.
- **Start.** `CoreMailboxApi.recordSearchStart` places `Order.StartActor` for the actor name `search`. The diff leaves that function in place.

## 4. Tests

- **Suite.** `scripts/agent-test.sh --full` ran with `DOTNET_ThreadPool_ForceMinWorkerThreads` and `COMPlus_ThreadPool_ForceMinWorkerThreads` set to 64. `bash scripts/check-test.sh 3003` printed `DONE` and `/tmp/ambit-full-suite-20261115-154528.log`. The log ends `full suite finished`.
- **Server.** Server.Tests passed 775, failed 0, skipped 1, total 776. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** Shared.Tests passed 1946, failed 0, skipped 3, total 1949. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Named test.** ``postSearch records ActorStart at the root`` is in that Server run. Its `recordStop` closure returns unit. After the actor body returns, `waitForNotices` requires `EventBody.ActorStop` of `graph.root` with `ActorSucceeded`. That pattern is `Notice.ActorStopped`. `hasStartActor` requires `Order.StartActor` for the name `search` at `graph.root`.
- **Count.** `List.exists` accepts a log that contains that stop row. The schedule posts one `Notice.ActorStopped`.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope. The fixes are suggested.

- **Accepted.** **Problem.** The arch section **Search event record** lists `recordSearchStop` and says that function commits `ActorStop`. The **Actor stop** checkbox says `CoreMsg.RecordSearchStop` stores a second stop. Module map 10 **Search event record** lists `recordSearchStop` on `CoreMailboxApi`. **Fix.** Name the search stop as the one `Notice.ActorStopped` from `CoreActorPool.postStopUnlessCancelled`, and drop `recordSearchStop` from those interface lines.
- **Accepted.** **Problem.** Spec item 12 **Search Actor door** says the start mailbox case is `RecordSearchStart`, that `recordStop` is a queue message, that the mailbox case is `RecordSearchStop`, and that the queue puts `ActorStop` on the event source. **Fix.** Say `recordStart` places `Order.StartActor` for the actor `search`, that `recordStop` returns unit, and that the stop row is `Notice.ActorStopped` from the pool post.
- **Accepted.** **Problem.** [Online search](../project.md) says 24 — Remove CoreMsg.RecordSearchStop is `defined`. The ticket **Status** is `coded`. **Fix.** Say that ticket is `coded` on the frontier line.
- **Accepted.** **Problem.** [Core refinement](../../core-refinement/project.md) says `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`, in the 2026-11-13 note on [114 — Remove CoreMsg.ActorStop](../../core-refinement/issues/114-remove-coremsg-actorstop.md) and in that ticket's list entry, which also says 24 — Remove CoreMsg.RecordSearchStop is next. **Fix.** Say `CoreMsg` is `StartLoadSaveCommand` and `PostEvent`, and that 24 — Remove CoreMsg.RecordSearchStop is `coded`.
- **Accepted.** **Problem.** [Search Actor](../../doc/current/search-actor.md) section **Uses** says `CoreMsg` has `RecordSearchStop`, and that `CoreMailboxApi.recordSearchStop` stores a second stop through `CoreMailboxLoop.runMsg`. The open **Stop** item says `searchDoor.recordStop` becomes a function that returns unit. **Fix.** Say `CoreMsg` is the two remaining cases, that the stop row is the one pool notice, and check that open item.
- **Accepted.** **Problem.** [Message shape](../../core-refinement/cqrs-messages.md) says `CoreMsg` includes `RecordSearchStop`, and that this case logs a second stop, in section 1 **Orders**, section 3 **StartActor**, and the `ActorStopped` row. **Fix.** Say `CoreMsg` is `StartLoadSaveCommand` and `PostEvent`, and that the search stop is the one pool notice.
