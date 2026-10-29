# 107 — Add EventBody.Order

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [106 — Move Order beside EventBody, in Ev.fs](106-move-order-beside-eventbody.md)
[ ] [105 — One Ev.withOps in History](105-one-ev-withops-in-history.md) is optional (2026-10-25). If it is not done, 107, 108, 65, 109, and 110 change each of the three op rebuilds (`PersistStamp.appendToEvent`, `CoreMailboxEvents.withAppliedOps`, `CoreEventDispatch.withConfirmedOps`) where they name `Ev.withOps`. 107 then has 7 source files.

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `EventBody`, `Ev.ops`, `Ev.target`, `Ev.withOps`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeBody`, `decodeBody`, `encodeStartRequest`, `decodeStartRequest`, `encodeLoadSaveCommandRequest`, `decodeLoadSaveCommandRequest`, `encodeEventId`, `decodeEventId`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`
[ ] [ActorLive.fs](src/Shared/ActorLive.fs) `applyEvent`, `resultOf`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `completeAction`

### Match sites

[ ] Each match on `EventBody` with no wildcard gets one new arm
[ ] `History.fs` `Ev.ops`, `Ev.target`, `Ev.withOps`
[ ] `EventJson.fs` `encodeBody`
[ ] `MailboxLog.fs` `orderOfBody`
[ ] `ActorLive.fs` `applyEvent`, `resultOf`
[ ] `CoreEventDispatch.fs` `completeAction`
[ ] [MailboxHistoryDurabilityTests.fs](tests/Server.Tests/MailboxHistoryDurabilityTests.fs) `bodyKind`
[ ] These matches have a wildcard and stay: `Api.countsAsExternal`, `Api.inferredNodeEvents`, `CoreMailboxApi.isClientActorStop`, `CoreMailboxApi.expandStored`, `CoreMailboxActors.dispatchPostEvent`, `CoreEventDispatch.pushChangeSpecials`, `SyncLogic.isNoticeEvent`, `SyncLogic.markUndoRedoVersion`, `ClientHistory.stampBody`
[ ] Nothing writes `EventBody.Order` in this ticket

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips a Change`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change`
[ ] [EventTests.fs](tests/Shared.Tests/EventTests.fs) `Ev.withOps replaces the ops and keeps the body kind`

## 4. Context

### EventBody

[ ] `EventBody` is `Change | Undo | Redo | ActorStart | ActorStop | Load | Cancel | Notice`
[ ] `Order` compiles before `EventBody` in `History.fs`
[ ] `Order` is `Change of ops * origin | Undo of target | Redo of target | StartActor | StartPeerActor | LoadSave | Load | Cancel`

### JSON

[ ] `encodeBody` writes a `kind` field and the case fields
[ ] `decodeBody` reads `kind` and selects the case decoder
[ ] `EventJson` is the server codec and the client codec
[ ] `EventLogFile`, `DbAgent`, `BootCache`, and the client poll decode through `EventJson.decode`
[ ] `encodeStartRequest` and `encodeLoadSaveCommandRequest` exist
[ ] No codec for `LoadSavePath`, `PeerActorName`, or `ChangeOrigin` exists

## 5. Changes

### Change

[ ] One expand: add `EventBody.Order of Order` beside the old cases

### History.fs

[ ] Add `| Order of Order` to `EventBody`, before `Notice`
[ ] `Ev.ops` of `EventBody.Order (Order.Change(ops, _))` is `Some ops`
[ ] `Ev.ops` of any other `EventBody.Order` is `None`
[ ] `Ev.target` of `EventBody.Order _` is `None`
[ ] `Ev.withOps` of `EventBody.Order (Order.Change(_, origin))` gives `EventBody.Order (Order.Change(ops, origin))`
[ ] That `Ev.withOps` arm is the first arm, so it runs before `EventBody.Change`
[ ] `Ev.withOps` of any other `EventBody.Order` returns the Ev unchanged

### Arms

[ ] In every other match the `EventBody.Order _` arm is the last arm
[ ] `ActorLive.applyEvent` returns `live`
[ ] `ActorLive.resultOf` returns `None`
[ ] `CoreEventDispatch.completeAction` returns `Error "Order Events are mailbox-generated"`
[ ] `MailboxLog.orderOfBody` returns `Ok order`

### EventJson.fs

[ ] Add `encodeOrder` and `decodeOrder`
[ ] The order `kind` values are `change`, `undo`, `redo`, `startActor`, `startPeerActor`, `loadSave`, `load`, `cancel`
[ ] `change` writes `ops` and `origin`; `origin` is `edit` or `parse`
[ ] `undo` and `redo` write `target`
[ ] `startActor` writes `start` with `encodeStartRequest`
[ ] `startPeerActor` writes `peerName` and `start`
[ ] `loadSave` writes `path`, `peerName`, and `request` with `encodeLoadSaveCommandRequest`
[ ] `path` is `git` or `desk`
[ ] `load` writes `nodeId`; `cancel` writes `focusId`
[ ] `encodeBody` writes `EventBody.Order` as `kind` `order` and field `order`
[ ] `decodeBody` reads `kind` `order` with `decodeOrder`
[ ] The old body kinds stay

### Size

[ ] 5 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`, `ActorLive.fs`, `CoreEventDispatch.fs`
[ ] 4 test files: `EventJsonTests.fs`, `MailboxLogTests.fs`, `EventTests.fs`, `MailboxHistoryDurabilityTests.fs`

## 6. Tests

### JSON

[ ] Add `Ev JSON round-trips each EventBody.Order case` in `EventJsonTests.fs`
[ ] Each of the eight `Order` cases inside `EventBody.Order` decodes to the same Ev
[ ] `Order.Change` keeps `ChangeOrigin.Parse`
[ ] Filter: `Ev JSON round-trips each EventBody.Order case`

### MailboxLog

[ ] Add `ofEv reads EventBody.Order` in `MailboxLogTests.fs`
[ ] `ofEv` of `EventBody.Order (Order.Change(ops, ChangeOrigin.Parse))` gives that order
[ ] Filter: `ofEv reads EventBody.Order`

### Readers

[ ] Add `Ev.ops reads an Order.Change body` in `EventTests.fs`
[ ] `Ev.ops` of `EventBody.Order (Order.Change(ops, origin))` is `Some ops`
[ ] `Ev.withOps` of that body keeps `origin`
[ ] Filter: `Ev.ops reads an Order.Change body`

### Ripple

[ ] In `MailboxHistoryDurabilityTests.fs`, `bodyKind` maps `EventBody.Order _` to `"order"`
[ ] Filter: `MailboxHistoryDurabilityTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
