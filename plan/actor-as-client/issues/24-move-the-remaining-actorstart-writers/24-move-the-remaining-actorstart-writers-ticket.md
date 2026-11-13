# 24 — Move the remaining ActorStart writers onto StartActor
**Status**: defined
One job: The Browser queues an `ActorStart` request and posts it, and its event-log copy records the `ActorOrder` the Server returns. The Browser never builds an `ActorOrder`, and `EventBody.ActorStart` with `(|ActorStart|_|)` stays.

[ ] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) depends on this ticket.
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
3. [ ] `queueStart` queues and posts the client `ActorStart` request. The client's event-log copy records the `ActorOrder` the server returns. The client never builds an `ActorOrder`.
4. [ ] `EventBody.ActorStart` stays in `src/Shared/History.fs`. It is the client request.
5. [ ] `(|ActorStart|_|)` stays in `src/Shared/History.fs`. It is the active pattern for that request.

## Outputs

[ ] `src/Shared/RunLaunch.fs`
[ ] `src/Client/Commands.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Shared.Tests/RunEditCommitTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`: a test where `postEvent` posts a start that stops at once and returns the stored start.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/24-move-the-remaining-actorstart-writers-stop.md`, then ask.
[ ] Stop when the client builds an `ActorOrder`.
[ ] Stop when the edit deletes `EventBody.ActorStart` or `(|ActorStart|_|)`.
[ ] Stop when the edit deletes `Order.StartActorDeprecated`.
[ ] Stop when the change passes 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `RunEditCommitTests`
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
[x] All executed tests pass.
[ ] Rewrite `24-review.md` with the present line. The line is `Check: pass` or `STOP: <reason>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
