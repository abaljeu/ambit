# 117 — The pool posts Notice.ActorStopped when an Actor ends

Ticket: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)

`CoreActorPool.runSchedule` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` of `row.actorNodeId` and that result through `CoreMailboxApi.postNotice` on `coreChanges.host`. A cancelled row posts nothing.

`CoreMailboxLoop.logActorStopped` handles that notice. A secret from `CoreActorPool.trySecretForActorNode` logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, then `CoreActorPool.finish`. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing. Every other notice stays on `CoreMailboxLoop.logPostedNotice`: `CoreEventDispatch.logNotice`, then `CoreMailboxLoop.applyNotice`. `applyNotice` stays `Ok ()` for `Notice.ActorStopped`.

The Actor bodies return the result. `RunAgentActor.stop` has left. `SearchActor.actorCaller` has left with the stop calls. The `SearchActor` edits are

- `queryActorBody`
- `functionStart`
- `functionActorAt`
- `searchActorAt`

``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged, including `Authority "Actor"` and the stop body.

``function start yields its secret and a later stop is dropped`` waits with `TestBackend.waitCancelled`. The explicit stop still drops the live row. A cancelled body posts nothing, so that stop stays the one row.

`origin/staging` at `4273ef22` is the branch base.

Check: pass

- `scripts/agent-test.sh` with no arguments passed Server 57. Failed 0. Shared had no mapped files.
- `scripts/agent-test.sh --filter` for the five named tests passed inside the Server run of 6. Failed 0. The sixth name in that run was ``postSearch records ActorStart at the root``.
- ``function start yields its secret and a later stop is dropped`` passed 1. Failed 0. Outputs names this wait. The ticket's Check list leaves it unnamed.
- `scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1` passed Server 776, skipped 1, and Shared 1944, skipped 3. Failed 0.
