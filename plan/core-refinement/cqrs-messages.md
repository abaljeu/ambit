# Message shape

Updated: 2026-10-24

Alan, 2026-10-19. This note is the end-game spec of the mailbox and the event log. §2 **Target shape** is the end state, as decided on 2026-10-24. A line that starts with "Open decision:" is not decided yet. Scope: this note, and one pointer from [core-refinement architecture](arch.md). The map, [Document-state events](spec.md), the tickets in §5 **Tickets 44–57**, and code stay. §7 **Tickets 59–68** indexes the §4 migration.

[core-refinement architecture](arch.md) §10 **Core loop** stays the record of today's queue. [core-refinement](map.md) decision 15 **InMsg on the mailbox queue**, decision 16 **SnapshotDone with Persisted**, and decision 19 **Node events replace document-state Set ops** stay that same record.

## 1. Problem

`CoreMsg` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) mixes orders and queries. An order carries a `Caller` and an `AsyncReplyChannel`. A query carries an `AsyncReplyChannel` and no `Caller`. `Login`, `Logout`, and `AdmitCaller` sit on that same queue today. They leave it. Decision 8 **Session leaves**.

1. **Orders** — `PostEvent`, `PostGraphOnly`, `StartActor`, `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `LoadNode`, `ActorStop`, `CancelActor`.
2. **Queries** — `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, `EventsSince`.

`Notice` is `NodeParsed`, `NodePersisted` of `nodeId`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` is a second door onto that same queue.

`EventBody` in [History.fs](../../src/Shared/History.fs) is the flat union `Change of Op list`, `Undo of EventId * Op list`, `Redo of EventId * Op list`, `ActorStart of ActorStart`, `ActorStop of NodeId * ActorResult`, `Load of NodeId`, `Cancel of NodeId`, and `Notice of Notice`. `Ev` holds `id`, `submissionId`, `authority`, `commandName`, and `body`. `MailboxEventBody` in MailboxLog.fs is `Order of PlacedOrder | Notice of Notice`.

History `EventBody = Notice | CoreMsg` is not a valid log type. `CoreMsg.PostEvent` holds an `Ev`. That `Ev` holds an `EventBody`. A body that includes `CoreMsg` includes that `Ev` again. The `Caller` and the reply are the envelope. The envelope is never logged. The payload is logged.

## 2. Target shape

Alan, 2026-10-24. This section is the end state. It is what the code holds after [104 — PlacedOrder is the mailbox envelope](issues/104-placedorder-is-the-mailbox-envelope.md) through [118 — Test Actors return their result](issues/118-test-actors-return-their-result.md), with [102 — Place Parse changes as Order.Change](issues/102-place-parse-changes-as-order-change.md), [65 — Place Undo and Redo as orders](issues/65-undo-and-redo-resolve-their-ops-from-the-target.md), [79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md), [80 — Place Actor starts through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md), [81 — Log Actor and search stops as Notices](issues/81-log-actor-and-search-stops-as-notices.md), [68 — Remove the CoreMsg case](issues/68-remove-the-coremsg-case.md), and the persist-stop tickets [45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed](issues/45-remove-document-state-set-ops.md), [119 — The persist thread posts Notice.PersistStopped](issues/119-the-persist-thread-posts-notice-persiststopped.md), and [120 — NodePersisted and PersistFailed leave Notice](issues/120-nodepersisted-and-persistfailed-leave-notice.md). Where §3 or a ticket disagrees with this block, this block wins.

`ChangeOrigin`, `Order`, `PeerActorName`, `PersistResult`, `Notice`, `EventBody`, and `Ev` are Shared in [History.fs](../../src/Shared/History.fs) ([106 — Move Order beside EventBody](issues/106-move-order-beside-eventbody.md) moves `ChangeOrigin`, `Order`, and `PeerActorName` there from [MailboxLog.fs](../../src/Shared/MailboxLog.fs)). `PlacedOrder` is Shared in [MailboxLog.fs](../../src/Shared/MailboxLog.fs). `OrderId` is Shared in [OrderId.fs](../../src/Shared/OrderId.fs). `Caller`, `Query`, `Mailbox`, and `CoreQueue` are server-only. `Caller` is in [CoreChanges.fs](../../src/Server/Core/CoreChanges.fs). `Query`, `Mailbox`, and `CoreQueue` are in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs). `CoreMsg`, `TrackingDetails`, `TrackingOrder`, `MailboxEventBody`, and `MailboxLog.Ev` do not exist.

```fsharp
// Shared, History.fs
type ChangeOrigin =
    | Edit
    | Parse

type Order =
    | Change of ops: Op list * origin: ChangeOrigin
    | Undo of target: EventId * ops: Op list
    | Redo of target: EventId * ops: Op list
    | StartActor of ActorStart
    | StartSearch of ActorStart
    | StartPeerActor of peerName: PeerActorName * request: ActorStart
    | LoadSave of
        path: LoadSavePath *
        peerName: PeerActorName *
        request: LoadSaveCommandRequest
    | Load of nodeId: NodeId
    | Save of nodeId: NodeId
    | Cancel of focusId: NodeId
    // Open decision: CancelSearch of rootId: NodeId. Not added yet.

