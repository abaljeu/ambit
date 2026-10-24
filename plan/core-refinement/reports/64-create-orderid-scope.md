# 64 — Create OrderId scope

Scope for slice **Create `OrderId` and use it for `Mailbox.PlaceOrder`'s `orderId`**, in context of [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md) (`orderId` maps to Ev `submissionId`). Mode: implement. Branch: `dev`. Source unchanged in this pass.

## Signatures

[ ] `Gambol.Shared.EventId` in [EventId.fs](../../src/Shared/EventId.fs) is a private DU `Zero | Int of int` with `[<RequireQualifiedAccess>]` and module `EventId` (`zero`, `next`, `max`, `value`, `display`, `isAccepted`, `fromJson`, `toJson`).
[ ] `Gambol.Shared.NodeId` in [Model.fs](../../src/Shared/Model.fs) is `[<Struct>] NodeId of Guid` with member `Value`, static `GuidTail8`, and static `New()`.
[ ] No `OrderId` type or module exists in the repo today.
[ ] `Gambol.Shared.Ev` in [History.fs](../../src/Shared/History.fs) is `{ id: EventId; submissionId: System.Guid; authority: Authority; commandName: string; body: EventBody }`.
[ ] `Gambol.Shared.PlacedOrder` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is `{ orderId: System.Guid; authority: Authority; commandName: string option; order: Order }`.
[ ] `Gambol.Server.Mailbox.PlaceOrder` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `caller: Caller * orderId: System.Guid * commandName: string option * order: Order * reply: AsyncReplyChannel<Result<EventId, string>>`.
[ ] `Gambol.Server.CoreMailboxLoad.placeOrder` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; parameters are `context`, `caller`, `orderId` (inferred `Guid`), `commandName`, `order`, `reply`.
[ ] `Gambol.Server.CoreMailboxLoad.dispatchMailbox` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; it matches `Mailbox.PlaceOrder (caller, orderId, commandName, order, reply)` and calls `placeOrder context caller orderId commandName order reply`.
[ ] `Gambol.Server.CoreMailboxLoad.replyLoad` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; signature includes `(orderId: Guid)` and passes `orderId` to `CoreEventDispatch.appendLifecycle` as `submissionId`.
[ ] `Gambol.Server.CoreMailboxLoad.editEvent` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; signature is `(caller: Caller) -> (orderId: Guid) -> (commandName: string option) -> (ops: Op list) -> Result<Ev, string>`.
[ ] `Gambol.Server.CoreMailboxLoad.replyEdit` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; signature includes `(orderId: Guid)`.
[ ] `Gambol.Server.CoreEventDispatch.appendLifecycle` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is `(Context -> Caller -> submissionId: System.Guid -> EventBody -> Result<Ev, string>)`.
[ ] `Gambol.Server.CoreEventDispatch.tryStored` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is private; `(Context -> System.Guid -> Ev option)` keys on `e.submissionId`.
[ ] `Gambol.Shared.MailboxLog.placedOrder` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is private; sets `orderId = event.submissionId`.
[ ] `Gambol.Shared.MailboxLog.toEv` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) sets `submissionId = placed.orderId` on the restored `HistoryEv`.
[ ] `Gambol.Server.CoreMailboxApi.postedMailbox` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is private; builds `Mailbox.PlaceOrder(caller, placed.orderId, placed.commandName, placed.order, channel)`.
[ ] `Gambol.Server.CoreMailboxApi.postEvents` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> Ev list -> Async<Result<CoreChangesAccepted, string>>`; each Change draft routes through `postedMailbox` and thus `PlaceOrder`.
[ ] `Gambol.Server.CoreMailboxApi.mbLoadNode` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> NodeId -> Async<Result<EventId, string>>`; posts `PlaceOrder` with `System.Guid.NewGuid()` as `orderId`.

## Match sites

### `Mailbox.PlaceOrder` case and dispatch

