STOP: `EventBody.ActorStart` in `src/Shared/History.fs` builds `Order.StartActorDeprecated`. Remake that use first. Proposed ticket: 29 — Give the client start its own body. One job: `EventBody.ActorStart` stays the client request and stops building `Order.StartActorDeprecated`.

## Present situation

[ ] Process step 1 stops on one Working input. `dispatchStartLoadSaveCommand` calls `dispatchActorStartResult`. `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk` and for `LoadSavePath.Git`. `dispatchActorStartResult` calls `finishReadyStart` when that start returns `Some secret`.
[ ] `finishReadyStart` appends `EventBody.ActorStart`. Its arguments are `ActorStart`, `Credential`, and `CoreChangesDoor`.
[ ] `Order.StartActor` stores `ActorOrder`. `ActorOrder` stores the curried `ActorFunction`.
[ ] ``Desk load-save runs no Actor and posts no StartActor`` requires the Desk history to hold no `Order.StartActor`.
[ ] A Desk `ActorFunction` belongs to [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md). Arch leaves `postLoadSaveCommand` as the handler that fills that function.
[ ] `queueStart` queues `EventBody.ActorStart`.
[ ] `decodeOrder` builds `Order.StartActorDeprecated` from wire kind `startActor`.
[ ] Giving `EventBody.ActorStart` its own representation, and then deleting the case, stays inside `History.fs`, `EventJson.fs`, `MailboxLog.fs`, `CoreMailboxLoop.fs`, and `EventJsonTests.fs`.
[ ] Recording `Order.StartActor` from the Desk path also takes `CoreMailboxActors.fs`, `CoreMailboxLoad.fs`, `CoreActorPool.fs`, `DeskLoadPush.fs`, and `LoadSaveCommandTests.fs`. That set passes the size limit in [Size limit](../../../context/size-limit.md).
[ ] This run edits no file under `src/` or `tests/`.
