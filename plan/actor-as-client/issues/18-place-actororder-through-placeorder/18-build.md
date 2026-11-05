# 18 — Place ActorOrder through PlaceOrder build

Ticket: [18 — Place ActorOrder through PlaceOrder](18-place-actororder-through-placeorder-ticket.md)

## Process

[x] `startChosen` calls `pool.startActor` for the query arm and the named arm.
[x] `FunctionStart` is `actor`, `getGraph`, and `focusId`.
[x] `placeOrder` builds that `FunctionStart` from `order.actor`, `order.getGraph`, and `order.focusId`.
[x] `postActorOrder` posts `Mailbox.PlaceOrder` with the client's `Caller`.
[x] A Desk Load logs `Order.ActorOrder` and calls no `startFunction`.
[x] `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` stay unsupported in `placeOrder`.
[x] `stubActor` is the wire actor. This build does not call it.

## Check

[x] [18 — Place ActorOrder through PlaceOrder check](../../reports/18-place-actororder-through-placeorder-check.md). The first line is `pass`.