[ ] [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) `Mailbox.PlaceOrder` case declares `orderId: System.Guid`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchMailbox` — `Mailbox.PlaceOrder (caller, orderId, commandName, order, reply)` calls `placeOrder` when `authorityHeld context caller`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `isStartupRead` — `Mailbox.PlaceOrder _` returns `false`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) startup `TryScan` — `CoreQueue.Public message` when `isStartupRead message` calls `dispatchMailbox` (includes `PlaceOrder` during startup reads only for `Ask`).

### `PlaceOrder` constructors and Guid minting

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postedMailbox` — `EventBody.Change _` with `event.id = EventId.zero` and `MailboxLog.ofEv` yields `Order placed`; constructs `Mailbox.PlaceOrder` with `placed.orderId`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `mbLoadNode` — constructs `Mailbox.PlaceOrder(caller, System.Guid.NewGuid(), None, Order.Load subject, channel)` via `MailboxHost.postMailboxAndAsyncReply`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``login during a pending logout runs and a dropped generation rejects a late order`` — `MailboxHost.postMailboxAndAsyncReply` with `Mailbox.PlaceOrder(testCaller, Guid.NewGuid(), Some "late", Order.Change(..., ChangeOrigin.Edit), channel)`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``admitted route order after DropAuthority is rejected`` — same `PlaceOrder` shape with `Guid.NewGuid()`.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — constructs `Mailbox.PlaceOrder(testCaller, Guid.Empty, None, Order.Load Graph.rootId, ...)`.

### `orderId` through `placeOrder` arms

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Load subject` calls `replyLoad context caller orderId subject reply`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Change (ops, ChangeOrigin.Edit)` calls `replyEdit context caller orderId commandName ops reply`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `replyLoad` — passes `orderId` into `CoreEventDispatch.appendLifecycle` as the lifecycle `submissionId`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `editEvent` — sets `PlacedOrder.orderId = orderId` inside `MailboxLog.toEv`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `replyEdit` — passes the same `orderId` into `editEvent` then `CoreEventDispatch.postEvent` (dedup via `event.submissionId`).

### `MailboxLog` Guid bridge (`submissionId` ↔ `orderId`)

