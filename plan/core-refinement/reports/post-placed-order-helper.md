# Post placed order helper

## Helper

`postPlacedOrder` is in [MailboxHost.fs](../../../src/Server/Core/MailboxHost.fs).

```fsharp
let internal postPlacedOrder
    (host: MailboxHost)
    (caller: Caller)
    (placed: PlacedOrder)
    : Async<Result<EventId, string>>
```

The body posts `Mailbox.placeForCaller` through `postMailboxAndAsyncReply`. `placeForCaller` sets the PlacedOrder authority from the Caller.

## Call sites

1. `postActorOrder` in MailboxHost.fs builds the Actor Order PlacedOrder, then calls `postPlacedOrder`.
2. `acceptOne` in [CoreMailboxApi.fs](../../../src/Server/Core/CoreMailboxApi.fs) posts an existing Undo or Redo PlacedOrder.
3. `coreMailboxStartActor` in CoreMailboxApi.fs posts a fresh `Order.StartActor` and maps the EventId reply to `Result<unit, string>`.
4. `mbLoadNode` in CoreMailboxApi.fs posts a fresh `Order.Load` and returns `Result<EventId, string>`.
5. `cancelByFocus` in CoreMailboxApi.fs posts a fresh `Order.CancelActor` and maps the EventId reply to `Result<unit, string>`.
6. `place` in ``second Cancel on the same focus replies the same`` in [CancelByFocusTests.fs](../../../tests/Server.Tests/CancelByFocusTests.fs) posts a fresh `Order.CancelActor` and keeps the EventId reply.
7. ``a placed Parse Change is stored without a document file write`` in [Issue41CoreMailboxTests.fs](../../../tests/Server.Tests/Issue41CoreMailboxTests.fs) posts the Parse Change with its submission id and command name `Parse`.
8. ``login during a pending logout runs and a dropped generation rejects a late order`` in [SessionTests.fs](../../../tests/Server.Tests/SessionTests.fs) posts `latePlaced`.

## placeForCaller sites that stay

1. `messageForPosted` in CoreMailboxApi.fs returns one Mailbox for `postForReplies`. The batch shares one gate.
2. `parseBuild` in CoreMailboxApi.fs returns the Mailbox builder for a Parse Change in that same batch.
3. `placeForCaller` in [CoreMsg.fs](../../../src/Server/Core/CoreMsg.fs) builds `Mailbox.PlaceOrder`. `postPlacedOrder` calls it.

## Posts outside this pair

1. `ask` in CoreMailboxApi.fs posts `Mailbox.Ask`.
2. ``getEventsSince seed Error propagates and closes writes`` in [PersistHandlersRestoreTests.fs](../../../tests/Server.Tests/PersistHandlersRestoreTests.fs) posts `Query.EventsSince`.
3. `startPeerActor`, `startLoadSaveCommand`, `recordSearchStart`, `recordSearchStop`, and `actorStop` in CoreMailboxApi.fs post CoreMsg through `postAndAsyncReply`.
4. Callers of `postActorOrder` stay on `postActorOrder`. That function calls the helper.

## Branch

`dev`

## Check

```
scripts/agent-test.sh --filter "FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CancelByFocusTests|FullyQualifiedName~ActorCoreChangesDoorTests|FullyQualifiedName~Issue41CoreMailboxTests|FullyQualifiedName~SessionTests" server
```

Passed 94. Failed 0. Skipped 0.

The filter is the four named suites plus Issue41CoreMailboxTests and SessionTests. Those two files call the helper. Peer start and load-save stay on CoreMsg, outside this filter.
