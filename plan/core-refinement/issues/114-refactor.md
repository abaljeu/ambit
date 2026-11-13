Check: pass

# 114 — Remove CoreMsg.ActorStop

Ticket: [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md)

Review: [114 — Remove CoreMsg.ActorStop](114-review.md)

Behavior is `126b4c81`. `origin/staging` at `80cad19c` is the merge base. That commit is an ancestor of this branch.

## 1. Edits

1. **Secret lookup.** `CoreActorPool` keeps `trySecretForActorNode`. `getActorNodeId` and `actorNodeOf` have left. `CoreMailboxLoop.logActorStopped` reads the secret for the focus and logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`. The stored authority is `Authority "Actor"`.
2. **Fixed pool.** `poolForFixedSecret` in `tests/Server.Tests/TestBackend.fs` sets `trySecretForActorNode` to a function that returns `None`.
3. **Recording pool.** `recordingPool` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` sets `trySecretForActorNode` from the head of the live list.
4. **Door secret.** ``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row`` share `secretWhileLive`.

## 2. Kept path

1. **Message.** `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`. `CoreMailboxLoop.runMsg` matches `RecordSearchStop` and calls `CoreEventDispatch.actorStop`.
2. **Notice.** `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped`. `CoreMailboxLoop.logActorStopped` stores one stop row. The authority is `Authority "Actor"`.
3. **Refusal.** `CoreAuth.refuse` stays for admission. ``function start with blank Authority does not hand off`` still expects that error. A `Notice.ActorStopped` carries no caller.
4. **Names.** These tests post `Notice.ActorStopped` and keep their names:

   - ``ActorStop ActorSucceeded drops live row without waiting``
   - ``ActorStop ActorFailed drops live row and records terminal``
   - ``ActorStop after the pool row is finished is dropped``
   - ``actorStop after cancel does not emit a root Focus ActorStop``

   Each stored stop is `EventBody.ActorStop` with `Authority "Actor"`.

## 3. Suite

1. **Command.** `scripts/agent-test.sh --full` prints the pid, then the log path. `AMBIT_FULL_SUITE=1` is set inside that command. `bash scripts/check-test.sh 7230` printed `DONE` and `/tmp/ambit-full-suite-20261113-221939.log`. The log ends `full suite finished`.
2. **Server.** `tests/Server.Tests` passed 775, skipped 1, failed 0, total 776, duration 2 m 56 s. The skip is ``Change.upload structure persistence preserves existing file mtime``.
3. **Shared.** `tests/Shared.Tests` passed 1946, skipped 3, failed 0, total 1949, duration 29 s. The skips are

   - ``reuses existing owned path without duplicating``
   - ``planServerFilePresentOps transitions only matching absent files``
   - ``listForPush without git still returns walk``

## 4. Scan

1. **Standards scan.** `python3 scripts/standards-scan.py --diff origin/staging` exits 0.
2. **secretWhileLive.** 10 lines (71–80) in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
