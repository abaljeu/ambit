# 114 — Remove CoreMsg.ActorStop

Ticket: [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md)

`CoreMsg.ActorStop` has left. The stop that remains is `Notice.ActorStopped`, posted by the pool through `CoreActorPool.postStopUnlessCancelled` and stored by `CoreMailboxLoop.logActorStopped`.

These symbols have left:

- `CoreMsg.ActorStop` in `src/Server/Core/CoreMsg.fs`
- the `ActorStop` arms of `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`
- `CoreMailboxApi.actorStop`
- `CoreDoor.actorStop`
- `ActorChanges.actorStop`
- `CoreMailboxActors.dispatchActorStop`

`CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`. `CoreMailboxLoop.runMsg` still matches `RecordSearchStop` and calls `CoreEventDispatch.actorStop`. `EventBody.ActorStop` and `Notice.ActorStopped` stay. `src/Server/SearchActor.fs` stays.

``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row`` post `Notice.ActorStopped` with `CoreMailboxApi.postNotice`. Each recording pool's `trySecretForActorNode` returns the live secret. The logged stop row's authority is `Authority "Actor"`.

`postActorStop` posts that notice, then reads the log. ``actorStop after cancel does not emit a root Focus ActorStop`` posts that notice after the Cancel. The log holds one stop.

``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused`` have left.

``ActorStopped Notice looks up the live secret once`` counts one `trySecretForActorNode` call. ``function start yields its secret and a later stop is dropped`` and ``ActorStop after the pool row is finished is dropped`` keep one stop row after the second notice.

The `ActorChanges` record in ``fetch completes before the workspace lock then the pull merges and marks`` is `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.

``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged, including `Authority "Actor"` and the stop body.

`origin/staging` at `80cad19c` is the branch base.

Check: pass

- `scripts/agent-test.sh` with no arguments passed Server 66 and Shared 61. Failed 0.
- `scripts/agent-test.sh --filter` for the six named tests passed inside the Server run of 18. Failed 0.
- `scripts/agent-test.sh --full` with `AMBIT_FULL_SUITE=1` passed Server 775, skipped 1, and Shared 1946, skipped 3. Failed 0.
