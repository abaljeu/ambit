# 108 — Log Change as EventBody.Order

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [107 — Add EventBody.Order](107-add-eventbody-order.md)

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `EventBody.Change`, `Ev.withOps`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeBody`, `decodeBody`, `decodeChangeBody`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`, `historyBody`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `editEvent`, read only; it builds the stored Ev through `MailboxLog.toEv`

### Match sites and constructors

[ ] `EventBody.Change` appears in 13 source files and 63 test files
[ ] Constructors in source: `ClientHistory.mintChange`, `ImportText` (3), `GraphOnlyChangePost`, `RunAgentActor`, `TestActor` (2), `Ev.withOps`, `EventJson.decodeChangeBody`, `MailboxLog.historyBody`
[ ] Patterns in source: `Ev.ops`, `Ev.target`, `Ev.withOps`, `ActorLive.applyEvent`, `ActorLive.resultOf`, `CoreEventDispatch.completeAction`, `CoreEventDispatch.pushChangeSpecials`, `CoreMailboxApi.postedMailbox`, `CoreMailboxApi.cursorAfterDuplicateChange`, `Api.inferredNodeEvents`, `EventJson.encodeBody`, `MailboxLog.orderOfBody`
[ ] Test files construct and match `EventBody.Change` 269 times
[ ] The `EventBody.Change` helper module keeps every one of those sites as written

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips a Change`, `mintChange encodes eventId 0 on the wire`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `postEvents places Order.Change with origin Edit`, `a resubmitted Change replies the stored id`
[ ] [BootCacheTests.fs](tests/Shared.Tests/BootCacheTests.fs) `decideBootRead fetches /state on decode error`

## 4. Context

### Logged Change

[ ] The stored Change is `EventBody.Change ops`
[ ] `placeOrder` builds it through `MailboxLog.toEv`, and `historyBody` maps `Order.Change(ops, _)` to `EventBody.Change ops`
[ ] The stored Change drops `origin`
[ ] `Ev.withOps` already keeps `origin` for `EventBody.Order`

### Old logs

[ ] `EventLogFile.readAllEvents` skips an entry that does not decode
[ ] `DbAgent` restore skips a row that does not decode
[ ] `BootCache` fetches `/state` on a decode error
[ ] Alan, 2026-10-24: old-form logs are dropped, not mapped

### Helper module

[ ] F# allows a module with the same name as a type
[ ] `EventBody.Change ops` then calls the module function
[ ] A pattern `EventBody.Change ops` then uses the module's partial active pattern
[ ] A match stays complete when it lists `EventBody.Order` and the other union cases
[ ] `TreatWarningsAsErrors` is on, so an arm that can never match fails the build

## 5. Changes

### Change

[ ] One contract: the `EventBody.Change` case leaves, and a Change is logged as `EventBody.Order` of `Order.Change`

### History.fs

[ ] Remove `| Change of ops: Op list` from `EventBody`
[ ] Add `[<RequireQualifiedAccess>] module EventBody` after the type
[ ] `EventBody.Change (ops: Op list) : EventBody` is `EventBody.Order (Order.Change(ops, ChangeOrigin.Edit))`
[ ] `EventBody.(|Change|_|)` matches `EventBody.Order (Order.Change(ops, _))` and gives `ops`
[ ] Remove the `EventBody.Change` arm from `Ev.withOps`; the `EventBody.Order` arm keeps `origin`

### EventJson.fs

[ ] Remove the `EventBody.Change` arm from `encodeBody`
[ ] `encodeBody` writes a Change as `kind` `order`
[ ] Remove `decodeChangeBody` and the `change` body kind from `decodeBody`
[ ] An old `change` row does not decode

### MailboxLog.fs

[ ] Remove the `HistoryBody.Change` arm from `orderOfBody`; the `HistoryBody.Order` arm reads it
[ ] `historyBody` maps `Order.Change _` to `HistoryBody.Order order`
[ ] `editEvent` then stores `EventBody.Order placed.order` with its `origin`

### Size

[ ] 3 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`
[ ] 2 test files: `EventJsonTests.fs`, `CoreMailboxDoorTests.fs`

## 6. Tests

### Parse origin

[ ] Add `a stored Parse Change keeps origin Parse` in `CoreMailboxDoorTests.fs`
[ ] `postGraphOnly` of a Change stores `EventBody.Order (Order.Change(ops, ChangeOrigin.Parse))`
[ ] `postEvents` of a Change stores origin `Edit`
[ ] Filter: `a stored Parse Change keeps origin Parse`

### Old row

[ ] Add `An old change row is skipped on read` in `EventJsonTests.fs`
[ ] A JSON body with `kind` `change` fails `EventJson.decode`
[ ] Filter: `An old change row is skipped on read`

### Kept

[ ] Keep `Ev JSON round-trips a Change`, `mintChange encodes eventId 0 on the wire`, `ofEv then toEv keeps a Change`, `postEvents places Order.Change with origin Edit`, `a resubmitted Change replies the stored id`, `decideBootRead fetches /state on decode error`
[ ] Filter: `Ev JSON round-trips a Change`
[ ] Filter: `mintChange encodes eventId 0 on the wire`
[ ] Filter: `ofEv then toEv keeps a Change`
[ ] Filter: `postEvents places Order.Change with origin Edit`
[ ] Filter: `a resubmitted Change replies the stored id`
[ ] Filter: `decideBootRead fetches /state on decode error`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
