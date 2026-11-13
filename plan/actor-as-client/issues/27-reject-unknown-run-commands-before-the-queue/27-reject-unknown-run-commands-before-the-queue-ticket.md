# 27 — Reject unknown Run commands before the queue
**Status**: coded
One job: The Browser rejects a `?` command that has no function start before that command is queued. Only `test`, `ai`, and a query reach the queue, and the user sees the failure. Run command in [Arch](../../arch.md) is this Run.

[ ] [26 — Retire the posted named start](../26-retire-the-posted-named-start/26-retire-the-posted-named-start-ticket.md) depends on this ticket. This ticket starts without waiting for 26.
[ ] After this ticket, `postChosenStart` has no production caller. [25 — Remove the coreMailboxStartActor door](../25-remove-the-coremailboxstartactor-door/25-remove-the-coremailboxstartactor-door-ticket.md) retires the door. 26 — Move the id-record starts onto function starts retires `pool.startActor` on the named path. [16 — Rename the client start request to ClientRequestActor](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) keeps `Order.ClientRequestActor`.
[ ] Pairing a stored `Order.StartActor` with its stop in `expandStored`, and posting nothing on a refused start, becomes moot once that door, that `pool.startActor` call, and `Order.StartActorDeprecated` are retired. 26 — Move the id-record starts onto function starts already requires that a refused start posts nothing.

Type:

[x] coding

Blockers:

[ ] None.

## Inputs

### Working

[x] `execRunOp` in `src/Client/Commands.fs` is the Run command. It calls `submitQuery`, then `CommandRequest.tryStart`. On `Ok` it calls `queueRun`.
[x] `submitQuery` calls `CommandRequest.tryQueryStart` and then `queueRun`. A query reaches the queue on that path.
[x] `queueRun` calls `RunLaunch.queueStart` in `src/Shared/RunLaunch.fs`. `queueStart` queues `posted "Exec" (EventBody.ActorStart request)`.
[x] `CommandRequest.tryStart` in `src/Shared/CommandRequest.fs` returns `Ok` for every `?` command `actorNodeOnOwnerPath` finds, including `?nope`. `CommandRequest.actorNameFromText` reads the name from that command node's text. `?test` and `?TEST` are `test`. `?ai` is `ai`. `?nope` is `nope`.
[x] On `Error msg`, `execRunOp` sets `lastCmdResult` to `CmdLastResult.Error (Some "Run", msg)` and returns `commitEffects`. That arm is the refusal. `AmbleRun.shouldExec` is false for a `?` line, so the arm shows the failure. The edit is `CommandRequest.tryStart`. `execRunOp` already uses this arm.
[x] `CmdLastResult.formatDisplay` renders `CmdLastResult.Error (Some "Run", msg)` as `Run: <msg>`. `setCmdLastResultDisplay` in `src/Client/Controller.fs` writes that text into `#cmd-last-result`. The missing-command message is `No command found`. This refusal uses `unknown actor`, the same words `failedMessage` returns for `StartError.UnknownActor`.
[x] `admittedQueue` in `src/Server/Core/CoreMailboxActors.fs` queues a posted start when the name is `test` or `ai`, or when `SearchActor.isQueryRequest` is true. Every other name calls `postChosenStart`.
[x] `RouteRegistration.CreateBoot` in `src/Server/RouteRegistration.fs` sets `Actors` to `[]`.
[x] `afterEditCommitThenTryStart` in `tests/Shared.Tests/RunEditCommitTests.fs` copies the `tryStart` arm. The new Run fact uses that helper.

### Reference

[x] Run command in Arch. `actorNameFromText : string -> string option`.

## Do not load

[x] `postChosenStart`, `dispatchStartActor`, `finishReadyStart`, `recordFailedStart`, and `expandStored`.
[x] `CoreMailboxApi.coreMailboxStartActor`. 25 — Remove the coreMailboxStartActor door owns that door.
[x] `pool.startActor`. 26 — Move the id-record starts onto function starts owns that call.
[x] `Order.ClientRequestActor`. 16 — Rename the client start request to ClientRequestActor keeps that case.
[x] The event shape inside `queueStart`. [24 — Move the remaining ActorStart writers onto StartActor](../24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md) owns that caller.
[x] A new `ActorFunction`.
[x] A session id.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `CommandRequest.tryStart`. Read the command name with `actorNameFromText`. Return `Ok` when the name is `test` or `ai`. Return `Error "unknown actor"` for every other `?` command, including `?nope`.
3. [x] Leave the `Error` arm of `execRunOp` as the Browser refusal. That arm sets `CmdLastResult.Error (Some "Run", msg)` and returns `commitEffects`.
4. [x] Leave `submitQuery` as the query path into `queueRun`.
5. [x] Add ``tryStart returns unknown actor for ?nope`` and ``tryStart returns Ok for ?ai`` in `tests/Shared.Tests/CommandRequestTests.fs`.
6. [x] Add ``?nope Run leaves the queue and shows the Run error`` in `tests/Shared.Tests/RunEditCommitTests.fs`. The helper returns `CmdLastResult.Error (Some "Run", "unknown actor")`. The pending list gains no `EventBody.ActorStart`.

## Outputs

[x] `src/Shared/CommandRequest.fs`
[x] `tests/Shared.Tests/CommandRequestTests.fs` `--filter` ``tryStart returns unknown actor for ?nope``
[x] `tests/Shared.Tests/CommandRequestTests.fs` `--filter` ``tryStart returns Ok for ?ai``
[x] `tests/Shared.Tests/RunEditCommitTests.fs` `--filter` ``?nope Run leaves the queue and shows the Run error``

## Stop when

[x] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when a `?` command other than `test` and `ai` reaches `queueRun`.
[ ] Stop when `submitQuery` changes.
[ ] Stop when the visible result is other than `CmdLastResult.Error (Some "Run", "unknown actor")`.
[ ] Stop when `execRunOp` needs an edit to show that result.
[ ] Stop when this ticket edits `postChosenStart`, the door, or `pool.startActor`.
[ ] Stop when this ticket adds an `ActorFunction` or a session id.
[ ] Stop when the change passes 2 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``tryStart returns unknown actor for ?nope``
  - `tests/Shared.Tests`: ``tryStart returns Ok for ?ai``
  - `tests/Shared.Tests`: ``?nope Run leaves the queue and shows the Run error``
[x] All executed tests pass.
[x] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
