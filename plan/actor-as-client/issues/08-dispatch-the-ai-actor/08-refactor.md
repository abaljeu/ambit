# 08 — Start the AI Actor

Ticket: [[08-dispatch-the-ai-actor-ticket.md]]

Arch: [[plan/actor-as-client/arch.md]]

## Stage

Stage is refactor in [08 — Start the AI Actor](CONTEXT.md).

## 1. Pool constructor

1. **createWithInstall.** `create` and `createWithInstall` in `src/Server/Core/CoreActorPool.fs` both call `newPool`. `create` passes no install. `createWithInstall` passes the install. The record fields are set in `newPool`.
2. **Boot.** `createPublishing` in `src/Server/Core/CoreRuntime.fs` calls `create` when the boot install is absent and `createWithInstall` when the boot install is present.
3. **Ask harness.** `createHost` in `tests/Server.Tests/AskCancelHarness.fs` calls `createWithInstall`. `startAiRun` reads that pool's install and `sessionIdFor`.
4. **Fixed secret.** `poolForFixedSecret` in `tests/Server.Tests/TestBackend.fs` is `{ CoreActorPool.create () with ... }` for the shared test overrides.

## 2. Pool sites

1. **Lifecycle order.** ``mailbox appends ActorStart and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` calls `poolForFixedSecret`.
2. **History durability.** `stubPool` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` calls `poolForFixedSecret`.
3. **Persist restore.** ``ActorStart persists across File restart`` and `stubPool` in `tests/Server.Tests/PersistHandlersRestoreTests.fs` call `poolForFixedSecret`.
4. **Door actor stop.** ``CoreMailboxApi.actorStop with valid credential drops live row`` and ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` start from `poolForFixedSecret` and override `isLive`, `finish`, and, in the second fact, `getActorNodeId`.
5. **Recorded start.** `recordingPool` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts from `poolForFixedSecret` and overrides `startActor`, `isLive`, `finish`, and `getActorNodeId`.
6. **Live receipt.** ``ActorStop checks the live receipt once`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts from `poolForFixedSecret` and overrides `isLive`, `finish`, and `getActorNodeId`.

## 3. Admitted order

1. **postBuiltOrder.** `queueAdmittedTest` and `queueAdmittedAi` in `src/Server/Core/CoreMailboxActors.fs` call `postBuiltOrder`.
2. **whenAdmitted.** `postActorStart` calls `whenAdmitted` for `test` and for `ai`.

## 4. Replace post

1. **postReplace.** `postReplace` in `src/Server/RunAgentActor.fs` takes the actor changes and the ops. `postAdd` and `runBody` call that shape.

## 5. Suite

1. **Full suite.** `scripts/agent-test.sh --full` passed. Server: 759 passed, 1 skipped, 0 failed. Shared: 1937 passed, 3 skipped, 0 failed.

## 6. Scan

1. **Standards scan.** `python3 scripts/standards-scan.py --diff HEAD` prints one `MUTABLE` line: `let mutable model` in `newPool`. That cell is the pool model. Each measured binding in the diff is 40 lines or under. `newPool` is 38 lines.

## 7. Still open

1. **Boot records.** `fileRuntime` in `tests/Server.Tests/CoreRuntimeTests.fs`, `fileRuntime` in `tests/Server.Tests/CredentialedChangePostsTests.fs`, and `devRuntime` in `tests/Server.Tests/SessionTests.fs` build a `CoreBoot` with `aiInstall = None`. `CoreRuntime.create` reads that field.
2. **Door overrides.** ``Actor start rejects a secret the pool does not admit`` and ``Actor post with a secret the pool does not hold is dropped`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` already start from `CoreActorPool.create` and override `startActor` plus `admit` or `isLive`.
3. **Model cell.** The pool model stays a mutable cell. The scan names that line.
