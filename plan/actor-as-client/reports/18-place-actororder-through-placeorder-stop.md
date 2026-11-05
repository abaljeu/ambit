# 18 — Place ActorOrder through PlaceOrder stop

Ticket: [18 — Place ActorOrder through PlaceOrder](../issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md)

Arch: [Arch](../arch.md)

## Stop

[ ] The build stops before any source edit and before any commit of source.

[ ] Stop when the pull cannot both call `startFunction` and run `order.actor`.

[ ] Continue when a human names where `placeOrder` reads `actorNodeId` for `FunctionStart`.

## Why

[ ] Process step 4 calls `startFunction` and runs `order.actor` on the .NET thread pool.

[ ] `startFunction` takes `FunctionStart`. `FunctionStart.actorNodeId` is the live-row id in `admitFunctionStart` and `putFunctionStart` in `src/Server/Core/CoreActorPool.fs`.

[ ] `ActorOrder` is `actor`, `getGraph`, `focusId`, and `actorName` in `src/Shared/History.fs`. It has no `actorNodeId` field.

[ ] Arch Curried actor stores the applied `ActorFunction` on `ActorOrder`. `curriedActor` is `NodeId -> ActorFunction`.

[ ] Arch Place actor order says `placeOrder` reads `actorNodeId` from that closure.

[ ] `ActorFunction` is a function. A caller of that function receives `FunctionRun` and the door. Captured `actorNodeId` is not a field the pull can read.

[ ] `FunctionRun` holds `getGraph` and `secret`. It has no `actorNodeId`.

[ ] `PlacedOrder` holds `orderId`, `authority`, `commandName`, and `order`. It has no `actorNodeId`.

[ ] The client's `Caller` holds `authority`, `name`, and `secret`. It has no `actorNodeId`.

[ ] Reading the id from `actorName` is a Stop when on this ticket.

[ ] Adding `actorNodeId` to `ActorOrder` is a Do not load on this ticket.

## Inputs that match

[ ] `postActorOrder` in `src/Server/Core/MailboxHost.fs` posts `Authority "Actor"` and `Credential ""`.

[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, and `Order.ActorOrder`.

[ ] `Order.CancelActor` is already accepted.

[ ] The Shared actor type and the pool actor type are both named `FunctionActor`.

[ ] `startFunction` mints the Actor credential and the Actor session id.

## Check

[ ] Check did not run. `scripts/agent-test.sh` did not run.

[ ] `plan/actor-as-client/reports/18-place-actororder-through-placeorder-check.md` was not written.
