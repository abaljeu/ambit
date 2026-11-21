Check: pass

`Order.StartActorDeprecated` is `Order.ClientRequestActor` on `Order`. `EventBody.ActorStart` and `(|ActorStart|_|)` keep those names and build and match `Order.ClientRequestActor`.

`encodeOrder` and `decodeOrder` keep wire kind `startActor`.

`historyBody` keeps the `Order.ClientRequestActor` arm and returns `Ok (EventBody.Order order)`.

`MailboxHost.postPlacedOrder` returns `Error "ClientRequestActor is retired"` for that order and does not post it. `placeOrder` replies with `MailboxHost.clientRequestRetired`, the same text, and appends nothing.

``postPlacedOrder refuses ClientRequestActor and appends nothing`` calls `MailboxHost.postPlacedOrder` with `Order.ClientRequestActor`. The reply is that error. `eventPast` matches the log from before the call.

``Ev JSON round-trips each EventBody.Order case`` builds `Order.ClientRequestActor`. The encoded event contains `startActor`, and the event round-trips.

A search of `src/` and `tests/` for `StartActorDeprecated` is empty.

## Suites

- ``Ev JSON round-trips each EventBody.Order case`` passed 1, failed 0, skipped 0.
- ``postPlacedOrder refuses ClientRequestActor and appends nothing`` passed 1, failed 0, skipped 0.
- `scripts/agent-test.sh --full`: `tests/Server.Tests` passed 765, failed 0, skipped 1. Total 766.
- `scripts/agent-test.sh --full`: `tests/Shared.Tests` passed 1940, failed 0, skipped 3. Total 1943.
