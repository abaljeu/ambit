# Scope: 06 — Log ActorOrder

Mode: implement. Ticket: [06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md). Branch: `staging`.

## Signatures

[ ] `src/Shared/History.fs` lines 80–93 — `type Order =` with `Change`, `Undo`, `Redo`, `StartActor`, `StartPeerActor`, `LoadSave`, `Load`, `CancelActor`; no `ActorOrder` case yet.

[ ] `src/Shared/History.fs` lines 26–31 — `type ActorStart = { zoomId; focusId; actorNodeId; graphIds; eventId }`.

[ ] `src/Server/Core/CoreActorPool.fs` lines 18–25 — `type GraphCarrier = unit -> Graph`; `type FunctionRun = { getGraph; secret }`; `type FunctionActor = FunctionRun -> CoreChangesDoor -> Async<unit>`.

[ ] `src/Server/Core/CoreActorPool.fs` lines 28–32 — `type FunctionStart = { actor; getGraph; focusId; actorNodeId }`.

[ ] `src/Server/Core/CoreActorPool.fs` line 54 — `startFunction: FunctionStart -> Result<Credential, string>` on `CoreActorPool`.

[ ] `src/Server/Core/CoreActorPool.fs` lines 249–256 — `runStartFunction` / `runStartFunction putLive getModel (request: FunctionStart)`.

[ ] `src/Server/Core/CoreChanges.fs` line 5 — `type Credential = Credential of string`.

[ ] `src/Server/Core/CoreChangesDoor.fs` lines 4–7 — `type CoreChangesDoor = { host; caller; writable }`.

[ ] `src/Server/Core/CoreDoor.fs` lines 14–17 — `create writable host caller : CoreChangesDoor`.

[ ] `src/Shared/MailboxLog.fs` lines 3–7 — `type PlacedOrder = { orderId; authority; commandName; order }`.

[ ] `src/Shared/EventJson.fs` lines 304–338 — `private encodeOrder (order: Order)`.

[ ] `src/Shared/EventJson.fs` lines 392–404 — `private decodeOrder: Decoder<Order>`.

[ ] `src/Server/Core/CoreMailboxLoop.fs` lines 191–196 — `private placeOrder (context) (placed) (caller) (reply)`.

[ ] `src/Server/Core/MailboxHost.fs` lines 36–37 — `internal postItem (host) (item: CoreQueue)`.

[ ] `src/Server/Core/CoreMsg.fs` lines 72–77 — `type internal CoreQueue = Public of Mailbox | Notice | DropAuthority | Shutdown`.

[ ] [plan/actor-as-client/arch.md](../arch.md) lines 62–66 — planned `type ActorOrder = { actor; getGraph; focusId; actorName }` (no `actorNodeId` field).

[ ] [plan/actor-as-client/arch.md](../arch.md) lines 33–37 — planned `type ActorChanges = { postChanges; actorStop; getState; loadNode }`.

[ ] [plan/actor-as-client/arch.md](../arch.md) lines 31 — planned `type FunctionActor = FunctionRun -> ActorChanges -> Async<unit>`.

[ ] No `ActorOrder`, `ActorChanges`, `stubActor`, `encodeActorOrder`, `decodeActorOrder`, `postActorOrder`, or `CoreDoor.actorChanges` in source on `staging` yet.

## Match sites

[ ] `src/Shared/History.fs` — `EventBody.Change`, `Undo`, `Redo`, `Load`, `ActorStart`, `Cancel` constructors and active patterns match `Order` cases (lines 102–151).

[ ] `src/Shared/EventJson.fs` — `encodeOrder` and `decodeOrder` match every current `Order` case (lines 304–338, 340–404).

[ ] `src/Shared/MailboxLog.fs` — `historyBody` matches `Order.Change`, `Undo`, `Redo`, `Load`, `StartActor`, `CancelActor` as supported; `StartPeerActor` and `LoadSave` as `Error "unsupported"` (lines 26–36).

[ ] `src/Shared/MailboxLog.fs` — `orderOfBody` maps `EventBody.Order` to `Ok order` (lines 20–24).

[ ] `src/Server/Core/CoreMailboxLoop.fs` — `placeOrder` matches `Load`, `Change`, `Undo`, `Redo`, `CancelActor`; unsupported arms call `replyUnsupported` for `StartActor`, `StartPeerActor`, `LoadSave` (lines 197–212).

