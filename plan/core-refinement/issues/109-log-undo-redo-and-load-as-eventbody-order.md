# 109 — Log Undo, Redo, and Load as EventBody.Order

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `EventBody.Undo`, `EventBody.Redo`, `EventBody.Load`, the `EventBody` module, `Ev.withOps`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeBody`, `decodeBody`, `decodeUndoBody`, `decodeRedoBody`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`, `historyBody`

### Match sites and constructors

[ ] `EventBody.Undo` and `EventBody.Redo` appear in 6 source files and 10 test files
[ ] `EventBody.Load` appears in 5 source files and 3 test files
[ ] `ClientHistory.undoEvent` and `ClientHistory.redoEvent` pass `EventBody.Undo` and `EventBody.Redo` as functions to `invertAs`
[ ] `CoreMailboxLoad.replyLoad` builds `EventBody.Load subject`
[ ] Patterns: `Ev.ops`, `Ev.target`, `Ev.withOps`, `ActorLive.applyEvent`, `ActorLive.resultOf`, `CoreEventDispatch.completeAction`, `CoreEventDispatch.pushChangeSpecials`, `SyncLogic.markUndoRedoVersion`, `ClientHistory.stampBody`, `EventJson.encodeBody`, `MailboxLog.orderOfBody`
[ ] The `EventBody` module functions and patterns keep each site as written

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips name-only Undo`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) the Undo and Redo round trip (today `Undo and Redo keep the target and drop the ops`), `Load keeps its node through EventJson and MailboxLog`
[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `loadNode logs Order.Load`
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `name-only Undo and Redo store completed Events with submission ids`

## 4. Context

### Today

[ ] [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md) added the `EventBody` module with `Change` and `(|Change|_|)`
[ ] After [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md), `Order.Undo` and `Order.Redo` hold the target and the Op list
[ ] After 65, a placed Undo or Redo is logged as `EventBody.Order` of `Order.Undo` or `Order.Redo`
[ ] After 65, nothing on the Server logs `EventBody.Undo` or `EventBody.Redo`; the cases stay for the wire and for old rows
[ ] `historyBody` maps `Order.Load` to `EventBody.Load`
[ ] An undecoded row is skipped on read, and `BootCache` fetches `/state` on a decode error

## 5. Changes

### Change

[ ] One contract: `EventBody.Undo`, `EventBody.Redo`, and `EventBody.Load` leave with their `undo`, `redo`, and `load` body kinds, and Load is logged as `EventBody.Order`

### History.fs

[ ] Remove `Undo`, `Redo`, and `Load` from `EventBody`
[ ] The `EventBody` module gains `Undo (target: EventId, ops: Op list)`, which is `EventBody.Order (Order.Undo(target, ops))`
[ ] The module gains `Redo (target: EventId, ops: Op list)`, which is `EventBody.Order (Order.Redo(target, ops))`
[ ] The module gains `Load (nodeId: NodeId)`, which is `EventBody.Order (Order.Load nodeId)`
[ ] The module gains `(|Undo|_|)`, `(|Redo|_|)`, and `(|Load|_|)` over those orders
[ ] Remove the `EventBody.Undo` and `EventBody.Redo` arms from `Ev.withOps`

### EventJson.fs

[ ] Remove the `undo`, `redo`, and `load` body kinds from `encodeBody` and `decodeBody`
[ ] Remove `decodeUndoBody` and `decodeRedoBody`

### MailboxLog.fs

[ ] Remove the `Undo`, `Redo`, and `Load` arms from `orderOfBody`
[ ] `historyBody` maps `Order.StartActor` and `Order.Cancel` to the old cases
[ ] `historyBody` replies `Error "unsupported"` for `Order.StartPeerActor` and `Order.LoadSave`
[ ] `historyBody` maps every other order to `HistoryBody.Order order`

### Size

[ ] 3 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`
[ ] 1 test file: `EventJsonTests.fs`

## 6. Tests

### Old rows

[ ] Add `An old undo, redo, or load row is skipped on read` in `EventJsonTests.fs`
[ ] A JSON body with `kind` `undo`, `redo`, or `load` fails `EventJson.decode`
[ ] Filter: `An old undo, redo, or load row is skipped on read`

### Kept

[ ] Keep `Ev JSON round-trips name-only Undo`
[ ] Keep the Undo and Redo round trip in `MailboxLogTests.fs`
[ ] Keep `Load keeps its node through EventJson and MailboxLog`
[ ] Keep `loadNode logs Order.Load`
[ ] Keep `name-only Undo and Redo store completed Events with submission ids`
[ ] Filter: `Ev JSON round-trips name-only Undo`
[ ] Filter: the Undo and Redo round-trip name in `MailboxLogTests.fs`
[ ] Filter: `Load keeps its node through EventJson and MailboxLog`
[ ] Filter: `loadNode logs Order.Load`
[ ] Filter: `name-only Undo and Redo store completed Events with submission ids`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
