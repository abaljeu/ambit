# 06 — Log ActorOrder build

Ticket: [[06-add-the-function-and-focus-start-ticket.md]]

## Stage

[x] Stage is build in [06 — Log ActorOrder](CONTEXT.md).

## Edits

[x] `GraphCarrier`, `FunctionRun`, and `Credential` are in `src/Shared/History.fs`.

[x] Log `FunctionActor` is `FunctionRun -> ActorChanges -> Async<unit>`.

[x] Pool `FunctionActor` stays in `src/Server/Core/CoreActorPool.fs` as `FunctionRun -> CoreChangesDoor -> Async<unit>`.

[x] `FunctionStart` stays on the pool function. `startFunction` arguments stay.

[x] `TestActor.functionActor` is `Gambol.Server.FunctionActor`.

[x] `Order.ActorOrder` carries `ActorOrder`. Wire kind is `actorOrder`.

[x] `EventJson.stubActor`, `encodeActorOrder`, and `decodeActorOrder` are in `src/Shared/EventJson.fs`.

[x] `MailboxLog` accepts `Order.ActorOrder`. `placeOrder` replies unsupported for that case.

[x] `MailboxHost.postActorOrder` posts `Mailbox.PlaceOrder`.

[x] `actorChanges` is not in this build.

## Check

[x] Verdict is `pass` in [06 — Log ActorOrder check](../../reports/06-add-the-function-and-focus-start-check.md).

## Status

[x] Ticket status is `coded`.