[ ] `src/Server/Core/CoreMailboxLoop.fs` — `dispatchMailbox` routes `Mailbox.PlaceOrder` to `placeOrder` (lines 262–269).

[ ] `src/Server/Core/CoreMailboxApi.fs` — `cancelByFocus` builds `order = Order.CancelActor focusId` and `Mailbox.placeForCaller` (lines 405–418).

[ ] `src/Server/Core/CoreMailboxApi.fs` — `messageForPosted` / `acceptOne` use `MailboxLog.placedOrderFromEvent` then `Mailbox.placeForCaller` (lines 91–109).

[ ] `src/Server/Core/CoreEventDispatch.fs` — `postEvent` path matches `Order.Change`, `Undo`, `Redo`; other orders refused (lines 146–150, 247–249).

[ ] `src/Server/Core/CoreActorPool.fs` — `runSchedule` matches `FunctionPending` and invokes `actor { getGraph; secret } coreChanges` (lines 304–312).

[ ] `src/Server/Core/CoreMailboxActors.fs` — `startChosen` calls `context.pool.startFunction` (lines 87–91).

[ ] `src/Server/TestActor.fs` — `functionActor: FunctionActor` (line 108).

[ ] `src/Server/SearchActor.fs` — `functionStart` builds `FunctionStart` with inline `FunctionActor` lambda (lines 71–90).

## Existing tests

[ ] `tests/Shared.Tests/EventJsonTests.fs` — ``Ev JSON round-trips each EventBody.Order case`` (lines 360–399) encodes and decodes every current `Order` case including `StartActor`, `StartPeerActor`, `LoadSave`, `Load`, `CancelActor`.

[ ] `tests/Shared.Tests/MailboxLogTests.fs` — ``placed order then logged event keeps a Change`` checks `placedOrderFromEvent` and `eventFromPlacedOrder` for `Order.Change`.

[ ] `tests/Shared.Tests/MailboxLogTests.fs` — ``Undo and Redo keep the target and the ops`` matches `Order.Undo` / `Order.Redo`.

[ ] `tests/Shared.Tests/MailboxLogTests.fs` — ``Load keeps its node through EventJson and MailboxLog`` round-trips `Order.Load` through EventJson and MailboxLog.

[ ] `tests/Shared.Tests/MailboxLogTests.fs` — ``StartPeerActor carries Shared PeerActorName`` pattern-matches `Order.StartPeerActor`.

[ ] `tests/Shared.Tests/MailboxLogTests.fs` — ``placed order reads EventBody.Order`` reads `Order.Change` with Parse origin.

[ ] `tests/Server.Tests/CancelByFocusTests.fs` — ``cancelByFocus places Order.CancelActor`` asserts placed order uses `Order.CancelActor`.

