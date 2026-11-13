Check: pass

# 113 — Actors return their ActorResult

Ticket: [113 — Actors return their ActorResult](../113-actors-return-their-actorresult.md)

Review: [113 — Actors return their ActorResult](113-review.md)

Code commit: `70305ece`. `origin/staging` at `2b3ab7f3` is the merge base.

## 1. Query result

1. **queryActorBody.** `queryActorBody` in `src/Server/SearchActor.fs` returns `Async<ActorResult>`. It calls `queryIds` once, passes `ActorQuery` to the stop, and returns that `ActorResult`.
2. **functionStart.** `SearchActor.functionStart` passes `CoreDoor.actorStop` and returns that async.
3. **functionActorAt.** `SearchActor.functionActorAt` passes `ActorChanges.actorStop` and returns that async.
4. **searchActorAt.** `SearchActor.searchActorAt` calls `changes.actorStop` with `ActorSucceeded` and returns `ActorSucceeded`.
5. **One read.** ``ActorStop reads the carrier graph once`` in `tests/Server.Tests/QueryActorTests.fs` still reads one `EventBody.ActorStop` of `ActorQuery`.

## 2. Shared wait

1. **waitCancelled.** `waitCancelled` in `tests/Server.Tests/TestBackend.fs` reads `Async.CancellationToken`, waits on `Async.AwaitWaitHandle` of that token's wait handle, and returns `ActorSucceeded`.
2. **Call sites.** These files call `waitCancelled`:

   - `tests/Server.Tests/ActorCoreChangesDoorTests.fs`
   - `tests/Server.Tests/ActorOrderFromStartTests.fs`
   - `tests/Server.Tests/AskCancelHarness.fs`
   - `tests/Server.Tests/CoreActorPoolDeliverTests.fs`
   - `tests/Server.Tests/CoreMsgActorCasesTests.fs`

## 3. Suite

1. **Command.** `scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1` printed `3703` and `/tmp/ambit-full-suite-20261113-192523.log`. `bash scripts/check-test.sh 3703` printed `DONE`. The log ends `full suite finished`.
2. **Server.** `tests/Server.Tests` passed 774, skipped 1, failed 0, total 775. The skip is ``Change.upload structure persistence preserves existing file mtime``.
3. **Shared.** `tests/Shared.Tests` passed 1944, skipped 3, failed 0, total 1947. The skips are

   - ``reuses existing owned path without duplicating``
   - ``planServerFilePresentOps transitions only matching absent files``
   - ``listForPush without git still returns walk``

## 4. Scan

1. **Standards scan.** `python3 scripts/standards-scan.py --diff origin/staging` exits 0.
2. **queryActorBody.** 11 lines (72–82).
3. **functionStart.** 16 lines (85–100).
4. **functionActorAt.** 10 lines (102–111).
5. **waitCancelled.** 7 lines (515–521).
