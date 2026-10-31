# 107 — Add EventBody.Order

**Status:** `coded`
**Trackers:** architecture

## 1. Type

[x] `coding`

## 2. Blockers

[x] [106 — Move Order beside EventBody](106-move-order-beside-eventbody.md)
[x] [105 — One Ev.withOps in History](105-one-ev-withops-in-history.md)

## 3. Look at

### Owner

[x] [History.fs](src/Shared/History.fs) owns `EventBody`, `Ev.ops`, `Ev.target`, `Ev.withOps`

### Source

[x] [EventJson.fs](src/Shared/EventJson.fs) `encodeBody`, `decodeBody`, `encodeStartRequest`, `decodeStartRequest`, `encodeLoadSaveCommandRequest`, `decodeLoadSaveCommandRequest`, `encodeEventId`, `decodeEventId`
[x] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`
[x] [ActorLive.fs](src/Shared/ActorLive.fs) `applyEvent`, `resultOf`
[x] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `completeAction`

### Match sites

[x] Each match on `EventBody` with no wildcard gets one new arm
[x] `History.fs` `Ev.ops`, `Ev.target`, `Ev.withOps`
[x] `EventJson.fs` `encodeBody`
[x] `MailboxLog.fs` `orderOfBody`
[x] `ActorLive.fs` `applyEvent`, `resultOf`
[x] `CoreEventDispatch.fs` `completeAction`
[x] [MailboxHistoryDurabilityTests.fs](tests/Server.Tests/MailboxHistoryDurabilityTests.fs) `bodyKind`
[ ] These matches have a wildcard and stay: `Api.countsAsExternal`, `Api.inferredNodeEvents`, `CoreMailboxApi.isClientActorStop`, `CoreMailboxApi.expandStored`, `CoreMailboxActors.dispatchPostEvent`, `CoreEventDispatch.pushChangeSpecials`, `SyncLogic.isNoticeEvent`, `SyncLogic.markUndoRedoVersion`, `ClientHistory.stampBody`
[x] Nothing writes `EventBody.Order` in this ticket

### Tests this ticket names

[x] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips a Change`
[x] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change`
[x] [EventTests.fs](tests/Shared.Tests/EventTests.fs) `Ev.withOps replaces the ops and keeps the body kind`

## 4. Context

### EventBody

[x] `EventBody` is `Change | Undo | Redo | ActorStart | ActorStop | Load | Cancel | Notice`
[x] `Order` compiles before `EventBody` in `History.fs`
[x] `Order` is `Change of ops * origin | Undo of target | Redo of target | StartActor | StartPeerActor | LoadSave | Load | Cancel`

### JSON

[x] `encodeBody` writes a `kind` field and the case fields
[x] `decodeBody` reads `kind` and selects the case decoder
[x] `EventJson` is the server codec and the client codec
[x] `EventLogFile`, `DbAgent`, `BootCache`, and the client poll decode through `EventJson.decode`
[x] `encodeStartRequest` and `encodeLoadSaveCommandRequest` exist
[ ] No codec for `LoadSavePath`, `PeerActorName`, or `ChangeOrigin` exists

## 5. Changes

### Change

[x] One expand: add `EventBody.Order of Order` beside the old cases

### History.fs

[x] Add `| Order of Order` to `EventBody`, before `Notice`
[x] `Ev.ops` of `EventBody.Order (Order.Change(ops, _))` is `Some ops`
[x] `Ev.ops` of any other `EventBody.Order` is `None`
[x] `Ev.target` of `EventBody.Order _` is `None`
[x] `Ev.withOps` of `EventBody.Order (Order.Change(_, origin))` gives `EventBody.Order (Order.Change(ops, origin))`
[x] That `Ev.withOps` arm is the first arm, so it runs before `EventBody.Change`
[x] `Ev.withOps` of any other `EventBody.Order` returns the Ev unchanged

### Arms

[x] In every other match the `EventBody.Order _` arm is the last arm
[x] `ActorLive.applyEvent` returns `live`
[x] `ActorLive.resultOf` returns `None`
[x] `CoreEventDispatch.completeAction` returns `Error "Order Events are mailbox-generated"`
[x] `MailboxLog.orderOfBody` returns `Ok order`

### EventJson.fs

[x] Add `encodeOrder` and `decodeOrder`
[x] The order `kind` values are `change`, `undo`, `redo`, `startActor`, `startPeerActor`, `loadSave`, `load`, `cancel`
[x] `change` writes `ops` and `origin`; `origin` is `edit` or `parse`
[x] `undo` and `redo` write `target`
[x] `startActor` writes `start` with `encodeStartRequest`
[x] `startPeerActor` writes `peerName` and `start`
[x] `loadSave` writes `path`, `peerName`, and `request` with `encodeLoadSaveCommandRequest`
[x] `path` is `git` or `desk`
[x] `load` writes `nodeId`; `cancel` writes `focusId`
[x] `encodeBody` writes `EventBody.Order` as `kind` `order` and field `order`
[x] `decodeBody` reads `kind` `order` with `decodeOrder`
[x] The old body kinds stay

### Size

[x] 5 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`, `ActorLive.fs`, `CoreEventDispatch.fs`
[x] 4 test files: `EventJsonTests.fs`, `MailboxLogTests.fs`, `EventTests.fs`, `MailboxHistoryDurabilityTests.fs`

## 6. Tests

### JSON

[x] Add `Ev JSON round-trips each EventBody.Order case` in `EventJsonTests.fs`
[x] Each of the eight `Order` cases inside `EventBody.Order` decodes to the same Ev
[x] `Order.Change` keeps `ChangeOrigin.Parse`
[x] Filter: `Ev JSON round-trips each EventBody.Order case`

### MailboxLog

[x] Add `ofEv reads EventBody.Order` in `MailboxLogTests.fs`
[x] `ofEv` of `EventBody.Order (Order.Change(ops, ChangeOrigin.Parse))` gives that order
[x] Filter: `ofEv reads EventBody.Order`

### Readers

[x] Add `Ev.ops reads an Order.Change body` in `EventTests.fs`
[x] `Ev.ops` of `EventBody.Order (Order.Change(ops, origin))` is `Some ops`
[x] `Ev.withOps` of that body keeps `origin`
[x] Filter: `Ev.ops reads an Order.Change body`

### Ripple

[x] In `MailboxHistoryDurabilityTests.fs`, `bodyKind` maps `EventBody.Order _` to `"order"`
[x] Filter: `MailboxHistoryDurabilityTests`

## 7. Done
[x] Run `scripts/agent-test.sh` with no arguments.
[x] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[x] All executed tests pass.