type PersistResult =
    | Written of persistedVersion: EventId * updateTime: System.DateTime
    | Failed of message: string

type Notice =
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodeParsed of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | PersistStopped of nodeId: NodeId * result: PersistResult
    | ActorStopped of focusId: NodeId * result: ActorResult
    | SearchStopped of rootId: NodeId

type EventBody =
    | Order of Order
    | Notice of Notice

type Ev = {
    id: EventId
    submissionId: System.Guid
    authority: Authority
    commandName: string
    body: EventBody }

// Shared, MailboxLog.fs
type PlacedOrder = {
    orderId: OrderId
    authority: Authority
    commandName: string option
    order: Order }

// Server, CoreChanges.fs
type Caller = {
    authority: Authority
    name: string
    secret: Credential }

// Server, CoreMsg.fs
type internal Query =
    | State of AsyncReplyChannel<Result<State, string>>
    | LastEventId of AsyncReplyChannel<Result<EventId, string>>
    | EventsSince of after: EventId * AsyncReplyChannel<Result<EventLog, string>>

type internal Mailbox =
    | PlaceOrder of PlacedOrder * Caller * AsyncReplyChannel<Result<EventId, string>>
    | Ask of Query
    | Notice of Notice

type internal CoreQueue =
    | Public of Mailbox
    | DropAuthority of stopped: StoppedAdmission
    | Shutdown of AsyncReplyChannel<unit>
