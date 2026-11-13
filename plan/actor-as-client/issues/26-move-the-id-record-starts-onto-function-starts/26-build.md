STOP: `root`, `probe`, and `load` reach `dispatchStartActor` with no `ActorFunction`, and `gate` and `nope` reach `postChosenStart` with no `ActorFunction`. A `test` start on that door is observed by `tests/Server.Tests/CoreMailboxDoorTests.fs`, outside Outputs.

`dispatchStartActor` passes `pool.startActor` to `dispatchActorStartResult`. `placeOrder` calls `dispatchStartActor` for `Order.StartActorDeprecated`. `CoreMailboxApi.coreMailboxStartActor` places that order. Callers of that door are tests.

The run-definition node supplies the door name. `ROOT` is `root`. `?probe` is `probe`. ``Peer Actor is not available through the Run actor table`` expects `actor 'load' not registered`.

`postActorStart` calls `postChosenStart` for a name other than `test`, `ai`, or a query. `startChosen` calls `pool.startActor`. `finishReadyStart` appends `EventBody.ActorStart`.

[27 — Reject unknown Run commands before the queue](plan/actor-as-client/issues/27-reject-unknown-run-commands-before-the-queue/27-reject-unknown-run-commands-before-the-queue-ticket.md) is on `staging`. `CommandRequest.tryStart` returns `Ok` for `test` and `ai`. Production Run reaches `admittedQueue` for `test`, `ai`, and a query. Tests still post `EventBody.ActorStart` for `?gate` and `?nope`.

`EventBody.ActorStart` stays the client start request. `admittedQueue` fills `Order.StartActor` of `ActorOrder` for `test`, `ai`, and a query. The client builds no `ActorOrder`.

These tests sit outside Outputs and use the bag door or a posted start that has no `ActorFunction`:

- `tests/Server.Tests/CoreMailboxDoorTests.fs` — ``CoreActorPool.startActor selects actor from command node text`` expects `EventBody.ActorStart` after command text `test`.
- `tests/Server.Tests/CoreActorPoolDeliverTests.fs` — the door starts a registered `root` bag actor.
- `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` — `coreMailboxStartActor` on the root request.
- `tests/Server.Tests/PersistHandlersRestoreTests.fs` — ``ActorStart persists across File restart``.
- `tests/Server.Tests/Issue41CoreMailboxTests.fs` — ``mailbox appends ActorStart and ActorStop in lifecycle order`` expects `EventBody.ActorStart`.
- `tests/Server.Tests/AskCancelHarness.fs` — `startOnCommand` calls `coreMailboxStartActor`.
- `tests/Server.Tests/AgentAskTests.fs` — ``Ask extract carries Graph.focus from launch`` registers bag actor `probe`.
- `tests/Server.Tests/GithubTransportActorTests.fs` — ``Peer Actor is not available through the Run actor table``.
- `tests/Server.Tests/TestActorCommandErrorTests.fs` — ``unregistered Actor name fails start`` and ``production events door records a failed ActorStop for an unknown actor``.

`pool.startActor` remains in `src/Server/Core/CoreMailboxActors.fs` at `startChosen` and `dispatchStartActor`. Direct test calls of the pool member stay.

- `tests/Server.Tests` passed 781, failed 0, skipped 1.
- `tests/Shared.Tests` passed 1940, failed 0, skipped 3.
