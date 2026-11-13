Check: pass

`coreMailboxStartActor` is removed from `src/Server/Core/CoreMailboxApi.fs`.

`dispatchStartActor` is removed from `src/Server/Core/CoreMailboxActors.fs`. `placeOrder` was its caller.

`placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies `Error "StartActorDeprecated is retired"` for `Order.StartActorDeprecated`.

`replyStarted` is removed. It wrapped that reply.

`Order.StartActorDeprecated` stays. [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) owns that case.

`EventBody.ActorStart` stays. It is the client start request.

`dispatchActorStartResult` stays. `CoreMailboxLoad.dispatchStartLoadSaveCommand` calls it. `finishReadyStart` stays with that call.

A search of `src/` and `tests/` for `coreMailboxStartActor` is empty.

## Suites

`dotnet test` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false`, the same arguments as `scripts/agent-test.sh --full`:

- Server: passed 762, failed 0, skipped 1. Total 763.
- Shared: passed 1940, failed 0, skipped 3. Total 1943.
