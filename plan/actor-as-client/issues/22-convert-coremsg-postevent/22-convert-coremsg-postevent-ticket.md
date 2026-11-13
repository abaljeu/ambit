# 22 — Convert CoreMsg.PostEvent
**Status**: defined
One job: A client `Order` is posted through `Mailbox.PlaceOrder`. A client `Notice` is refused. `CoreMsg.PostEvent` is gone, and so is `CoreMsg` when that case is the last one.

The blockers remove the other `CoreMsg` cases. This ticket removes the last one. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` is the Order path. `CoreQueue.Notice` is the Notice path. A client notice stays refused, so this ticket does not call `CoreMailboxApi.postNotice` for a client event.

[ ] `FunctionRun` in `src/Shared/History.fs` stays `getGraph` and `secret`.
[ ] This ticket adds no session id. `putFunctionStart` in `src/Server/Core/CoreActorPool.fs` already stores the live-row `sessionId`.
[ ] The source files are 6. The test files are 3. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [09 — Start the query command](../09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)
[ ] [24 — Post Notice.SearchStopped](../../../online-search/issues/24-post-searchstopped-notice.md)
[ ] [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md)
[ ] [21 — Convert CoreMsg.ActorStop](../21-convert-coremsg-actorstop/21-convert-coremsg-actorstop-ticket.md)

## Inputs

### Working

[ ] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent` before the blockers.
[ ] After the blockers, `PostEvent` is the only `CoreMsg` case.
[ ] `CoreMsg.PostEvent` is `caller`, `event`, and `AsyncReplyChannel<Result<EventId, string>>`.
[ ] `CoreMailboxApi.postEventAccepted` in `src/Server/Core/CoreMailboxApi.fs` builds `PostEvent(caller, event, channel)`.
[ ] `CoreMailboxApi.messageForPosted` builds `Mailbox.CoreMsg` of `PostEvent` when `sendsAsOrder` is false or `MailboxLog.placedOrderFromEvent` fails.
[ ] `sendsAsOrder` is true for `EventBody.Undo`, `EventBody.Redo`, and a `Change` whose id is `EventId.zero`.
[ ] `CoreMailboxApi.acceptOne` calls `MailboxHost.postPlacedOrder` for Undo and Redo. Every other event calls `postEventAccepted`.
[ ] `MailboxLog.placedOrderFromEvent` in `src/Shared/MailboxLog.fs` returns `Ok` for `EventBody.Order`. `orderOfBody` returns `Error "unsupported"` for a `Notice`, including `EventBody.ActorStop`.
[ ] `CoreMailboxLoop.runMsg` matches `PostEvent` and calls `CoreMailboxActors.dispatchPostEvent`.
[ ] `CoreMailboxLoop.messageCaller` matches `PostEvent`.
[ ] `CoreMailboxEvents.operationContext` and `CoreMailboxEvents.replyFailure` in `src/Server/Core/CoreMailboxEvents.fs` match `PostEvent`.
[ ] `CoreMailboxActors.dispatchPostEvent` replies `Error "ActorStop is not a client event type"` for `EventBody.ActorStop`.
[ ] `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart` and `postCancel` for `EventBody.Cancel`.
[ ] `postActorStart` posts `Order.StartActor` for `?test` and for `?ai` through `postBuiltOrder`. Any other name calls `postChosenStart`.
[ ] `CoreEventDispatch.completeOther` replies `Error "Notice is not a client event type"` for `EventBody.Notice`.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` runs `replyLoad`, `replyEdit`, `replyPlaced`, `replyCancel`, `replyActorOrder`, and `dispatchStartActor`.
[ ] The `Order.StartActorDeprecated` arm calls `CoreMailboxActors.dispatchStartActor`.
[ ] The `Order.CancelActor` arm calls `CoreMailboxActors.replyCancel`.
[ ] `postCancel` replies the stored event when `CoreMailboxEvents.storedBySubmission` finds the submission. `replyCancel` replies `Ok EventId.zero`.
[ ] `CoreMailboxApi.coreMailboxStartActor` places `Order.StartActorDeprecated` through `MailboxHost.postPlacedOrder`.
[ ] `CoreMailboxApi.addCoreMsg` posts `Mailbox.CoreMsg`. `MailboxHost.coreMsgItem` and `MailboxHost.postAndAsyncReply` in `src/Server/Core/MailboxHost.fs` build that case.
[ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` in `tests/Server.Tests/MailboxExpandTests.fs` builds `PostEvent`.
[ ] ``postEvent runs through Mailbox CoreMsg`` in `tests/Server.Tests/MailboxExpandTests.fs` calls `CoreMailboxApi.postEvent` with a zero-id `Change`.
[ ] ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` calls `CoreMailboxApi.postEvents`.
[ ] ``mailbox appends ActorStart and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` calls `CoreMailboxApi.coreMailboxStartActor`.

