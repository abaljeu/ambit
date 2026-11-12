Check: pass

# 24 — Move the remaining ActorStart writers onto StartActor

Ticket: [[24-move-the-remaining-actorstart-writers-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Range.** The diff is this review, the ticket, `CONTEXT.md` in this folder, and the sequence row for 24 — Move the remaining ActorStart writers onto StartActor.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **One job.** The Browser queues an `ActorStart` request and posts it. Its event-log copy records the `ActorOrder` the Server returns. The Browser never builds an `ActorOrder`. `EventBody.ActorStart` and `(|ActorStart|_|)` stay.
- **Step 2.** `postEvent` in `src/Server/Core/CoreMailboxApi.fs` returns the stored start when `storedEvents` returns that start and the `EventBody.ActorStop` for the same focus. ``postEvent returns the stored start when that start stops at once`` posts that pair and asserts the returned id and the `EventBody.ActorStart` body.
- **Queue.** `queueStart` in `src/Shared/RunLaunch.fs` queues `posted "Exec" (EventBody.ActorStart request)`. `queueRun` in `src/Client/Commands.fs` calls `queueStart`. `SyncPlanner.queueWithOpenBatch` appends that event to `pending` and emits `SubmitPendingBatch`.
- **Server order.** `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` sends `test`, `ai`, and a query through `postActorOrder`. `replyAffirmedStart` in `src/Server/Core/CoreMailboxLoop.fs` appends `EventBody.Order` of `Order.StartActor` and replies that event id. `postEvents` reads that stored event into the ack.
- **Client copy.** `BootCache.acceptedForLog` returns the confirmed events when that list has an event. The `SubmitResponse` arm in `src/Client/App.fs` appends those events when the pending count drops and the sync state is not `ServerRejected`.
- **Wire order.** `EventJson.decodeActorOrder` in `src/Shared/EventJson.fs` fills `actor` with `stubActor` when the client reads the returned order.
- **Client builder.** No file under `src/Client` constructs an `ActorOrder`.
- **Step 4.** `EventBody.ActorStart` stays in `src/Shared/History.fs`. It builds `EventBody.Order` of `Order.StartActorDeprecated`. It is the client request.
- **Step 5.** `(|ActorStart|_|)` stays in `src/Shared/History.fs`. It matches that same case.

## 3. Arch

- **Run command.** `execRunOp` calls `queueRun`, which calls `RunLaunch.queueStart`. That matches the checked Run command path. The diff leaves each story path and each module-map interface as they are.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 3226` printed `DONE` and `/tmp/ambit-full-suite-20261112-224752.log`. That server run passed 777, failed 1, skipped 1. ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` failed on order, and the script stopped before Shared.
- **Server.** A second `tests/Server.Tests` run with `AMBIT_FULL_SUITE=1` passed 778, failed 0, skipped 1. The log is `/tmp/ambit-server-suite-20261112-225754.log`. The same DbAgent test passed in that run. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` with `AMBIT_FULL_SUITE=1` passed 1936, failed 0, skipped 3. The log is `/tmp/ambit-shared-suite-20261112-225406.log`. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Outputs.** ``postEvent returns the stored start when that start stops at once`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts a start, stops that actor, posts the same start again, and asserts the returned id and the `EventBody.ActorStart` body.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Rejected.** **Problem.** The sequence row for 26 — Move the id-record starts onto function starts says `finishReadyStart` logs `EventJson.stubActor`. **Fix.** That row is a checkbox and a link. `finishReadyStart` appends `EventBody.ActorStart`. 26 — Move the id-record starts onto function starts already names that log.
- **Rejected.** **Problem.** `dispatchStartActor`, `recordFailedStart`, and `expandStored` still match `EventBody.ActorStart`, and the process of 24 — Move the remaining ActorStart writers onto StartActor does not name them. **Fix.** Leave those matches. 26 — Move the id-record starts onto function starts names the `finishReadyStart` log. 27 — Reject unknown Run commands before the queue lists those functions under Do not load. This ticket keeps `EventBody.ActorStart` as the client request.
