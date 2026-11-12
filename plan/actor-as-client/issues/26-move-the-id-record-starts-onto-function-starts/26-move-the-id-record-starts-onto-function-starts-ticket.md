# 26 — Move the id-record starts onto function starts
**Status**: defined
One job: No start calls `pool.startActor` with an `ActorStart`. The server posts `Order.StartActor` of `ActorOrder` with the real curried `ActorFunction`.

[ ] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) depends on this ticket. 16 deletes `Order.StartActorDeprecated` after this ticket.
[ ] The client request stays its own type. The server handler fills in the `ActorFunction`. The pool affirms before `Order.StartActor` is posted. A refused start posts nothing. [Arch](../../arch.md) **Curried actor** and **Place actor order**.
[ ] [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md) owns the Desk caller of `dispatchActorStartResult`. That caller does not use `pool.startActor`.
[ ] [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) keeps `dispatchStartActor` for a name other than `test` and `ai`. This ticket changes that function. 22 still deletes `CoreMsg.PostEvent`.

Type:

[ ] coding

Blockers:

[ ] [24 — Move the remaining ActorStart writers onto StartActor](../24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md). `postActorStart` posts `test`, `ai`, and a query through `postActorOrder`.
[ ] [27 — Reject unknown Run commands before the queue](../27-reject-unknown-run-commands-before-the-queue/27-reject-unknown-run-commands-before-the-queue-ticket.md). 27 rejects a `?` command with no function start before `queueRun`, so `postChosenStart` has no production caller.

## Inputs

### Working

[ ] `dispatchStartActor` in `src/Server/Core/CoreMailboxActors.fs` passes `pool.startActor` to `dispatchActorStartResult`. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls `dispatchStartActor` for `Order.StartActorDeprecated`. `CoreMailboxApi.coreMailboxStartActor` is the placer. Its callers are tests.
[ ] The door name comes from the run-definition node: the word after `?`, otherwise an `actor-*` class suffix, otherwise the trimmed lower text. The names that reach this path are `root` (node text `ROOT`), `test` (`?test`, bag `TestActor.actorFn`), `probe` (`?probe`), and `load` (the Git peer test expects `actor 'load' not registered`).
[ ] `dispatchStartLoadSaveCommand` in `src/Server/Core/CoreMailboxLoad.fs` calls `dispatchActorStartResult` with `DeskLoadPush.runLoadSave`. `CoreActorPool.startLoadSaveCommand` returns `Ok None` for Desk, so `finishReadyStart` does not run.
[ ] `postChosenStart` calls `startChosen`, which calls `pool.startActor`. `postActorStart` calls `postChosenStart` when the name from the node text is not `test`, `ai`, or a query. `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart`. `runMsg` calls `dispatchPostEvent` for `CoreMsg.PostEvent`. `CoreMailboxApi.postEventAccepted` builds that message.
[ ] `RunLaunch.queueStart` queues the client `EventBody.ActorStart`. `execRunOp` in `src/Client/Commands.fs` calls it for every `?` command. The server peels `test`, `ai`, and a query off before `postChosenStart`. The names that remain are `gate` (`?gate`) and `nope` (`?nope`).
[ ] `finishReadyStart` logs `EventBody.ActorStart`. `dispatchActorStartResult` and `postChosenStart` call `finishReadyStart`.
[ ] `TestActor.functionActorAt`, `RunAgentActor.actorFn`, and `SearchActor.functionActorAt` are the curried functions for `test`, `ai`, and a query. `RouteRegistration.CreateBoot` sets `Actors` to `[]`. `root`, `gate`, and `probe` exist only as test `ActorFn` registrations. `load` and `nope` have no `ActorFunction`.

### Reference

[ ] `postActorOrder` in `src/Server/Core/MailboxHost.fs`. `replyActorOrder` in `src/Server/Core/CoreMailboxLoop.fs`.

## Do not load

[ ] `CoreMsg.StartLoadSaveCommand` and `DeskLoadPush.runLoadSave`. 20 — Move Desk Load and Desk Save owns them.
[ ] `dispatchPostEvent` and `CoreMsg.PostEvent`. 22 — Convert CoreMsg.PostEvent owns them.
[ ] `CoreMailboxApi.coreMailboxStartActor`. 25 — Remove the coreMailboxStartActor door owns that door.
[ ] `Order.StartActorDeprecated`, its codec, and its log arm. 16 — Delete the old Order cases owns them.
[ ] A posted `?test`, `?ai`, or query Run. `postActorStart` already sends those through `postActorOrder`.
[ ] Git Load, Git Save, and Find and Move. Those starts already carry an `ActorFunction`.
[ ] The pool member `CoreActorPool.startActor`. Direct test calls of that member stay.
[ ] A new `Order` case.
[ ] A session id on `FunctionRun`, `ActorChanges`, or `ActorOrder`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Stop and ask when a start on either path has no `ActorFunction`. Those names are `root`, `gate`, `probe`, `load`, and `nope`. `load` on the Run door stays unregistered.
3. [ ] A `test`, `ai`, or query start that still reaches `dispatchStartActor` becomes `ActorOrder` through `TestActor.functionActorAt`, `RunAgentActor.actorFn`, or `SearchActor.functionActorAt`. `postActorOrder` sends it. The pool affirms before the append. A refusal posts nothing.
4. [ ] `dispatchStartActor` and `startChosen` no longer call `pool.startActor`. `finishReadyStart` no longer logs `EventJson.stubActor` for these starts.
5. [ ] Search `src/Server` for `pool.startActor`. The search is empty.

## Outputs

[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs`

## Stop when

[ ] Record the stop in this ticket file, then ask.
[ ] Stop when a start cannot obtain a real `ActorFunction` on the server.
[ ] Stop when the edit adds a session id.
[ ] Stop when the edit adds an `Order` case.
[ ] Stop when the edit deletes `Order.StartActorDeprecated`.
[ ] Stop when the edit posts `Order.StartActor` before the pool affirms.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when the change passes 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `CoreMsgActorCasesTests`
  - `tests/Server.Tests`: `OrderedEventStreamTests`
  - `tests/Server.Tests`: `QueryActorTests`
[ ] All executed tests pass.
[ ] Record the verdict in this ticket file. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
