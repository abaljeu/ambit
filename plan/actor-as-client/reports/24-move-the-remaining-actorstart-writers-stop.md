# 24 — Move the remaining ActorStart writers onto StartActor

Ticket: [24 — Move the remaining ActorStart writers onto StartActor](../issues/24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md)

## Stop

[x] Resolved. `queueStart` keeps the client `ActorStart`. `postActorStart` converts test, query, and ai into `Order.StartActor` and fills the server `ActorFunction`.