```

`Session` is outside the core mailbox. Login registers an authority and opens admission. Admit checks whether that caller may start a route. Logout stops admitting and queues `CoreQueue.DropAuthority` behind orders already on the queue. The route returns at that post. The core removes the authority when it processes that case. At Receive, `CoreMailboxLoad` rejects an order whose current generation `DropAuthority` closed. `PlaceOrder` replies `Error` with a reason. Nothing is applied or logged. A caller with an open generation still runs. Login returns `Ok`. A login while admission is closed opens a new generation, and the queued drop closes only the generation logout queued. `Shutdown` asks the loop to exit after that item.

1. **Change** — `Order.Change` of `ops: Op list * origin: ChangeOrigin` replaces `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly`. `ChangeOrigin` is `Edit` or `Parse`. `CoreEventDispatch` reads that `origin`. The logged Change holds the op list and the `origin`.
2. **Undo and Redo** — `Order.Undo` and `Order.Redo` carry the target `EventId` and the Op list. Accept fills an empty Op list from the inverse ops of the target. The logged Undo or Redo holds the target and the filled Op list.
3. **StartActor** — `Order.StartActor` of `ActorStart` replaces `CoreMsg.StartActor`. `Order.StartSearch` of `ActorStart` replaces `CoreMsg.RecordSearchStart`. `StartSearch` records the start and does not run the walk.
4. **StartPeerActor** — `Order.StartPeerActor` replaces `CoreMsg.StartPeerActor`. It carries `PeerActorName` and `ActorStart`.
5. **LoadSave** — `Order.LoadSave` replaces `CoreMsg.StartLoadSaveCommand`. It carries `LoadSavePath`, `PeerActorName`, and `LoadSaveCommandRequest`. A Desk Load or Save runs no Actor. Its handler logs the start, then `Notice.ActorStopped` of its focus and `ActorSucceeded`. That stays as [80 — Place Actor starts through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md) has it until a Desk start and stop runs through an Actor.
6. **Load and Save** — `Order.Load` of `NodeId` replaces `CoreMsg.LoadNode`. `Order.Save` of `NodeId` is git Save ([79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md)).
7. **Cancel** — `Order.Cancel` of the focus `NodeId` replaces `CoreMsg.CancelActor` and the old History `EventBody.Cancel`. A Cancel of a live run logs `Notice.ActorStopped` of that focus and `ActorCancelled`. Names follow `Order.Cancel*`. Open decision: `Order.CancelSearch` of the root `NodeId` is not added. Its future consumers are the Cancel button of the server-mode Search dialog and the cancel of an Amble search-query run. Neither exists yet.
8. **Notice** — The five Node cases are `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, and `NodePersisting`. Persist just stops, like an Actor or a search. The persist thread posts one `Notice.PersistStopped` of the node and a `PersistResult` per write. `Written` carries the version of the graph written to file and the update time; it applies the written-version rule. `Failed` carries the write error; persist state stays, so the node stays Unpersisted, and the client shows the error. Decision 12 **PersistStopped**. `Notice.ActorStopped` of the focus `NodeId` and the `ActorResult` replaces `CoreMsg.ActorStop`. An Actor sends no stop. It just stops. The pool sees the Actor's work end and posts `Notice.ActorStopped` with the notice function. A Cancel logs it too. Exactly one stop is logged per run. `Notice.SearchStopped` of the root `NodeId` replaces `CoreMsg.RecordSearchStop`. The server posts it when a search ends. Names follow `Notice.*Stopped`. `MarkUnparsed` is not a `Notice`. `Notice.NodeUnpersisted` is not added: `CoreEventDispatch.pushChangeSpecials` already marks Unpersisted and logs `Notice.NodePersisting` for Change, Undo, and Redo.
9. **EventBody** — History `EventBody` is `Order of Order | Notice of Notice`. The seven old cases (`Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, `Load`, `Cancel`) leave one at a time. `Ev` keeps `id`, `submissionId`, `authority`, and `commandName` (choice (c)). For an order, accept assigns `id`, sets `submissionId` from `orderId`, `authority` from the `Caller`'s `Authority`, and `commandName` from the `PlacedOrder` (`None` is `""`). A `Notice` `Ev` has a fresh `submissionId`, an empty `authority`, and an empty `commandName`. `MailboxEventBody` and `EventBody` are one type, and `MailboxLog.Ev` leaves ([116 — One EventBody type, and MailboxLog.Ev leaves](issues/116-one-eventbody-type-and-mailboxlog-ev-leaves.md)).
10. **Mailbox** — `Mailbox` is the mailbox. It receives `PlaceOrder`, `Ask`, and `Notice`, and logs orders and notices. `PlaceOrder` carries the Shared `PlacedOrder`, with the server `Caller` and the reply beside it. The reply is `Result<EventId, string>`. `Ask` is a query: `State`, `LastEventId`, and `EventsSince`. `Ask` is not logged. `Mailbox.Notice` replaces `CoreQueue.Notice` ([115 — Notice is a Mailbox case](issues/115-notice-is-a-mailbox-case.md)). Only the notice function builds it.
11. **Who sends** — A client or an Actor places an `Order`. A client sends an Order, such as `Order.Cancel`. It receives a Notice. Only a holder of the notice function posts a `Notice`. The pool and the search code hold it for the stops.
12. **One Ev** — Each accepted `PlaceOrder` becomes one `Ev`. Each `Notice` becomes one `Ev`. That `Ev` is what clients receive. A Notice starts no client catch-up ([77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](issues/77-rename-snapshotdone-and-notices-start-no-catch-up.md)). Stops are no exception.
13. **Old logs** — Old-form log rows are dropped. A row of an old body kind does not decode, and the log reader skips it.
14. **Leaves** — `CoreMsg`, `Mailbox.CoreMsg`, `TrackingDetails`, `TrackingOrder`, `CoreQueue.Notice`, `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, and `toEv` leave. `MarkUnparsed` leaves `Notice`. `Notice.SnapshotDone` became `Notice.NodePersisted`, and `Notice.NodePersisted` and `Notice.PersistFailed` leave for `Notice.PersistStopped`. §6 **SnapshotDone**.

## 3. Decisions

1. **Logged as placed** — The log stores the order as placed. History `EventBody.Order` holds the placed `Order` unchanged, `origin` included. Undo and Redo are logged with their target and their filled Op list. The placed `order` and the logged `order` are the same value. The `Caller` and the reply are never logged.
2. **Load is logged** — `Load n` is logged as an `Order`. Its effects follow as ordinary entries: `NodeParsing n`, Changes to `n` and descendants, then `NodeParsed n`. Load does more than push onto the parse stack. The Load `Order` itself does not post a Node notice. `NodeParsing` stays a later entry.
3. **Ask waits** — A query stays on the mailbox as `Ask`. It waits until any prior `PlaceOrder` has completed.
4. **One Change** — `PostGraphOnly` and `PostEvent` merge into one `Change`. `Order.Change` is `ops: Op list * origin: ChangeOrigin`. `ChangeOrigin` is `Edit` or `Parse`. `CoreEventDispatch` reads that `origin`. The logged Change holds the op list and the `origin`. `CoreEventDispatch.pushChangeSpecials` marks each owning special node Unpersisted and logs `Notice.NodePersisting` for Change, Undo, and Redo (Alan, 2026-10-24). Undo keeps that notice as its own log entry. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. Parse posts its Changes, then `NodeParsed`. The client places an `Order`. Document state is a server `Notice`. Decision 18 **ChangeOrigin**.
5. **SnapshotDone** — There is no whole-graph snapshot on the log or on the wire. The trace and the target are §6 **SnapshotDone**. Today the case is `Notice.NodePersisted` of the node, the written version, and the update time ([99 — NodePersisted carries the written version](issues/99-nodepersisted-carries-the-written-version.md)). The end state is `Notice.PersistStopped` with `Written` (decision 12 **PersistStopped**).
6. **PlaceOrder reply** — Alan, 2026-10-19. `PlaceOrder` replies `Result<EventId, string>`. An accepted order is assigned a number (`Ok` of `EventId`). A rejected order gives a reason (`Error` of `string`). Nothing else rides on the reply. `CoreChangesAccepted` is not part of it.
7. **Ask reply** — Alan, 2026-10-19. Each `Query` case carries its own reply channel. `Ask` carries the `Query` and no reply of its own. The three queries are `State`, `LastEventId`, and `EventsSince`. `GetEventsSince`, `EventsSince`, and `GetEventHistory` are that one `EventsSince`. History is events since zero.
8. **Session leaves** — Alan, 2026-10-19. `Login`, `Logout`, and `Admit` touch neither the graph nor actors. Login and Admit stay off the core mailbox. [RouteRegistration.fs](../../src/Server/RouteRegistration.fs) stops admitting that caller and queues `CoreQueue.DropAuthority` behind orders already on the queue. The logout route returns at that post. `CoreMailboxLoad` removes the authority when it processes `DropAuthority`. Earlier orders on that queue run first, including while the database startup prelude is still serving `Ask`. `Session.waitForAdmittedRoutes` is how a caller can observe open routes, and the logout reply does not call it: orders already queued are already ahead of the drop. `Login` and `Admit` need no barrier. Alan, 2026-10-20: the logout route returns when the drop is queued. At Receive, `CoreMailboxLoad` rejects a caller whose current generation `DropAuthority` closed. The reply is `Error` "authority dropped". A caller with an open generation still runs. Login returns `Ok`. A login while admission is closed opens a new generation, and that drop closes only the generation logout queued. `PlaceOrder` uses that reply. A `CoreMsg` order uses the same reason and skips apply and the log. An Actor order stays held by the live pool secret. An order already ahead of `DropAuthority` still runs.
9. **Admission at the entry** — Alan, 2026-10-19. Exactly two kinds of poster may place an order: an Actor and a logged-in user. Permission is checked once, at the entry point. `RouteRegistration` checks a logged-in user before the order. An Actor gets its permission when it starts (admitted from the pool at start). At Receive, `CoreMailboxLoad` rejects an order whose current generation `DropAuthority` closed. `PlaceOrder` replies `Error`. Nothing is applied or logged. The core reads `authority` when it applies an order it still holds. `admitCaller` inside `CoreEventDispatch.postEvent` is the call that moves. The target shape does not include that call. The notice function is the second, private door. Holding that function is the permission to post a `Notice`. The core hands the function out. It does not admit the holder again on each post.
10. **Order, not Command** — Alan, 2026-10-19. A Command is the user operation a person invokes. [GLOSSARY](../../GLOSSARY.md) keeps that meaning. A Command is the source of a message, not the message. The client payload type is `Order`. The target log body is History `EventBody` (§2 decision 9 **EventBody**).
11. **commandName** — Alan, 2026-10-19. `commandName` comes from `CommandEntry.name`. Only an order that a Command started has one. It is not transmitted universally. `PlacedOrder` carries `commandName: string option`. Accept copies it onto `Ev.commandName`, with `None` as `""`. A `Notice` has no `commandName`; its `Ev.commandName` is `""`.
12. **PersistStopped** — Alan, 2026-10-24. Persist just stops, like an Actor or a search. The persist thread posts `Notice.PersistStopped` of `nodeId: NodeId * result: PersistResult`. `PersistResult` is `Written of persistedVersion: EventId * updateTime: DateTime` or `Failed of message: string`. It replaces `Notice.NodePersisted` and `Notice.PersistFailed`. `Written` does what `NodePersisted` does: the server and the client apply the written-version rule and the update time. `Failed` does what `PersistFailed` does: persist state stays, the node stays Unpersisted, `noteWriteFailed` runs, and the client shows the message as the command error. `PersistStopped` is a Notice, so it starts no client catch-up (§2 item 12 **One Ev**). `Notice.NodeUnpersisted` is not added. [64 — Place Edit changes as Order.Change](issues/64-merge-postevent-and-postgraphonly-into-change.md) logs `Notice.NodePersisting` as the Unpersisted mark. The steps are [45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed](issues/45-remove-document-state-set-ops.md), [119 — The persist thread posts Notice.PersistStopped](issues/119-the-persist-thread-posts-notice-persiststopped.md), and [120 — NodePersisted and PersistFailed leave Notice](issues/120-nodepersisted-and-persistfailed-leave-notice.md). The `Op.SetDocumentState` and `MarkUnparsed` removal is [121 — SetDocumentState and MarkUnparsed leave](issues/121-remove-document-state-set-ops-and-markunparsed.md).
13. **PeerActorName** — Alan, 2026-10-19. `PeerActorName` moves to Shared. `Order.StartPeerActor` and `Order.LoadSave` carry it.
14. **Stored replay** — Alan, 2026-10-19. Stored replay skips the Node notices and `Notice.PersistStopped`, and marks Unpersisted. [55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md).
15. **Notice has its own door** — Alan, 2026-10-19. Only a holder of the notice function can post a `Notice`. Today's `CoreMailboxApi.postNotice` is that function. The core hands it out, for example to the parse thread, the persist thread, reconcile, the Actor pool, the search code, and an Actor given a `Notice` handle. Alan, 2026-10-24: the mailbox receives Order, Ask, and Notice. The function posts `Mailbox.Notice` ([115 — Notice is a Mailbox case](issues/115-notice-is-a-mailbox-case.md)); until 115, it posts `CoreQueue.Notice`. No route builds `Mailbox.Notice`. Both an `Order` and a `Notice` become an `Ev` of History `EventBody`.
16. **Order carries its envelope fields** — Alan, 2026-10-19, revised 2026-10-24. `authority`, the order id, and `commandName` belong to an order, not to its body. While placed they sit on the Shared `PlacedOrder` `{ orderId; authority; commandName; order }`. The `Caller` and the reply ride beside it in `Mailbox.PlaceOrder of PlacedOrder * Caller * AsyncReplyChannel<Result<EventId, string>>`. They are not fields of `PlacedOrder`. When logged, they sit on `Ev` as `submissionId`, `authority`, and `commandName`, and `EventBody.Order` holds the bare `Order` (choice (c)). A `Notice` has none of them. Accept assigns `id`. The reply stays `Result<EventId, string>`.
17. **Expand inside Mailbox** — Alan, 2026-10-19. Expand added `Mailbox` with today's `CoreMsg` as a case, beside `PlaceOrder` and `Ask`. `CoreQueue.Public` carries `Mailbox` ([60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)). Change moves messages one at a time from `CoreMsg` cases to `PlaceOrder`, queries to `Ask`, and stops to Notices. Collapse removes the `CoreMsg` case and the `CoreMsg` type.
18. **ChangeOrigin** — Alan, 2026-10-20. `ChangeOrigin` is `Edit | Parse` beside `Order` (in [History.fs](../../src/Shared/History.fs) after [106 — Move Order beside EventBody](issues/106-move-order-beside-eventbody.md)). `Order.Change` is `ops: Op list * origin: ChangeOrigin`. `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks` place `Change` with origin `Parse`. Every other `Change` poster places origin `Edit`. `CoreEventDispatch` reads that `origin` and writes document files for origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. The logged Change holds the op list and the `origin` (Alan, 2026-10-24). `commandName` is the command label on `PlacedOrder`.
19. **One case each** — Alan, 2026-10-21. Each `CoreMsg` case moves to its own case. The two stops are `Notice` cases. Every other case is an `Order` case. Cases are not merged on the move. Every accepted order and every notice appends an `Ev`. The `PlaceOrder` reply is `Ok` of the order's `EventId`.
20. **Type cleanup** — Alan, 2026-10-24. History `EventBody` ends as `Order of Order | Notice of Notice`, and `Ev` keeps `submissionId`, `authority`, and `commandName` (choice (c)). The Shared `PlacedOrder` stays in Shared as written. Names follow `Order.Cancel*` and `Notice.*Stopped`; existing `EventBody` cases are not renamed. `Order.Undo` and `Order.Redo` carry `target * ops`. Old-form logs are dropped. An Actor sends no stop; it just stops, and the pool posts `Notice.ActorStopped`, which tells the client it can drop its hold on the Actor. A search just stops; the server posts `Notice.SearchStopped`. The rule of [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](issues/77-rename-snapshotdone-and-notices-start-no-catch-up.md) covers stops. The same-focus race in [117 — The pool posts Notice.ActorStopped when an Actor ends](issues/117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) is a known limit, accepted for now.

## 4. Migration

Expand, then change, then collapse. No step breaks upload, import, parse, Load, undo, or persist.

1. [x] **Expand** — Added `Mailbox` with today's `CoreMsg` as a case. `CoreQueue.Public` carries `Mailbox` ([60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)). The notice function posts a `Notice` onto `CoreQueue` beside `Mailbox`. The shape on staging, 2026-10-24:

```fsharp
type internal Mailbox =
    | CoreMsg of CoreMsg
    | PlaceOrder of TrackingOrder
    | Ask of Query

type internal CoreQueue =
    | Public of Mailbox
    | Notice of Notice
    | DropAuthority of stopped: StoppedAdmission
    | Shutdown of AsyncReplyChannel<unit>
```

[104 — PlacedOrder is the mailbox envelope](issues/104-placedorder-is-the-mailbox-envelope.md) makes `PlaceOrder` the §2 shape.

2. [ ] **Change** — Move messages one at a time from `CoreMsg` cases to `PlaceOrder`, queries to `Ask`, and stops to Notices. Move the old History `EventBody` cases one at a time into `EventBody.Order` and `EventBody.Notice`.
3. [ ] **Collapse** — Remove the `CoreMsg` case and the `CoreMsg` type. Then make `MailboxEventBody` and `EventBody` one type, and remove `MailboxLog.Ev`.

Tickets for these three steps are §7 **Tickets 59–68**, which also indexes 102–118.

## 5. Tickets 44–57

These tickets name `Notice` and `CoreMsg`.

1. **[44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md)** — Handlers for `Notice.NodeUploading`, `Notice.NodeReceived`, `Notice.NodeParsing`, `Notice.NodePersisting`, and `Notice.NodePersisted` apply those cases. The effects in [Document-state events](spec.md) stay.
2. **[45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed](issues/45-remove-document-state-set-ops.md)** — Rewritten 2026-10-24. Expand for decision 12 **PersistStopped**. The `Op.SetDocumentState` and `Notice.MarkUnparsed` removal moved to 121 unchanged.
3. **[99 — NodePersisted carries the written version](issues/99-nodepersisted-carries-the-written-version.md)** — `Notice.NodePersisted` becomes `NodeId * EventId * DateTime`: the node, the version of the graph written to file, and the update time. The version check is [100 — Core loop gates Persisted on the written version](issues/100-core-loop-gates-persisted-on-the-written-version.md). These replace [46 — Record the persist version](issues/46-record-the-persist-version.md).
4. **[47 — Undo and redo set Unpersisted](issues/47-undo-and-redo-set-unpersisted.md)** — Undo and Redo still set Unpersisted. The logged `order` is `Order.Undo` or `Order.Redo` of the target `EventId` and the filled Op list.
5. **[48 — Browser applies Node events](issues/48-browser-applies-node-events.md)** — The Browser applies the `Ev` log. Logged Node notices are `Notice` values. The client receives every `Ev`.
6. **[49 — Rename NoServerFile to PullingFile](issues/49-rename-noserverfile-to-pullingfile.md)** — This rename does not change the message sum.
7. **[50 — Upload emits NodeUploading](issues/50-upload-emits-nodeuploading.md)** — The upload post becomes `Notice.NodeUploading`.
8. **[71 — Persist thread takes from the stack](issues/71-persist-thread-takes-from-the-stack.md)** — The finish post becomes `Notice.NodePersisted`. `SnapshotDone` is the misnamed persist-finished signal, not a second snapshot. §6 **SnapshotDone**.
9. **[52 — Log stack pulls](issues/52-log-stack-pulls.md)** — The pull posts become `Notice.NodeParsing` and `Notice.NodePersisting`.
10. **[53 — Arrival and reconcile emit NodeReceived](issues/53-arrival-and-reconcile-emit-nodereceived.md)** — Those posts become `Notice.NodeReceived`.
11. **[54 — Parse and import stop the Parsed Set](issues/54-parse-and-import-stop-the-parsed-set.md)** — The finish post becomes `Notice.NodeParsed`. The Parsed `Op.SetDocumentState` still leaves.
12. **[55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md)** — Stored replay skips `Notice.PersistStopped` like the Node notices and marks Unpersisted. Decision 14 **Stored replay**.
13. **[56 — Remove ParseThread markUnparsed](issues/56-remove-parsethread-markunparsed.md)** — `ParseThread.markUnparsed` still leaves. `MarkUnparsed` is not a `Notice`.
14. **[57 — Upload Actor posts Node notices](issues/57-upload-actor-posts-document-state.md)** — The core gives the upload Actor the notice function. The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. `Api.postEvents` still does not infer those two from the ops of a client `Change`.
15. **[58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md)** — Deletes the dead flush, the `FileAgent` no-op, and the `Graph` payload. Renames the live follow-up file diff. Does not take the version check from [46 — Record the persist version](issues/46-record-the-persist-version.md).

## 6. SnapshotDone

The server writes individual document files. It does not write a whole-graph snapshot. `Database.initSchema` drops the `snapshots` table. `DocumentPersistChange.persistGraphChange` diffs two graphs and writes the document files that differ. `persistGraphOps` writes the document files for an op list. Both jobs share `PersistThread.finishWhenBound`.

1. **finishWhenBound** — Live. After a successful Ops or Change write it posts `Notice.NodePersisted` of the node id. A failed write does not post `Notice.NodePersisted`. The stored word `snapshotDone` still decodes as that case. The encoder writes `nodePersisted`. `PersistThread.afterWrite` posts one `Notice.PersistFailed` per submitted node, with that node id and the message, through `CoreMailboxApi.postNotice`. `CoreMailboxLoad.applyNotice` does not change the persist state. The order reply is success and its `message` carries the write error. The graph on a successful write is the stamped in-memory graph from the file writer.
2. **applyNotice** — Live. The `Notice.NodePersisted` case calls `persist.noteDiffDone`, then `GraphMutate.setPersistState` with `PersistState.Persisted`. This is the only core-loop writer of Persisted. `DocumentPersistPath.stampNode` also sets Persisted on the graph the writer returns, and both agents store that stamped graph as state. A missing persist field decodes as Persisted. That decode is not a post-write set.
3. **handleSnapshotDone** — The batch count is live. The graph match is dead. It copies the payload into `baselineGraph` only when `GraphProjection.graphEquals` matches the live graph, then calls `countDiffMark`. On the ops path `commitPostChange` already stored that graph. On the Change path the stamped graph has new `updateTime` values, so `graphEquals` fails and the copy does not run.
4. **countDiffMark** — Live. Misnamed. It counts down `diffIds` and `diffMarksLeft`. At zero it clears `diffInProgress` and may start another batch.
5. **requestFileDiff** — Live. Misnamed. It does nothing until `bindPersistFinished` fills `persistFinished`. Otherwise it calls `startFileDiff`.
6. **startFileDiff** — Live. Misnamed. It queues `PersistKind.Change` for workspace ids the ops write did not already notify and that are not Unparsed. That job calls `persistGraphChange`. It does not write a snapshot blob.
7. **Batch fields** — Live. Misnamed. `diffIds`, `diffMarksLeft`, `diffWorkspaces`, `diffNeeded`, and `diffInProgress` are that follow-up file diff. `opsNotifiedIds` is the same batch, and that name already says what it holds.
8. **baselineGraph** — Live. Misnamed. It is the baseline graph for the next file diff. Ops commit, a graph-only post, and projection reload update it. It is not a snapshot file.
9. **enqueueFileDiff** — Live. Misnamed. `startFileDiff` uses it for the workspace diff.
10. **FileAgent noteDiffDone** — Dead. The filling sets `noteDiffDone` to `fun _ _ -> ()`.
11. **flushSnapshot** — Dead. `DbAgent` and `FileAgent` return `Ok ()`. `SavePrep.syncDataDir` calls it only when `DbStatus` is not `Ok`. When the database is `Ok`, save reads the state event id and does not call it. `RouteRegistration.prepareGitSave` and `RouteRegistration.flushForGit` only pass the function in.
12. **bindPersistFinished** — Live. Misnamed. `CoreMailbox.hostPublishing` passes `CoreMailboxApi.postNotice`. Each agent stores that poster in `persistFinished`. `finishWhenBound` posts only when the slot is filled.
13. **PersistHandlers.noteDiffDone** — Live slot. Misnamed. `DbAgent` sets `handleSnapshotDone`. `FileAgent` sets the no-op. `CoreMailboxLoad.failedPersist` replaces it with a no-op while writes are refused.
14. **Graph payload** — Dead. `applyNotice` sets Persisted from the node id. `FileAgent` ignores the graph. The only reader is the equality copy in `handleSnapshotDone`, and that copy does not keep a graph the ops path has not already stored.

The live case is `Notice.NodePersisted` of `nodeId`. [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](issues/77-rename-snapshotdone-and-notices-start-no-catch-up.md) did that rename. A Notice on a reply or a poll does not set `externalChanges` and does not start a browser catch-up. [99 — NodePersisted carries the written version](issues/99-nodepersisted-carries-the-written-version.md) added the written version and the update time, so the shape is `nodeId * persistedVersion: EventId * updateTime: DateTime`. The `Graph` leaves the message. The dead flush, the `FileAgent` no-op, and the equality copy leave. The follow-up file diff stays, under names that say file diff. [58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md) does the deletion and the renames. [46 — Record the persist version](issues/46-record-the-persist-version.md) adds the version check. Upload, import, parse, Load, undo, and persist stay working.

## 7. Tickets 59–68

These tickets are the §4 migration. Each one keeps upload, import, parse, Load, undo, and persist working. [57 — Upload Actor posts Node notices](issues/57-upload-actor-posts-document-state.md) stays as written.

1. **[59 — Rename InMsg to Notice](issues/59-rename-inmsg-to-notice.md)** — Expand. `Notice` keeps the case names. `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` posts a `Notice`.
2. **[60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)** — Expand. `Mailbox` is `CoreMsg of CoreMsg | PlaceOrder | Ask`. `Notice` is not a `Mailbox` case. `Order`, `Query`, `PlacedOrder`, `MailboxEventBody`, and `Ev = { id; body }` sit beside today's types, with mapping functions. Callers stay on today's types. `PeerActorName` moves to Shared. Blocked by [59 — Rename InMsg to Notice](issues/59-rename-inmsg-to-notice.md).
3. **[61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md)** — Change. `Login`, `Logout`, and `Admit` move to `Session`. Logout stops admitting the caller and queues `CoreQueue.DropAuthority`. The core removes the authority when it processes that case. Blocked by [63 — Move queries to Ask](issues/63-move-queries-to-ask.md) and [62 — Admit at the entry](issues/62-admit-at-the-entry.md).
4. **[62 — Admit at the entry](issues/62-admit-at-the-entry.md)** — Change. The route checks a logged-in user before an order. An Actor is admitted when it starts. `CoreEventDispatch.postEvent` does not call `admitCaller`. The core reads authority. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md) only. The order is [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md), then [63 — Move queries to Ask](issues/63-move-queries-to-ask.md), then [62 — Admit at the entry](issues/62-admit-at-the-entry.md), then [61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md).
5. **[63 — Move queries to Ask](issues/63-move-queries-to-ask.md)** — Change. `GetState`, `GetEventId`, and the three event reads become `Query.State`, `Query.LastEventId`, and `Query.EventsSince`. History is events since zero. Each query has its own reply. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
6. **[64 — Place Edit changes as Order.Change](issues/64-merge-postevent-and-postgraphonly-into-change.md)** and **[102 — Place Parse changes as Order.Change](issues/102-place-parse-changes-as-order-change.md)** — Change. Those two doors become `Order.Change` of ops and `ChangeOrigin`. 64 adds the origin and moves origin `Edit`; 102 moves origin `Parse`, which keeps the file skip, and removes `PostGraphOnly`. The logged Change holds the op list. The Unpersisted mark stays the `Notice.NodePersisting` that `pushChangeSpecials` logs. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
7. **[65 — Place Undo and Redo as orders](issues/65-undo-and-redo-resolve-their-ops-from-the-target.md)** — Change. The Undo and Redo bodies are placed as `Order.Undo` and `Order.Redo` of the target and the Op list. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md) and [64 — Place Edit changes as Order.Change](issues/64-merge-postevent-and-postgraphonly-into-change.md).
8. **[79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md)**, **[80 — Place Actor starts through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md)**, and **[81 — Log Actor and search stops as Notices](issues/81-log-actor-and-search-stops-as-notices.md)** — Change. git Save places `Order.Save`. `StartActor` becomes `Order.StartActor`. `StartPeerActor` becomes `Order.StartPeerActor`. `StartLoadSaveCommand` becomes `Order.LoadSave`. A stop is logged as `Notice.ActorStopped`, and a search stop as `Notice.SearchStopped`. `EventBody.ActorStop` leaves. 81 also has `applyLoggedNotice` call `ActorLive.applyEvent`, so a stop starts no catch-up. The order is §7 item 11.
9. **[67 — Log Load as Order.Load](issues/67-log-load-as-order-load.md)** — Change. `LoadNode` becomes `Order.Load` and is logged. The handler pushes the parse stack and posts no notice. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
10. **[68 — Remove the CoreMsg case](issues/68-remove-the-coremsg-case.md)** — Collapse. The `CoreMsg` case and the `CoreMsg` type leave. `Ev` keeps `submissionId`, `authority`, and `commandName` (choice (c)). Blocked by [61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md), [62 — Admit at the entry](issues/62-admit-at-the-entry.md), [63 — Move queries to Ask](issues/63-move-queries-to-ask.md), [64 — Place Edit changes as Order.Change](issues/64-merge-postevent-and-postgraphonly-into-change.md), [65 — Place Undo and Redo as orders](issues/65-undo-and-redo-resolve-their-ops-from-the-target.md), [79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md), [80 — Place Actor orders through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md), [81 — Log Actor and search stops as Notices](issues/81-log-actor-and-search-stops-as-notices.md), and [67 — Log Load as Order.Load](issues/67-log-load-as-order-load.md).

