# 68 — Remove CoreMsg.PostEvent

Check: pass

Ticket: [68 — Remove CoreMsg.PostEvent](68-remove-coremsg-postevent.md)

Status stays `coded`.

`origin/staging` at `da2f036e` is the merge base. This pass changed docs.

## 1. Current

[[../../../doc/current/mailbox.md]]

[[../../../doc/current/query-actor.md]]

- `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand` and `RecordSearchStop`.
- `MailboxHost.postPlacedOrder` queues `Order.ClientRequestActor`.
- `CoreMailboxLoop.placeOrder` runs `CoreMailboxActors.replyClientRequest` for `Order.ClientRequestActor` and `CoreMailboxActors.replyPlacedCancel` for `Order.CancelActor`.
- `replyClientRequest` places a query Run through `queueAdmittedQuery` and `postBuiltOrder` as `Order.StartActor`. The actor name is `query`.
- The events door places each order through `Mailbox.PlaceOrder`.

## 2. User

`doc/user` keeps its operating pages.

## 3. Architecture

[[../arch.md]]

[[../cqrs-messages.md]]

[[../project.md]]

[[../../online-search/arch.md]]

[[../../online-search/issues/24-remove-coremsg-recordsearchstop.md]]

[[../../online-search/issues/27-dialog-cancels-or-replaces-search-actor.md]]

[[../../actor-as-client/arch.md]]

[[../../actor-as-client/issues/21-convert-coremsg-actorstop/21-convert-coremsg-actorstop-ticket.md]]

- The queue text names `CoreMsg` as `StartLoadSaveCommand` and `RecordSearchStop`.
- This ticket is Status `coded`. `messageForPosted` and `acceptOne` place the client ActorStart and the client Cancel.
- 24 — Remove CoreMsg.RecordSearchStop records `CoreMsg` as `StartLoadSaveCommand` and `RecordSearchStop`.
- 27 — Dialog cancels or replaces the search actor names `replyPlacedCancel` as the Cancel entry.
- Actor as client architecture names `replyClientRequest` for the posted run and `replyPlacedCancel` for cancel by focus.
- Search architecture names `replyClientRequest` for the posted query start.

## 4. Records kept

- Core creation architecture keeps the hello-cut hops. The file date is 2026-09-20.
- Single event source architecture keeps `PostEvent` of Ev on that cut's module map.
- The 2026-11-13 journal line for 114 — Remove CoreMsg.ActorStop in the core-refinement project keeps the cases recorded that day.
- The 2026-09-19 journal line in the llm-connector project keeps `cancelByFocus` as the name coded that day.
- Closed tickets and review files keep their records.
- `FileAgent.processPostEvents` stays.
- `DbAgent.processPostEvents` stays.
- Those two functions apply a posted change.
