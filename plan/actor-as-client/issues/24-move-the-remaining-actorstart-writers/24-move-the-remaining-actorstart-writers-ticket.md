# 24 — Move the remaining ActorStart writers onto StartActor
**Status**: defined
One job: No code outside the `Order.StartActorDeprecated` case itself builds or matches `EventBody.ActorStart`. Each writer posts `Order.StartActor` of `ActorOrder`, and each reader reads that case.

[ ] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) depends on this ticket. 16 deletes the case once nothing here writes or reads it.
[ ] [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) covers the `dispatchPostEvent` arm. This ticket leaves that arm to 22.

Type:

[x] coding

Blockers:

[ ] None.

## Inputs

### Working

[ ] `EventBody.ActorStart` and `(|ActorStart|_|)` in `src/Shared/History.fs` build and match `EventBody.Order(Order.StartActorDeprecated request)`.
[ ] `postEvent` in `src/Server/Core/CoreMailboxApi.fs` matches `Ok [ stored ]` on `storedEvents log [ eventId ]`. When `expandStored` returns a start and its stop, `postEvent` returns `Error "stored Event not found"`.
[ ] `queueStart` in `src/Shared/RunLaunch.fs` queues `posted "Exec" (EventBody.ActorStart request)`. `queueRun` in `src/Client/Commands.fs` calls it, and `tests/Shared.Tests/RunEditCommitTests.fs` calls it.

### Reference

[ ] [Arch](../../arch.md).
[ ] `Order.StartActor` of `ActorOrder` in `src/Shared/History.fs`. `ActorOrder` carries `actor: ActorFunction`, `actorNodeId`, and `actorName`.

## Do not load

[ ] `dispatchPostEvent`. 22 — Convert CoreMsg.PostEvent owns it.
[ ] `CoreMailboxApi.coreMailboxStartActor`. 25 — Remove the coreMailboxStartActor door owns it.
[ ] `Order.StartActorDeprecated` itself, its codec, and its log arm. 16 — Delete the old Order cases owns them.
[ ] A new `Order` case or a new `CoreMsg` case.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `postEvent` returns the stored start when `storedEvents` returns that start with its stop. It does not return `Error "stored Event not found"` for that pair.
3. [ ] `queueStart` queues `Order.StartActor` of `ActorOrder`. If the client cannot build an `ActorOrder`, because `ActorFunction` is a server function, stop and ask.
4. [ ] Delete `EventBody.ActorStart` and `(|ActorStart|_|)` from `src/Shared/History.fs` when nothing calls them.
5. [ ] Search `src/` and `tests/` for `EventBody.ActorStart`. Hits remain only in files that 22 or 25 owns.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/RunLaunch.fs`
[ ] `src/Client/Commands.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Shared.Tests/RunEditCommitTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`: a test where `postEvent` posts a start that stops at once and returns the stored start.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/24-move-the-remaining-actorstart-writers-stop.md`, then ask.
[ ] Stop when the client cannot build an `ActorOrder`.
[ ] Stop when a search hit for `EventBody.ActorStart` remains outside Outputs and outside the files 22 and 25 own.
[ ] Stop when the edit deletes `Order.StartActorDeprecated`.
[ ] Stop when the change passes 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `RunEditCommitTests`
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
[x] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/24-move-the-remaining-actorstart-writers-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
