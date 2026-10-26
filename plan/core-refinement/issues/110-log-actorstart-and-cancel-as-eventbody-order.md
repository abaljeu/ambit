# 110 — Log ActorStart and Cancel as EventBody.Order

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `EventBody.ActorStart`, `EventBody.Cancel`, the `EventBody` module

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeBody`, `encodeActorStart`, `decodeBody`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`, `historyBody`

### Match sites and constructors

[ ] `EventBody.ActorStart` appears in 9 source files and 19 test files
[ ] `EventBody.Cancel` appears in 8 source files and 5 test files
[ ] Constructors: `RunLaunch.queueStart`, `RunLaunch.queueCancel`, `CoreEventDispatch.actorStart`, `CoreMailboxActors` start and failed-start appends, `MailboxLog.historyBody`
[ ] Patterns: `Ev.ops`, `Ev.target`, `ActorLive.applyEvent`, `ActorLive.resultOf`, `CoreEventDispatch.completeAction`, `CoreMailboxActors.dispatchPostEvent`, `CoreMailboxApi.expandStored`, `EventJson.encodeBody`, `MailboxLog.orderOfBody`
[ ] The `EventBody` module functions and patterns keep each site as written

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips ActorStart and ActorStop`, `cancel request JSON round-trips Focus NodeId and EventId`
[ ] [ActorLiveTests.fs](tests/Shared.Tests/ActorLiveTests.fs) `ActorStart adds and ActorStop removes a live Focus`
[ ] [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs), every test

## 4. Context

### Today

[ ] The pool start, the peer start, and the load-save start each log `EventBody.ActorStart` of their `ActorStart`
[ ] `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` each hold an `ActorStart`, directly or as `request.start`
[ ] A Cancel logs no `EventBody.Cancel` on the server; it logs the stop
[ ] `RunLaunch.queueCancel` posts `EventBody.Cancel` from the client
[ ] An undecoded row is skipped on read, and `BootCache` fetches `/state` on a decode error

## 5. Changes

### Change

[ ] One contract: `EventBody.ActorStart` and `EventBody.Cancel` leave, and those bodies are logged as `EventBody.Order`

### History.fs

[ ] Remove `ActorStart` and `Cancel` from `EventBody`
[ ] The `EventBody` module gains `ActorStart (start: ActorStart)`, which is `EventBody.Order (Order.StartActor start)`
[ ] The module gains `(|ActorStart|_|)`
[ ] `(|ActorStart|_|)` gives `start` for `Order.StartActor start` and `Order.StartPeerActor(_, start)`
[ ] `(|ActorStart|_|)` gives `request.start` for `Order.LoadSave(_, _, request)`
[ ] The module gains `Cancel (focusId: NodeId)` and `(|Cancel|_|)` over `Order.Cancel`

### EventJson.fs

[ ] Remove the `actorStart` and `cancel` body kinds from `encodeBody` and `decodeBody`
[ ] Remove `encodeActorStart`; `encodeStartRequest` and `decodeStartRequest` stay for the order codec

### MailboxLog.fs

[ ] Remove the `ActorStart` and `Cancel` arms from `orderOfBody`
[ ] `historyBody` maps every order to `HistoryBody.Order order`

### Size

[ ] 3 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`
[ ] 1 test file: `EventJsonTests.fs`

## 6. Tests

### Old rows

[ ] Add `An old actorStart or cancel row is skipped on read` in `EventJsonTests.fs`
[ ] A JSON body with `kind` `actorStart` or `cancel` fails `EventJson.decode`
[ ] Filter: `An old actorStart or cancel row is skipped on read`

### Kept

[ ] Keep `Ev JSON round-trips ActorStart and ActorStop`
[ ] Keep `cancel request JSON round-trips Focus NodeId and EventId`
[ ] Keep `ActorStart adds and ActorStop removes a live Focus`
[ ] Keep every test in `CancelByFocusTests.fs`
[ ] Filter: `Ev JSON round-trips ActorStart and ActorStop`
[ ] Filter: `cancel request JSON round-trips Focus NodeId and EventId`
[ ] Filter: `ActorStart adds and ActorStop removes a live Focus`
[ ] Filter: `CancelByFocusTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
