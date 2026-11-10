# 09 — Start the query command

Ticket: [[09-dispatch-the-query-command-ticket.md]]
Spec: [[plan/actor-as-client/spec.md]] is absent. This project has no spec.md. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

1. **Scan.** `python scripts/standards-scan.py --diff origin/staging` exited with `python: command not found`. `python3 scripts/standards-scan.py --diff origin/staging` printed two `MUTABLE` lines in `tests/Server.Tests/SearchActorTests.fs`, line 254 `let mutable started = false` and line 255 `let mutable stopped = false`. The measure list shows `queueAdmittedQuery` at 14 lines, `functionActorAt` at 12 lines, and `searchActorAt` at 6 lines. The printout is those two lines and those three sizes.
2. **Mutable flags.** **Hard.** [fsharp-source](.agents/rules/fsharp-source.md) says "Don't use mutable." The sentence "This rule does not apply to tests" follows the 800-line file rule. ``postSearch records ActorStart at the root`` adds `let mutable started` and `let mutable stopped`.
3. **Search wait.** **Judgement.** Overcomplication. The same fact waits with `DateTime.UtcNow.AddSeconds 2.0` and `Task.Delay`. The hunk is `do! System.Threading.Tasks.Task.Delay 10`. Source: [[plan/context/SMELLS.md]].
4. **Equals-line name.** **Judgement.** Mysterious Name. The fact ``equals-line ActorStart does not run the query function`` requires an `ActorQuery` stop. The hunk is `Assert.True(stopped.IsSome, "ActorQuery missing")`. Same smell source.
5. **Refusal name.** **Judgement.** Mysterious Name. The fact ``query start error stores ActorStart and ActorFailed and keeps the edit`` requires the refusal string and the edit text. The hunk is `Assert.Contains("actor node already has a live Actor", err)`. Same smell source.

## 2. Spec

1. **Query Run.** `postActorStart` sends a query Run through `whenAdmitted` and `queueAdmittedQuery`. `queueAdmittedQuery` builds `ActorOrder` with `SearchActor.functionActorAt`, `actorName` `query`, and `actorNodeId` `ActorStart.focusId`, then calls `postBuiltOrder`. `startFunction` stays `CoreActorPool.startFunction` through `scheduleActorOrder`. `startChosen` stays `context.pool.startActor`.
2. **Start comment.** **Wrong.** The comment on `startChosen` in `src/Server/Core/CoreMailboxActors.fs` says "Query and named starts both use the ID-bag start." A query Run goes through `queueAdmittedQuery`. The diff leaves that comment in place.
3. **Find and Move.** `recordSearchStart` takes the search `ActorFunction`, builds the order with `ActorOrderFromStart.orderFromStart`, sets `actorName` to `search` and `actorNodeId` to `ActorStart.focusId`, and calls `MailboxHost.postActorOrder`. The function names no `SearchActor` symbol. `replyActorOrder` calls `admitCaller` before it appends the order. `searchActorAt` calls `changes.actorStop` with `ActorSucceeded`.
4. **Session.** `FunctionRun`, `ActorOrder`, and the `ActorFunction` closures stay free of a session id. `FunctionRun` stays `getGraph` and `secret`.
5. **Query body.** `functionActorAt` takes `runDefinitionId` and `focusId` and returns the shared `ActorFunction`. The body posts `ActorQuery` from `queryIds`, then `changes.actorStop`. `functionStart` stays for the pool tests.
6. **Search reply.** On the success path, `Api.searchReply` calls `recordStart` and then returns `SearchActor.reply`.
7. **Boot value.** `AiInstall` gains `search`. `RouteRegistration.CreateBoot` sets `search` to `SearchActor.searchActorAt`. `searchDoor` reads `persistence.Core.pool.aiInstall` and passes `install.search` into `recordSearchStart`. `AskCancelHarness` sets `search` on its `AiInstall` literal.
8. **Missing install.** **Extra.** `searchDoor` returns `Error "search actor is not installed"` when `aiInstall` is `None`. The hunk is `async.Return (Error "search actor is not installed")`.
9. **Deleted case.** The diff deletes `CoreMsg.RecordSearchStart`, the `runMsg` arm, the `messageCaller` arm, the `operationContext` arm, the `replyFailure` arm, and `CoreEventDispatch.actorStart`.
10. **Order cases.** `Order` stays the cases in `src/Shared/History.fs`. The new orders use `Order.StartActor`.
11. **Refused start.** **Extra.** `replyActorOrder` appends `Order.StartActor` before `scheduleActorOrder`. A second query on a live actor node returns the refusal from `admitFunctionStart` and leaves that order in the log. The hunk is `let body = EventBody.Order(Order.StartActor order)`.

## 3. Arch

1. **Posted run.** The [x] line matches `queueAdmittedQuery` in `src/Server/Core/CoreMailboxActors.fs`. The query Run calls `SearchActor.isQueryRequest`, applies `SearchActor.functionActorAt`, sets `actorName` to `query`, and reaches `MailboxHost.postActorOrder`.
2. **Query command.** The [x] line matches `recordSearchStart` and `AiInstall.search` from `RouteRegistration.CreateBoot`. The [ ] line for Curried actor stays open.
3. **Query start today.** The [x] line matches `postActorStart` for a query Run and `startChosen` for a named start.
4. **Module map.** Item 14, Query command, marks Post actor order [x]. `postSearchRequest` reaches `MailboxHost.postActorOrder` through `recordSearchStart`.

## 4. Tests

1. **Suite.** `scripts/check-test.sh 2928` printed `Permission denied` and exited 126. `bash scripts/check-test.sh 2928` printed `DONE` and `/tmp/ambit-full-suite-20261110-180603.log`. The log ends with `full suite finished`. `tests/Server.Tests` passed 763, failed 0, skipped 1. The skip is `DocumentPersistenceTests.Change.upload structure persistence preserves existing file mtime`. `tests/Shared.Tests` passed 1937, failed 0, skipped 3. The skips are `WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`, and `WorkspaceLocalInventoryTests.listForPush without git still returns walk`.
2. **Equals line.** ``equals-line ActorStart does not run the query function`` exists. `waitStopEvent` accepts `ActorStop` of `ActorQuery` at the line id. The fact also requires `Order.StartActor` with `actorName` `query` and `actorNodeId` equal to that line id.
3. **Live actor.** ``query start error stores ActorStart and ActorFailed and keeps the edit`` exists. The fact requires the text `actor node already has a live Actor`, which is the string from `admitFunctionStart`, and it requires the node text to stay the equals line.
4. **Find and Move.** ``postSearch records ActorStart at the root`` exists. The fact requires `Order.StartActor` with `actorName` `search` and `actorNodeId` equal to the root, and `ActorStop` of `ActorSucceeded` at the root.
5. **Pool start.** The diff leaves `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` unchanged. The full suite passed, so the `startFunction` facts in that file passed.

## 5. Miss

1. **Start comment.** **Wrong.** The comment on `startChosen` says a query Run uses the ID-bag start. The diff sends a query Run through `queueAdmittedQuery`. This returns the ticket to build.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. No findings.
