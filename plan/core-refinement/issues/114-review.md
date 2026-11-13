# 114 — Remove CoreMsg.ActorStop

Ticket: [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md)

Spec: [core-refinement spec](../spec.md)

Arch: [core-refinement arch](../arch.md)

Behavior is `126b4c81`. This review adds no source edit. `origin/staging` at `80cad19c` is the merge base. Staging has no newer commit.

## 1. Standards

- `python scripts/standards-scan.py --diff origin/staging` printed no rows.
- No findings.

## 2. Spec

- The checked boxes in the ticket match the tree. `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`. `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure` match those three. The diff deletes the `ActorStop` arms only. `RecordSearchStop` still calls `CoreEventDispatch.actorStop`. `StartLoadSaveCommand` and `PostEvent` keep their arms.
- `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, `ActorChanges.actorStop`, and `CoreMailboxActors.dispatchActorStop` have left. `src/Server/SearchActor.fs` is absent from the diff. `EventBody.ActorStop` and `Notice.ActorStopped` stay in `src/Shared/History.fs`.
- On `80cad19c`, `CoreMailboxActors.dispatchActorStop` matched `caller.authority`. `Authority "Actor"` read `pool.getActorNodeId` of `caller.secret`. A live id logged through `CoreEventDispatch.actorStop` and called `pool.finish`. Any other authority replied `Error CoreAuth.refuse`. The live row stayed. The `stopped` list stayed empty.
- The three deleted tests called `CoreMailboxApi.actorStop` while that live row was present.

  - ``ActorStop with Test credentials is refused`` used `testCaller` (`Authority "Test"`).
  - ``ActorStop with Browser credentials is refused`` used a logged-in Browser caller.
  - ``ActorStop with Parse credentials is refused`` used `Authority "Parse"`.

- Each expected `Error CoreAuth.refuse`, `stopped.Count` 0, and the live secret still present.
- That authority match lived only on `CoreMsg.ActorStop`. `CoreMailboxApi.postNotice` takes a `Notice` and no `Caller`. `CoreMailboxLoop.logActorStopped` reads `CoreActorPool.trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, then `CoreActorPool.finish`. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
- A client `EventBody.ActorStop` still fails in `CoreMailboxApi` and in `CoreMailboxActors.dispatchPostEvent` with "ActorStop is not a client event type". ``ActorStop in a posted list applies nothing`` in `tests/Server.Tests/OrderedEventStreamTests.fs` covers that refusal for `testCaller`. The check reads the event body. The error is `CoreAuth.refuse` on the old stop message, and the client-event string on this path.
- After [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md), actor bodies return the `ActorResult`. `CoreDoor.actorStop` only filled `ActorChanges.actorStop`. No production caller invoked `CoreMailboxApi.actorStop`. A browser route builds no `CoreMsg.ActorStop`.
- Removing that door removes `CoreAuth.refuse` for a message with no remaining caller. A running actor still ends as one stop row with `Authority "Actor"` when the pool posts `Notice.ActorStopped`. A posted `ActorStop` body still gets the client-event refusal. The three tests have no remaining path. The deletion stands.
- Moved tests still assert the same rows.

  - ``ActorStopped Notice drops live row`` posts `Notice.ActorStopped` and asserts `finish` of the live secret and `ActorSucceeded`.
  - ``ActorStopped Notice is logged and drops live row`` asserts one stop of that focus and `Authority "Actor"`.
  - `postActorStop` posts the notice, reads the log, and `assertOneStop` checks one row, `Authority "Actor"`, and the result.
  - ``actorStop after cancel does not emit a root Focus ActorStop`` posts the notice after Cancel and expects 0 root stops and 1 focus stop.
  - ``mailbox appends StartActor and ActorStop in lifecycle order`` is unchanged, including `Authority "Actor"` and `ActorFailed ""`.

- ``function start yields its secret and a later stop is dropped`` and ``ActorStop after the pool row is finished is dropped`` used to expect `Error "authority dropped"` from a second `CoreMsg.ActorStop`. They now expect the log to keep one stop. That reply left with the message. The log row count stays one.
- ``runMixedHello`` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` starts `TestActor.functionActorAt` and calls no stop. The `ActorChanges` record in ``fetch completes before the workspace lock then the pull merges and marks`` is `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.

## 3. Arch

- The arch queue text names `CoreMailboxLoop.logActorStopped` and `CoreActorPool.postStopUnlessCancelled`. `CoreEventDispatch.actorStop` stays for that log write and for `RecordSearchStop`.
- No findings.

## 4. Tests

- `scripts/agent-test.sh --full` set `AMBIT_FULL_SUITE=1` and wrote `/tmp/ambit-full-suite-20261113-220539.log`. The log ends `full suite finished`.
- Server.Tests passed 775, failed 0, skipped 1 (``Change.upload structure persistence preserves existing file mtime``), total 776, duration 3 m.
- Shared.Tests passed 1946, failed 0, skipped 3 (``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, ``listForPush without git still returns walk``), total 1949, duration 30 s.
- The Server passed count is 3 below the 778 recorded for [117 — The pool posts Notice.ActorStopped when an Actor ends](117-review.md). Those 3 are the deleted credential tests.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope. The fixes are suggested.

- `doc/current/actors.md` still has an open item that `CoreMsg.ActorStop` stays until this ticket. The following checked item already names the pool post. Drop the open item.
- `doc/current/mailbox.md` still checks `CoreMsg.ActorStop` as the fast stop message on this loop. The present fast stop is `Notice.ActorStopped` through `CoreMailboxLoop.logActorStopped`.
- The Actor stop section of `plan/online-search/arch.md` still checks `CoreDoor.actorStop` as staying until this ticket. That symbol has left.
- [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) still checks `CoreDoor.actorStop` and `CoreMailboxActors.dispatchActorStop` as present. This ticket's overlap leaves that file for 24.
- `plan/core-refinement/project.md` describes 117 with `CoreMsg.ActorStop` staying until this ticket, and describes 114 without `Status` `coded`.
- `tests/Server.Tests/Issue41CoreMailboxTests.fs` says `functionActorAt` queues `actorStop` during the start call. `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` says `functionActorAt` stops itself and queues that stop during start. The pool posts `Notice.ActorStopped` after the body returns.
