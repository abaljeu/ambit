# 15 — Delete the old Actor starts

Ticket: [[15-delete-the-old-actor-starts-ticket.md]]
Spec: `plan/actor-as-client/spec.md` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

1. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
2. **Pool literals.** **Judgement.** Shotgun Surgery, suppressed. Removing `registerPeer` and `startPeerActor` from `CoreActorPool` forces the same two fields out of each pool literal. [15 — Delete the old Actor starts](15-delete-the-old-actor-starts-ticket.md) names those test files in Outputs. Source: [[plan/context/SMELLS.md]]. The hunk is the deleted `registerPeer` and `startPeerActor` fields beside `register` in `tests/Server.Tests/CoreMailboxDoorTests.fs:238`.

## 2. Spec

1. **One job.** `CoreMsg` in `src/Server/Core/CoreMsg.fs:5` is `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`. `CoreMailboxApi.startPeerActor`, `CoreMailboxActors.dispatchStartPeerActor`, `CoreActorPool.startPeerActor`, `CoreActorPool.registerPeer`, `runStartPeerActor`, and `peerDefs` are gone. `CoreActorPool.startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs:240` returns `Ok None` for `LoadSavePath.Desk` and `LoadSavePath.Git` and leaves `LoadSaveContext` unread.
2. **Arms.** `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs:14` and `messageCaller` in `src/Server/Core/CoreMailboxLoop.fs:59` match the five remaining cases. `operationContext` in `src/Server/Core/CoreMailboxEvents.fs:38` and `replyFailure` in `src/Server/Core/CoreMailboxEvents.fs:52` match those same cases.
3. **Kept starts.** `CoreActorPool` in `src/Server/Core/CoreActorPool.fs:39` keeps `register`, `startActor`, and `startFunction`. `create` wires them in `src/Server/Core/CoreActorPool.fs:358`. `Order.StartActorDeprecated` stays in `src/Shared/History.fs:119`. The diff adds no `CoreMsg` case and no `Order` case.
4. **Git route.** `RouteRegistration.registerStateRoutes` in `src/Server/RouteRegistration.fs:334` sends `LoadSavePath.Desk` to `CoreMailboxApi.startLoadSaveCommand` and every other path to `GithubTransportActor.start`. The diff leaves that file as it is.
5. **Deleted facts.** ``plain Load of a Workspace still requires the git actor`` and ``explicit git Load of a File still requires the git actor`` are absent from `tests/Server.Tests/ParseThreadLoadTests.fs`. ``plain Load of a File or Directory ignores a Git path`` stays at `tests/Server.Tests/ParseThreadLoadTests.fs:317`.
6. **Search.** A search of `src/` and `tests/` for `startPeerActor` and `StartPeerActor` is empty. `registerPeer` remains the forbidden name at `tests/Server.Tests/AppGithubTransportBoundaryTests.fs:96`. `StartLoadSaveCommand` remains.

## 3. Arch

1. **Obsolescent peer start.** Pool start marks `startPeerActor` `[o]`, and flow path 7 Named start today marks `dispatchStartPeerActor` `[o]`. The diff deletes both. The module map interface for Pool start stays `startFunction`. The `[x]` Load and save path stays `GithubTransportActor.start` for Git and `CoreMailboxApi.startLoadSaveCommand` for Desk.

## 4. Tests

1. **Suite.** `bash scripts/check-test.sh 2992` printed `DONE` and `/tmp/ambit-full-suite-20261110-002858.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 756, failed 0, skipped 1. `tests/Shared.Tests` passed 1937, failed 0, skipped 3. `scripts/check-test.sh` is not executable. The bare path prints `Permission denied`.
2. **Mapped run.** `scripts/agent-test.sh` with no arguments passed 113, failed 0, skipped 0. The filter includes `Issue42PersistHandlersTests`. That stem matched no test.
3. **Named filter.** `scripts/agent-test.sh --filter ParseThreadLoadTests server` passed 23, failed 0, skipped 0.
4. **Issue 42 file.** `tests/Server.Tests/Issue42PersistHandlersTests.fs:20` has six facts. `tests/Server.Tests/Gambol.Server.Tests.fsproj` has no Compile item for that file. `dotnet test tests/Server.Tests --filter FullyQualifiedName~Issue42PersistHandlersTests` printed `No test matches`. That gap is older than this diff.

## 5. Miss

1. No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. **Problem.** `tests/Server.Tests/Issue42PersistHandlersTests.fs` is absent from `tests/Server.Tests/Gambol.Server.Tests.fsproj`, so its six facts do not run. The diff still updates the pool literal at `tests/Server.Tests/Issue42PersistHandlersTests.fs:100`. Accepted.
2. **Fix.** Add the Compile item, or drop the file from the ticket outputs if it is retired. The user confirms the fix.