### Reference

[ ] [Arch](../../arch.md) **Place actor order**. `placeOrder` runs `replyActorOrder` for `Order.StartActor`.

## Do not load

[ ] `CoreMailboxApi.postNotice` for a client event. A client `Notice` stays refused.
[ ] `FunctionRun`. It stays `getGraph` and `secret`.
[ ] `ActorInput.sessionId` and the live-row `sessionId`.
[ ] A new `Order` case.
[ ] A new `CoreMsg` case.
[ ] `CoreEventDispatch.postEvent`. `replyPosted` still calls it for an `Order` that `placeOrder` accepts.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `acceptOne` and `messageForPosted` place an event when `MailboxLog.placedOrderFromEvent` returns `Ok`. The place is `MailboxHost.postPlacedOrder` or `Mailbox.placeForCaller`, which is the path Undo, Redo, and a zero-id `Change` already use.
3. [ ] A client `EventBody.ActorStop` still replies `Error "ActorStop is not a client event type"`. Any other client `Notice` still replies `Error "Notice is not a client event type"`.
4. [ ] Change: `postActorStart` is callable from `placeOrder`. The `Order.StartActorDeprecated` arm calls it for `?test` and for `?ai`. Any other name calls `dispatchStartActor`, which is the call that arm makes today.
5. [ ] `postActorStart` takes the event `MailboxLog.eventFromPlacedOrder` builds from the placed order.
6. [ ] Change: `replyCancel` uses the `storedBySubmission` check that `postCancel` uses. A stored event replies through `CoreMailboxEvents.replyStored`.
7. [ ] Contract: delete `sendsAsOrder` and `isUndoOrRedo` when `acceptOne` and `messageForPosted` no longer call them.
8. [ ] Contract: delete `CoreMsg.PostEvent`, `postEventAccepted`, `dispatchPostEvent`, and `postCancel`.
9. [ ] Contract: delete the `PostEvent` arms in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`.
10. [ ] Contract: `PostEvent` is the last `CoreMsg` case, so delete `CoreMsg`, `Mailbox.CoreMsg`, `CoreMailboxApi.addCoreMsg`, `CoreMailboxApi.reply`, `MailboxHost.coreMsgItem`, and `MailboxHost.postAndAsyncReply`.
11. [ ] `MailboxHost.postMailboxAndAsyncReply` and `MailboxHost.postPlacedOrder` stay.
12. [ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` builds a `Mailbox.PlaceOrder` and a `Mailbox.Ask`. It builds no `CoreMsg`.
13. [ ] ``postEvent runs through Mailbox CoreMsg`` posts that zero-id `Change` through `Mailbox.PlaceOrder`.
14. [ ] `FunctionRun` stays `getGraph` and `secret`.

## Outputs

[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/MailboxHost.fs`
[ ] `tests/Server.Tests/MailboxExpandTests.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` — ``mailbox appends ActorStart and ActorStop in lifecycle order`` keeps `coreMailboxStartActor`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/22-convert-coremsg-postevent-stop.md`, then ask.
[ ] Stop when a `CoreMsg` case other than `PostEvent` is still defined.
[ ] Stop when the edit adds a session id to `FunctionRun`, `Notice`, or `ActorChanges`.
[ ] Stop when a client `Notice` is stored.
[ ] Stop when the edit adds an `Order` case or a `CoreMsg` case.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `MailboxExpandTests`
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
  - `tests/Server.Tests`: `Issue41CoreMailboxTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/22-convert-coremsg-postevent-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