11. **Type cleanup, 2026-10-24** — These tickets reach §2. The order is [104 — PlacedOrder is the mailbox envelope](issues/104-placedorder-is-the-mailbox-envelope.md), [102 — Place Parse changes as Order.Change](issues/102-place-parse-changes-as-order-change.md), [105 — One Ev.withOps in History](issues/105-one-ev-withops-in-history.md), [106 — Move Order beside EventBody](issues/106-move-order-beside-eventbody.md), [107 — Add EventBody.Order](issues/107-add-eventbody-order.md), [108 — Log Change as EventBody.Order](issues/108-log-change-as-eventbody-order.md), [65 — Place Undo and Redo as orders](issues/65-undo-and-redo-resolve-their-ops-from-the-target.md), [109 — Log Undo, Redo, and Load as EventBody.Order](issues/109-log-undo-redo-and-load-as-eventbody-order.md), [79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md), [110 — Log ActorStart and Cancel as EventBody.Order](issues/110-log-actorstart-and-cancel-as-eventbody-order.md), [81 — Log Actor and search stops as Notices](issues/81-log-actor-and-search-stops-as-notices.md), [80 — Place Actor starts through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md), [111 — Place search start as an order, and post SearchStopped](issues/111-place-search-start-as-an-order-and-post-searchstopped.md), [112 — Place Cancel through PlaceOrder](issues/112-place-cancel-through-placeorder.md), [113 — Actors return their ActorResult](issues/113-actors-return-their-actorresult.md), [117 — The pool posts Notice.ActorStopped when an Actor ends](issues/117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md), [118 — Test Actors return their result](issues/118-test-actors-return-their-result.md), [114 — Remove CoreMsg.ActorStop](issues/114-remove-coremsg-actorstop.md), [115 — Notice is a Mailbox case](issues/115-notice-is-a-mailbox-case.md), [68 — Remove the CoreMsg case](issues/68-remove-the-coremsg-case.md), then [116 — One EventBody type, and MailboxLog.Ev leaves](issues/116-one-eventbody-type-and-mailboxlog-ev-leaves.md).
    1. 104: `Mailbox.PlaceOrder` is `PlacedOrder * Caller * reply`. `TrackingDetails` and `TrackingOrder` leave.
    2. 106 and 107: `Order` moves beside `EventBody`, and `EventBody.Order of Order` is added.
    3. 108, 109, and 110: `Change`, `Undo`, `Redo`, `Load`, `ActorStart`, and `Cancel` leave `EventBody` and are logged as `EventBody.Order`. The logged Change keeps `origin`.
    4. 65: `Order.Undo` and `Order.Redo` carry `target * ops`.
    5. 111: `Order.StartSearch` replaces `RecordSearchStart`. The search end posts `Notice.SearchStopped`.
    6. 112: `Order.Cancel` replaces `CoreMsg.CancelActor`.
    7. 113, 117, 118, and 114: Actors return their result, the pool posts `Notice.ActorStopped`, and the sent stop leaves.
    8. 115: `Notice` moves from `CoreQueue` to `Mailbox`.
    9. 116: `MailboxEventBody` and `EventBody` are one type, and `MailboxLog.Ev` leaves.

## 8. Ticket 69

1. **[69 — Persist an Edit without waiting](issues/69-persist-an-edit-without-waiting.md)** — An Edit document write returns before the persist thread finishes. There is no wait field. `PersistThread.afterWrite` posts `Notice.NodeStamped` with the stamp ops and the persist message when that write finishes with a stamped graph. `CoreMailboxLoad.applyNotice` applies those ops. The ack has no stamp ops and no persist message. A poll of `Notice.PersistFailed` sets the browser command error and does not change the graph. This ticket does not post or handle `Notice.NodePersisted` and does not add the version gate. Meant to come before [64 — Place Edit changes as Order.Change](issues/64-merge-postevent-and-postgraphonly-into-change.md). [46 — Record the persist version](issues/46-record-the-persist-version.md) is related and is not a blocker.
