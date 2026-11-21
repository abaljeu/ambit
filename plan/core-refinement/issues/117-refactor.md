Check: pass

# 117 — The pool posts Notice.ActorStopped when an Actor ends

Ticket: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)

Review: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-review.md)

Behavior is `5397c862`. `origin/staging` at `9c9e1aac` is in this branch.

## 1. Edits

1. **finishActor.** `GithubTransportActor.finishActor` takes the work async and returns `actorOutcome`. `actorFunction` passes that body.
2. **Search stop check.** ``postSearch records ActorStart at the root`` uses one `rootSucceeded` check for the wait and the assert.
3. **History read.** ``an Actor that returns logs Notice.ActorStopped`` and ``ActorStopped for an idle focus logs nothing`` read the log through `eventPast`.
4. **Lock row.** ``function start exposes Graph lockPresent via getState`` and ``GetState stamps lockPresent from the live table after a function start`` say the pool posts `ActorStopped` and `finish` drops the row. The stub `finish` leaves the row so getState can stamp it.

## 2. Kept path

1. **Pool.** `CoreActorPool.runReturned` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` unless the row token is cancelled.
2. **Mailbox.** `CoreMailboxLoop.logActorStopped` stores the stop through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`. The stored authority is `Authority "Actor"`. Every other notice stays on `CoreMailboxLoop.logPostedNotice`.
3. **Bodies.** These functions return the `ActorResult`:

   - `RunAgentActor.actorFn`
   - `SearchActor.queryActorBody`
   - `SearchActor.functionStart`
   - `SearchActor.functionActorAt`
   - `SearchActor.searchActorAt`
   - `TestActor.dispatch`
   - `TestActor.runBehavior`

## 3. Suite

1. **Command.** `scripts/agent-test.sh --full` prints the pid, then the log path. `AMBIT_FULL_SUITE=1` is set inside that command. `bash scripts/check-test.sh 6780` printed `DONE` and `/tmp/ambit-full-suite-20261113-211129.log`. The log ends `full suite finished`.
2. **Server.** `tests/Server.Tests` passed 778, skipped 1, failed 0, total 779, duration 3 m 3 s. The skip is ``Change.upload structure persistence preserves existing file mtime``.
3. **Shared.** `tests/Shared.Tests` passed 1946, skipped 3, failed 0, total 1949, duration 26 s. The skips are

   - ``reuses existing owned path without duplicating``
   - ``planServerFilePresentOps transitions only matching absent files``
   - ``listForPush without git still returns walk``

4. **Earlier process.** Process `3040` waited in ``a request uses its own graph when the holder's graph disagrees`` in `tests/Server.Tests/NodeLockPoolTests.fs`. That process was stopped. The recorded run is `6780`.

## 4. Scan

1. **Standards scan.** `python3 scripts/standards-scan.py --diff origin/staging` exits 0.
2. **postStopUnlessCancelled.** 12 lines (266–277).
3. **runReturned.** 10 lines (278–287).
4. **logActorStopped.** 24 lines (339–362).
5. **logPostedNotice.** 15 lines (363–377).
