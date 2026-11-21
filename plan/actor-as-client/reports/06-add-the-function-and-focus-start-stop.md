# 06 — Log ActorOrder stop

Ticket: [06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

Scope: [06 — Log ActorOrder scope](06-add-the-function-and-focus-start-scope.md)

## Stop

[ ] The build stops before any source edit and before any commit of source.

[ ] Continue when a human approves a build of 10 source files and 2 test files.

## Why

[ ] Process step 2 sets `FunctionActor` to `FunctionRun -> ActorChanges -> Async<unit>`.

[ ] `FunctionActor` today is `FunctionRun -> CoreChangesDoor -> Async<unit>` in `src/Server/Core/CoreActorPool.fs`.

[ ] `FunctionStart.actor` is that `FunctionActor`. `FunctionStart` stays in `src/Server/Core/CoreActorPool.fs`.

[ ] `src/Server/SearchActor.fs` `functionStart` builds that actor. The lambda passes its second argument to `CoreDoor.forCaller`, which takes `CoreChangesDoor`.

[ ] `src/Server/TestActor.fs` `functionActor` is a `FunctionActor`. It passes its second argument to `CoreDoor.postEvents` and `CoreDoor.forCaller`.

[ ] `src/Server/Core/CoreActorPool.fs` `runSchedule` is an Output. It can pass `CoreDoor.actorChanges` at the call.

[ ] `SearchActor.fs` and `TestActor.fs` are outside Inputs and outside Outputs.

[ ] A compiling tree after the signature change edits those two files.

[ ] [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) lists `src/Server/TestActor.fs` as its Output. This ticket says a caller move is 07 and the tickets after it.

## Size

[ ] Outputs already name 8 source files. The limit is 8 source files and 4 test files.

[ ] The green build would change these source files:

- `src/Shared/History.fs`
- `src/Server/Core/CoreActorPool.fs`
- `src/Server/Core/CoreChanges.fs`
- `src/Server/Core/CoreDoor.fs`
- `src/Shared/EventJson.fs`
- `src/Shared/MailboxLog.fs`
- `src/Server/Core/CoreMailboxLoop.fs`
- `src/Server/Core/MailboxHost.fs`
- `src/Server/SearchActor.fs`
- `src/Server/TestActor.fs`

[ ] Source count is 10. Each of `SearchActor.fs` and `TestActor.fs` is required so `FunctionActor` still compiles.

[ ] Test files stay the two Outputs. `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` calls `TestActor.functionActor` and `startFunction`. Its arguments stay valid when those two signatures stay.

## Inputs that match

[ ] `startFunction` is `FunctionStart -> Result<Credential, string>`.

[ ] `Credential` is `Credential of string` in `src/Server/Core/CoreChanges.fs`.

[ ] `Order.CancelActor` is the cancel case. [112 — Place Cancel through PlaceOrder](../../core-refinement/issues/112-place-cancel-through-placeorder.md) has landed. Wire kind is `cancel`.

[ ] [17 — Rename commandId to actorNodeId](../issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md) has landed. `actorNodeId` is on `ActorStart` and `FunctionStart`. It is absent from the planned `ActorOrder`.

[ ] `History.fs` is 571 lines. The other Output sources are under 800 lines.

## Check

[ ] Check did not run. `scripts/agent-test.sh` did not run.

[ ] [06 — Log ActorOrder check](06-add-the-function-and-focus-start-check.md) was not written.
