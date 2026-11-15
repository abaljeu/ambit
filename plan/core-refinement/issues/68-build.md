# 68 — Remove CoreMsg.PostEvent

Ticket: [68 — Remove CoreMsg.PostEvent](68-remove-coremsg-postevent.md)

The server places a client ActorStart and a client Cancel through `Mailbox.PlaceOrder`. `CoreMsg` is `StartLoadSaveCommand`. [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) is `coded`.

`messageForPosted` and `acceptOne` place `EventBody.ActorStart`, `EventBody.Cancel`, `EventBody.Undo`, `EventBody.Redo`, and a zero-id `EventBody.Change`. `MailboxHost.postPlacedOrder` queues `Order.ClientRequestActor`. `placeOrder`'s `ClientRequestActor` arm replies the stored event, places `?test`, `?ai`, and a query as `Order.StartActor`, and replies `Error "unknown actor"` for any other name. `placeOrder`'s `CancelActor` arm replies the stored event, else `runCancel`'s stored event, else `EventId.zero`.

These symbols have left:

- `CoreMsg.PostEvent`
- the `PostEvent` arms of `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`
- `CoreMailboxActors.dispatchPostEvent`
- `CoreMailboxActors.postActorStart`
- `CoreMailboxActors.postCancel`
- `CoreMailboxActors.replyCancel`
- `CoreMailboxApi.cancelByFocus`
- `MailboxHost.clientRequestRetired`

`runCancel`, `postBuiltOrder`, and the `queueAdmitted` helpers stay. `CoreMailboxApi.reply`, `CoreMailboxApi.addCoreMsg`, `Mailbox.CoreMsg`, and `MailboxHost.postAndAsyncReply` stay.

`cancelFocus` posts `EventBody.Cancel` with `id` `EventId.zero` and `commandName` `"Cancel"`. ``posted Cancel replies the stored stop id`` checks that stop id on the first post and on the same submission again. ``second Cancel on the same focus replies the same`` posts one order id twice and reads that stop id. A new order id on an idle focus replies `EventId.zero`.

`origin/staging` at `da2f036e` is the branch base.

Check: pass

- `scripts/agent-test.sh` with no arguments passed Server 57. Failed 0.
- `scripts/agent-test.sh --filter` for each named test passed. Failed 0.
- `scripts/agent-test.sh --full` passed Server 775, skipped 1, and Shared 1946, skipped 3. Failed 0.
