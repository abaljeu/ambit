STOP: Renaming `Order.StartActorDeprecated` to `Order.ClientRequestActor`, and `EventBody.ActorStart` with `(|ActorStart|_|)` to `RequestActor`, changes 10 source files and 13 test files. [Size limit](../../../context/size-limit.md) allows 8 source files and 4 test files.

Wire kind `startActor` stays. Client and server share `encodeOrder` and `decodeOrder`.

`historyBody` keeps its arm. That arm returns `Ok (EventBody.Order order)` for this order, and `orderOfBody` reads any `EventBody.Order`, so a stored log can still hold the case.

`postPlacedOrder` is the queue refusal: `Error "ClientRequestActor is retired"`, and the order is not posted. `placeOrder` uses that same refusal. `EventBody.RequestActor` stays the client request and still builds `Order.ClientRequestActor`. The server still converts that request into `Order.StartActor`.

`src/` and `tests/` still use `Order.StartActorDeprecated` and `EventBody.ActorStart`.

## Files the build would change

[ ] `src/Shared/History.fs` renames the order case and the request builders.
[ ] `src/Shared/EventJson.fs` renames the codec arms and keeps wire kind `startActor`.
[ ] `src/Shared/MailboxLog.fs` renames the `historyBody` arm and keeps the `Ok` result.
[ ] `src/Shared/RunLaunch.fs` queues `EventBody.RequestActor`.
[ ] `src/Shared/ActorLive.fs` matches `EventBody.RequestActor` in `applyEvent` and `resultOf`.
[ ] `src/Server/Core/MailboxHost.fs` refuses `Order.ClientRequestActor` in `postPlacedOrder`.
[ ] `src/Server/Core/CoreMailboxLoop.fs` uses that same refusal in `placeOrder`.
[ ] `src/Server/Core/CoreMailboxActors.fs` uses `RequestActor` in `finishReadyStart` and `dispatchPostEvent`.
[ ] `src/Server/Core/CoreMailboxApi.fs` matches `RequestActor` in `expandStored` and `affirmedStart`.
[ ] `src/Server/Core/CoreEventDispatch.fs` matches `RequestActor` in `completeOther`.
[ ] `tests/Shared.Tests/EventJsonTests.fs` builds `Order.ClientRequestActor` in the order round-trip.
[ ] `tests/Shared.Tests/EventTests.fs` builds and matches `EventBody.RequestActor`.
[ ] `tests/Shared.Tests/RunEditCommitTests.fs` matches `EventBody.RequestActor`.
[ ] `tests/Shared.Tests/SyncLogicTests.fs` builds `EventBody.RequestActor`.
[ ] `tests/Shared.Tests/ActorLiveTests.fs` builds `EventBody.RequestActor`.
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` matches `EventBody.RequestActor` and adds ``postPlacedOrder refuses ClientRequestActor and appends nothing``.
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` matches `EventBody.RequestActor`.
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` builds `EventBody.RequestActor`.
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs` builds and matches `EventBody.RequestActor`.
[ ] `tests/Server.Tests/QueryActorTests.fs` builds and matches `EventBody.RequestActor`.
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` builds `EventBody.RequestActor`.
[ ] `tests/Server.Tests/ActorOrderFromStartTests.fs` matches `EventBody.RequestActor`.
[ ] `tests/Server.Tests/ApiPostCommandTests.fs` builds `EventBody.RequestActor`.

## Suites

This run ran no suite.
