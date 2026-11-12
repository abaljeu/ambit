STOP: the client cannot build an ActorOrder.

# 24 — Move the remaining ActorStart writers onto StartActor

Ticket: [[24-move-the-remaining-actorstart-writers-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

1. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0. HEAD is `staging` at `7324a2c8`, so that range is empty.
2. **Ticket diff.** The code on staging for this ticket is commit `6ba8174a`. Added lines in `src/Server/Core/CoreMailboxApi.fs` and `tests/Server.Tests/CoreMailboxDoorTests.fs` stay within 100 characters and use no tab. `affirmedStart` is 12 lines. `postEvent` is 16 lines.
3. **Judgement.** Duplicated Code. `expandStored` already returns a start and its stop only when the stop focus matches. `affirmedStart` matches that same pair again.

```
| EventBody.ActorStart request,
  EventBody.ActorStop(id, _)
    when id = request.focusId ->
    Ok stored
```

## 2. Spec

1. **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
2. **Process.** The ticket has five process steps. Steps 1 and 2 are checked. Steps 3, 4, and 5 are open. `dispatchStartActor`, `recordFailedStart`, and the `expandStored` pairing of `Order.StartActor` are not steps of this ticket. 27 — Reject unknown Run commands before the queue does not take those steps.
3. **Step 2.** `postEvent` returns the stored start when `storedEvents` returns that start and the `EventBody.ActorStop` for the same focus. A single stored event still returns that event. Any other list returns `Error "stored Event not found"`.
4. **Missing.** Step 3. `queueStart` in `src/Shared/RunLaunch.fs` still queues `posted "Exec" (EventBody.ActorStart request)`. `queueRun` in `src/Client/Commands.fs` calls that function. It does not queue `Order.StartActor` of `ActorOrder`.
5. **Stop.** `ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>` in `src/Shared/History.fs`. The client has no server function to put in an `ActorOrder`. Step 3 says stop and ask.
6. **Partial.** Step 4. `EventBody.ActorStart` and `(|ActorStart|_|)` are still in `src/Shared/History.fs`. Callers remain, so the delete condition is false.
7. **Missing.** The one job, and step 5. Code outside the `Order.StartActorDeprecated` case still builds or matches `EventBody.ActorStart`. Hits remain outside Outputs and outside the files 22 — Convert CoreMsg.PostEvent and 25 — Remove the coreMailboxStartActor door own:

- `src/Shared/ActorLive.fs`
- `src/Server/Core/CoreEventDispatch.fs`
- `tests/Shared.Tests/ActorLiveTests.fs`
- `tests/Shared.Tests/EventJsonTests.fs`
- `tests/Shared.Tests/EventTests.fs`
- `tests/Shared.Tests/SyncLogicTests.fs`
- `tests/Server.Tests/ActorOrderFromStartTests.fs`
- `tests/Server.Tests/ApiPostCommandTests.fs`
- `tests/Server.Tests/LoadSaveCommandTests.fs`
- `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
- `tests/Server.Tests/OrderedEventStreamTests.fs`
- `tests/Server.Tests/PersistHandlersRestoreTests.fs`
- `tests/Server.Tests/QueryActorTests.fs`

## 3. Arch

1. No findings.
2. The diff stays in `postEvent` in `src/Server/Core/CoreMailboxApi.fs`. Run command still calls `queueRun`, which calls `RunLaunch.queueStart`. Place actor order is unchanged.

## 4. Tests

1. **Suite.** `tests/Server.Tests` passed 778, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``. `tests/Shared.Tests` passed 1936, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
2. **check-test.** `scripts/check-test.sh 3018` printed `Permission denied`. The script has no execute bit. `bash scripts/check-test.sh 3018` printed `DONE` and `/tmp/ambit-full-suite-20261112-212457.log`.
3. **Outputs.** ``postEvent returns the stored start when that start stops at once`` posts a start, stops that actor, posts the same start again, and asserts the returned id and the `EventBody.ActorStart` body.

## 5. Miss

1. `queueStart` does not queue `Order.StartActor` of `ActorOrder`. The client cannot build an `ActorOrder`.
2. `EventBody.ActorStart` and `(|ActorStart|_|)` remain. The one job is unmet. Search hits remain outside Outputs and outside the files 22 — Convert CoreMsg.PostEvent and 25 — Remove the coreMailboxStartActor door own.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

1. **Rejected.** **Problem.** Pull request 595 says the sequence row for 26 — Move the id-record starts onto function starts says `finishReadyStart` logs `EventJson.stubActor`. **Fix.** `plan/actor-as-client/sequence.md` does not say that. `finishReadyStart` appends `EventBody.ActorStart`. That ticket already names this log.
2. **Accepted.** **Problem.** `dispatchStartActor`, `recordFailedStart`, and `expandStored` still build or match `EventBody.ActorStart`. This ticket's process no longer names them. 27 — Reject unknown Run commands before the queue lists them under Do not load. **Fix.** Put the logging step on the ticket that owns those functions, or put the step back on 24 — Move the remaining ActorStart writers onto StartActor.