[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `placedOrder` — copies `event.submissionId` into `orderId`.
[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `toEv` — copies `placed.orderId` into `submissionId` on `HistoryEv`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postedMailbox` — reads `placed.orderId` from `MailboxLog.ofEv event` before posting `PlaceOrder`.

### Callers of `postEvents`, `dispatchMailbox`, and `mbLoadNode`

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEvents` — maps each `Ev` to `postedMailbox`; Change drafts become `PlaceOrder` with `orderId` from `ofEv`.
[ ] [CoreDoor.fs](../../src/Server/Core/CoreDoor.fs) `postEvents` — delegates to `CoreMailboxApi.postEvents` when writable.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchItem` — `CoreQueue.Public message` calls `dispatchMailbox`.
[ ] [CoreDoor.fs](../../src/Server/Core/CoreDoor.fs) `loadNode` (or equivalent) — calls `CoreMailboxApi.mbLoadNode` ([CoreDoor.fs](../../src/Server/Core/CoreDoor.fs) `callIfReady CoreMailboxApi.mbLoadNode`).
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``loadNode logs Order.Load``, ``loadNode pushes the subject``, ``loadNode of a plain Node replies Error`` — call `CoreMailboxApi.mbLoadNode`.
[ ] [ParseThreadLoadTests.fs](../../tests/Server.Tests/ParseThreadLoadTests.fs) — multiple tests call `CoreMailboxApi.mbLoadNode` (minted server `orderId` per load).

### Test match on `PlaceOrder`

[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) `mailboxKind` — `Mailbox.PlaceOrder _` maps to `"PlaceOrder"`.

## Existing tests

[ ] [MailboxLogTests.fs](../../tests/Shared.Tests/MailboxLogTests.fs) ``ofEv then toEv keeps a Change`` — `Assert.Equal(event.submissionId, order.orderId)` on `PlacedOrder.orderId` (`Guid`).
[ ] [MailboxLogTests.fs](../../tests/Shared.Tests/MailboxLogTests.fs) ``empty command name maps to None and returns as empty`` — uses `placed event` (includes `orderId` from `submissionId`) without asserting `orderId` directly.
[ ] [MailboxLogTests.fs](../../tests/Shared.Tests/MailboxLogTests.fs) ``Undo and Redo keep the target and drop the ops`` — uses `placed` helper (carries `orderId`) without asserting `orderId`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``login during a pending logout runs and a dropped generation rejects a late order`` — constructs `PlaceOrder` with `Guid.NewGuid()` as `orderId`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``admitted route order after DropAuthority is rejected`` — constructs `PlaceOrder` with `Guid.NewGuid()` as `orderId`.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — constructs `PlaceOrder` with `Guid.Empty` as `orderId`.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``postEvent runs through Mailbox CoreMsg`` — posts Change `Ev` with `submissionId = Guid.NewGuid()`; asserts stored `submissionId` matches (exercises `postedMailbox` → `PlaceOrder` path).
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``postEvents places Order.Change with origin Edit`` — posts `postedEvent()`; finds stored event by `event.submissionId` (same value as `PlaceOrder` `orderId` today).
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``a resubmitted Change replies the stored id`` — two `postEvents` with the same `postedEvent()` share `submissionId` / dedup key.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` — uses `storedBody host *.submissionId` (not `PlaceOrder.orderId` field).
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``loadNode logs Order.Load`` — `mbLoadNode` succeeds; stored body is `EventBody.Load` (server-minted `orderId` on `PlaceOrder`).

## Copy-from

[ ] [EventId.fs](../../src/Shared/EventId.fs) module `EventId` — `[<RequireQualifiedAccess>]` wrapper with smart constructors and wire helpers (`fromJson`, `toJson`, `display`); pattern for a named id with a dedicated module (serial int, not Guid).
[ ] [Model.fs](../../src/Shared/Model.fs) type `NodeId` — `[<Struct>]` single-case wrapper around `Guid`, member `Value`, static `New()`; closest copy-from for client-minted Guid identity (not `EventId`, not `NodeId` semantics).
[ ] [EventId.fs](../../src/Shared/EventId.fs) compile entry in [Gambol.Shared.fsproj](../../src/Shared/Gambol.Shared.fsproj) — new `OrderId.fs` should compile in Shared beside `EventId.fs` / before dependents ([Model.fs](../../src/Shared/Model.fs) order is `EventId.fs` then `Model.fs`; `OrderId.fs` fits the same band).

## implement

### Ticket and slice claims that differ from the code

[ ] [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md) describes `PlaceOrder` `orderId` as the Ev `submissionId` but does not name an `OrderId` type; plan docs ([cqrs-messages.md](../cqrs-messages.md) decision 16 **Order carries its envelope fields**) still show `orderId: System.Guid`.
[ ] The slice requires a new `OrderId` type; the codebase has zero matches for `OrderId` in `.fs` or `.md` sources.
[ ] `Mailbox.PlaceOrder` and `PlacedOrder.orderId` both use `System.Guid` today; the slice names `PlaceOrder` first, but `postedMailbox` passes `placed.orderId` into `PlaceOrder`, so [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `PlacedOrder.orderId` must align or convert at that seam.
[ ] `Gambol.Shared.Ev.submissionId` stays `System.Guid` in [History.fs](../../src/Shared/History.fs); `OrderId` converts at `MailboxLog` and `appendLifecycle` / `postEvent` boundaries (`OrderId` ↔ `Guid`), not by changing `Ev` in this slice.
[ ] [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md) section 3 lists `dispatchMailbox` — `PlaceOrder (caller, orderId, _, order, reply)` with `_` for `commandName`; code passes `commandName` into `placeOrder` ([CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) line 468).

### How each named new or changed test observes `OrderId` at a public seam

[ ] No new test name is in the slice; existing tests below must compile against `OrderId` on `Mailbox.PlaceOrder` and likely `PlacedOrder.orderId`.
[ ] ``ofEv then toEv keeps a Change`` — public seam `MailboxLog.ofEv` / `PlacedOrder.orderId`; assert `order.orderId` equals `OrderId` built from `event.submissionId` (or compare via `OrderId`/`Guid` helper).
[ ] ``login during a pending logout runs and a dropped generation rejects a late order`` — public seam `Mailbox.PlaceOrder` via `MailboxHost.postMailboxAndAsyncReply`; pass `OrderId.New()` (or equivalent) instead of `Guid.NewGuid()`.
[ ] ``admitted route order after DropAuthority is rejected`` — same `PlaceOrder` constructor seam as the login-during-logout test.
[ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — public seam `Mailbox.PlaceOrder` DU construction; use `OrderId` from `Guid.Empty` (or dedicated empty helper) instead of raw `Guid.Empty`.
[ ] ``postEvent runs through Mailbox CoreMsg`` — public seam `CoreMailboxApi.postEvent`; behavior unchanged if `submissionId` on `Ev` stays `Guid`; observes dedup still keyed on `submissionId`, not the `PlaceOrder` field type.
[ ] ``postEvents places Order.Change with origin Edit`` — public seam `CoreMailboxApi.postEvents`; stored event still found by `event.submissionId`; `MailboxLog.ofEv` yields `PlacedOrder` whose `orderId` type becomes `OrderId`.
[ ] ``a resubmitted Change replies the stored id`` — public seam `CoreMailboxApi.postEvents`; second post with the same `submissionId` on the input `Ev` still hits `tryStored`; no direct `PlaceOrder` construction in the test.
[ ] ``loadNode logs Order.Load`` — public seam `CoreMailboxApi.mbLoadNode`; server still mints a fresh id per load; implementer replaces `System.Guid.NewGuid()` in `mbLoadNode` with `OrderId.New()` at the `PlaceOrder` post site.