[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — ``StartActor with live credentials calls startActor with ActorStart`` exercises `StartActor` CoreMsg path (not `Order.StartActor` placement).

[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — ``postEvents places Order.Change with origin Edit`` and Undo/Redo tests assert logged `Order` bodies via MailboxLog.

[ ] `tests/Server.Tests/MailboxExpandTests.fs` — ``Mailbox is CoreMsg, PlaceOrder, and Ask`` constructs `Mailbox.PlaceOrder`.

[ ] No existing test names ``ActorOrder round-trips on the wire with stubActor`` or ``ActorOrder carries command name and FocusId`` in the repo.

## Copy-from

[ ] `src/Shared/EventJson.fs` — `encodeOrder` / `decodeOrder` for a new `Order.ActorOrder` arm; wire kinds today include `"change"`, `"undo"`, `"redo"`, `"startActor"`, `"startPeerActor"`, `"loadSave"`, `"load"`, `"cancel"`.

[ ] `src/Shared/EventJson.fs` lines 299–302 — `encodeCancel` uses `"kind"` `"cancel"` and `"focusId"` (same wire kind after 112 — Place Cancel through PlaceOrder).

[ ] `src/Shared/MailboxLog.fs` lines 31–34 — `Order.StartActor` / `Order.CancelActor` arm pattern in `historyBody` (add `Order.ActorOrder` beside them).

[ ] `src/Server/Core/CoreMailboxLoop.fs` lines 210–212 — unsupported `placeOrder` arms use `replyUnsupported reply` (pattern for new `Order.ActorOrder` arm).

[ ] `src/Server/Core/CoreMailboxApi.fs` lines 19–20 — `postNotice` posts via `MailboxHost.postItem host (CoreQueue.Notice msg)`.

[ ] `src/Server/Core/CoreMailboxApi.fs` lines 405–418 — `cancelByFocus` shows `MailboxHost.postMailboxAndAsyncReply` + `Mailbox.placeForCaller` with a built `PlacedOrder` (adapt for `postActorOrder : ActorOrder -> unit` on `MailboxHost`).

[ ] `src/Server/Core/MailboxHost.fs` lines 36–37 — `postItem` is the low-level queue post behind `postNotice` and CoreMsg adds.

[ ] `src/Server/Core/CoreDoor.fs` lines 34–40 — `callIfReady` and `postEvents` gate on `door.writable` (model for `ActorChanges.postChanges` returning `Error` when not writable).

[ ] `src/Server/Core/CoreActorPool.fs` lines 304–312 — `runSchedule` invokes `FunctionActor` (after move, wrap with `CoreDoor.actorChanges coreChanges` in this file only).

## implement

### Ticket claims vs code

[ ] [06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) Input says `Order` has `Cancel`; on `staging` the case is `CancelActor of focusId: NodeId` (112 — Place Cancel through PlaceOrder has landed).

[ ] Ticket Input unchecked: `Order` has no `ActorOrder` — matches code.

[ ] Ticket Input checked: `GraphCarrier`, `FunctionRun`, `FunctionActor` in `CoreActorPool.fs` — matches code.

[ ] Ticket Input checked: `Credential` in `CoreChanges.fs` — matches code.

[ ] Ticket: `FunctionActor` becomes `FunctionRun -> ActorChanges -> Async<unit>`; code today is `FunctionRun -> CoreChangesDoor -> Async<unit>` at `CoreActorPool.fs` line 25.

[ ] Ticket blocker [112 — Place Cancel through PlaceOrder](../../../core-refinement/issues/112-place-cancel-through-placeorder.md) is satisfied on `staging` (`Order.CancelActor`, wire `"cancel"`).

[ ] [17 — Rename commandId to actorNodeId](../issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md): `actorNodeId` is on `ActorStart` and `FunctionStart`; `EventJson.decodeStartRequest` reads wire field `"commandId"` (lines 62–63).

[ ] Planned `ActorOrder` has `focusId` and `actorName` only on the wire; no `actorNodeId` on `ActorOrder` ([arch.md](../arch.md) lines 62–66).

[ ] `ActorOrder`, `Order.ActorOrder`, `stubActor`, and codec functions are absent from code (ticket adds them).

### New tests at public seams

[ ] ``ActorOrder round-trips on the wire with stubActor`` (`EventJsonTests.fs`): build `Ev` with `EventBody.Order (Order.ActorOrder …)`, round-trip through `EventJson.encode` / `EventJson.decode` (same `roundTrip` helper as line 11–14); after decode, `actor` field equals `stubActor` and `focusId` / `actorName` match; extend or mirror the order list in ``Ev JSON round-trips each EventBody.Order case``.

[ ] ``ActorOrder carries command name and FocusId`` (`MailboxLogTests.fs`): post an `Ev` with non-empty `commandName` and `Order.ActorOrder` carrying that focus `NodeId`; `MailboxLog.placedOrderFromEvent` yields `commandName` and order with matching `focusId`; `eventFromPlacedOrder` round-trips.

### Type move and stop-condition files

[ ] `GraphCarrier` — `src/Server/Core/CoreActorPool.fs` lines 18–18; `src/Server/SearchActor.fs` line 73; `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` line 22.

[ ] `FunctionRun` — `src/Server/Core/CoreActorPool.fs` lines 21–23 only in source.

[ ] `FunctionActor` — `src/Server/Core/CoreActorPool.fs` lines 25, 29, 83; `src/Server/TestActor.fs` line 108; inline lambda in `src/Server/SearchActor.fs` lines 76–87.

[ ] `Credential` definition — `src/Server/Core/CoreChanges.fs` line 5; uses in src: `RouteAuthentication.fs`, `SearchActor.fs`, `TestActor.fs`, `CoreActorPool.fs`, `CoreMailboxActors.fs`, `CoreMailboxApi.fs`, `CoreRuntime.fs`, `BrowserRequestCreds.fs` (all already `open Gambol.Shared`).

[ ] Moving `Credential` to `History.fs` likely needs no src edits outside ticket Outputs if the type lives in `Gambol.Shared` and callers keep `open Gambol.Shared`.

[ ] Changing `FunctionActor` to `ActorChanges` forces edits outside ticket Outputs: `src/Server/TestActor.fs` (`functionActor` and `CoreDoor.postEvents` / `CoreChangesDoor` usage) and `src/Server/SearchActor.fs` (`functionStart` lambda uses `CoreDoor.forCaller` and `CoreDoor.actorStop` on the second parameter).

[ ] Stop-condition: those two Server modules are not in [06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) Outputs; implementer must reconcile (minimal adapter vs ticket stop rule).

### CoreDoor and pool

[ ] `CoreDoor` today: `create`, `forCaller`, `getState`, `getEventId`, `getEventsSince`, `isReady`, `postEvents`, `postGraphOnly`, `actorStop`, `loadNode` — no `actorChanges` (full module `src/Server/Core/CoreDoor.fs`, 49 lines).

[ ] Writable gate: `callIfReady` returns `readOnlyResult ()` when `not door.writable` (lines 34–38); message `"Database persistence is unavailable; file fallback is read-only."`.

[ ] `startFunction` signature stays `FunctionStart -> Result<Credential, string>` when types move; callers: `CoreActorPool.create` field, `CoreMailboxActors.startChosen`, `tests/Server.Tests/QueryActorTests.fs`, `CoreActorPoolFunctionStartTests.fs`, and test stubs setting `startFunction = fun _ -> Error "unused"`.

[ ] `runSchedule` at lines 296–312 must pass `ActorChanges` to `FunctionPending` actors after the signature change (wrap with new `CoreDoor.actorChanges` in `CoreActorPool.fs`).

### Wire, log, mailbox

[ ] `MailboxLog` unsupported pattern: `| Order.StartPeerActor _ | Order.LoadSave _ -> Error "unsupported"` (lines 35–36).

[ ] `placeOrder` unsupported pattern: `| Order.StartActor _ | Order.StartPeerActor _ | Order.LoadSave _ -> replyUnsupported reply` (lines 210–212).

[ ] `EventJson` uses `Thoth.Json.Core` and `Thoth.Json.JavaScript`; `Encode.object` / `Encode.string` produce values consumed as `IEncodable` (see `encodeEventBatch : EventBatch -> IEncodable` at line 443).

[ ] Focus type is `NodeId` (`focusId` fields on `ActorStart`, `FunctionStart`, planned `ActorOrder`); no separate `FocusId` type alias in code.

[ ] `stubActor` is decode-only; wire-read `ActorOrder` is a log record and must not be executed (ticket Do not load).

[ ] `postActorOrder` is new on `MailboxHost`; copy `postItem` + `CoreQueue.Public (Mailbox.PlaceOrder …)` pattern from `cancelByFocus` / `mbLoadNode`, or fire-and-forget variant the ticket specifies.

### Line counts and tests project

[ ] Line counts: `History.fs` 571; `EventJson.fs` 454; `CoreActorPool.fs` 417; `CoreChanges.fs` 63; `CoreDoor.fs` 49; `MailboxLog.fs` 60; `CoreMailboxLoop.fs` 470; `MailboxHost.fs` 95 — none at or over 800 lines.

[ ] `tests/Shared.Tests/Gambol.Shared.Tests.fsproj` already includes `MailboxLogTests.fs` (line 31) and `EventJsonTests.fs` (line 35); new facts in those files need no fsproj edit.

### Arch type sketches (unchecked)

[ ] From [arch.md](../arch.md) lines 25–37:

```fsharp
type GraphCarrier = unit -> Graph
type FunctionRun = { getGraph: GraphCarrier; secret: Credential }
type FunctionActor = FunctionRun -> ActorChanges -> Async<unit>
type ActorChanges = { postChanges; actorStop; getState; loadNode }
```

[ ] From [arch.md](../arch.md) lines 44–48:

```fsharp
type FunctionStart = { actor; getGraph; focusId; actorNodeId }
```

[ ] From [arch.md](../arch.md) lines 62–76:

```fsharp
type ActorOrder = { actor; getGraph; focusId; actorName }
| ActorOrder of ActorOrder
```

[ ] From [arch.md](../arch.md) lines 164–176:

```fsharp
stubActor : FunctionActor
encodeActorOrder : ActorOrder -> IEncodable
decodeActorOrder : string -> Result<ActorOrder, string>
```

[ ] From [arch.md](../arch.md) lines 186–194:

```fsharp
actorChanges : CoreChangesDoor -> ActorChanges
postActorOrder : ActorOrder -> unit
```
