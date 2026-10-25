# Message shape

This note is the end-game spec of the mailbox and the event log. §2 **Target shape** is the end state. A line that starts with "Open decision:" is not decided yet. Scope: this note, and one pointer from [core-refinement architecture](arch.md). The map, [Document-state events](spec.md), the tickets in §5 **Tickets 44–57**, and code stay. §7 **Tickets 59–68** indexes the §4 migration.

[core-refinement architecture](arch.md) §10 **Core loop** stays the record of today's queue. [core-refinement](map.md) decision 15 **InMsg on the mailbox queue**, decision 16 **SnapshotDone with Persisted**, and decision 19 **Node events replace document-state Set ops** stay that same record.

## 1. Problem

`CoreMsg` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) mixes orders and queries. An order carries a `Caller` and an `AsyncReplyChannel`. A query carries an `AsyncReplyChannel` and no `Caller`. `Login`, `Logout`, and `AdmitCaller` sit on that same queue today. They leave it. Decision 8 **Session leaves**.

1. **Orders** — `PostEvent`, `PostGraphOnly`, `StartActor`, `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `LoadNode`, `ActorStop`, `CancelActor`.
2. **Queries** — `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, `EventsSince`.

`Notice` is `NodeParsed`, `NodePersisted` of `nodeId`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` is a second door onto that same queue.

`EventBody` in [History.fs](../../src/Shared/History.fs) is the flat union `Change of Op list`, `Undo of EventId * Op list`, `Redo of EventId * Op list`, `ActorStart of ActorStart`, `ActorStop of NodeId * ActorResult`, `Load of NodeId`, `Cancel of NodeId`, and `Notice of Notice`. `Ev` holds `id`, `submissionId`, `authority`, `commandName`, and `body`. `MailboxEventBody` in MailboxLog.fs is `Order of PlacedOrder | Notice of Notice`.

History `EventBody = Notice | CoreMsg` is not a valid log type. `CoreMsg.PostEvent` holds an `Ev`. That `Ev` holds an `EventBody`. A body that includes `CoreMsg` includes that `Ev` again. The `Caller` and the reply are the envelope. The envelope is never logged. The payload is logged.

## 2. Target shape

This section is the end state. It will hold after core-refinement completes.  Where §3 or a ticket disagrees with this block, this block wins.

`CoreMsg` is gone, its uses moved into Notice and Order.
`TrackingDetails`, `TrackingOrder` are gone, replaced by Order.
`MailboxEventBody`, and `MailboxLog.Ev` no longer exist.  They were transformed and merged into EventBody and Ev in History.fs.

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

type Notice =
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodeParsed of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | NodePersisted of
        nodeId: NodeId *
        persistedVersion: EventId *
        updateTime: System.DateTime
    | PersistFailed of nodeId: NodeId * message: string
    | ActorStopped of focusId: NodeId * result: ActorResult
    | SearchStopped of rootId: NodeId
    // Open decision: NodeUnpersisted of nodeId: NodeId.

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

1. **Change** — `Order.Change` replaces `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly`. `ChangeOrigin` is `Edit` or `Parse`. `CoreEventDispatch` reads that `origin`. The logged Change holds the op list and the `origin`.
2. **Undo and Redo** — `Order.Undo` and `Order.Redo` carry the target `EventId` and the Op list. Accept fills an empty Op list from the inverse ops of the target. The logged Undo or Redo holds the target and the filled Op list.
3. **StartActor** — `Order.StartActor` of `ActorStart` replaces `CoreMsg.StartActor`. `Order.StartSearch` of `ActorStart` replaces `CoreMsg.RecordSearchStart`. `StartSearch` records the start and does not run the walk.
4. **StartPeerActor** — `Order.StartPeerActor` replaces `CoreMsg.StartPeerActor`. It carries `PeerActorName` and `ActorStart`.
5. **LoadSave** — `Order.LoadSave` replaces `CoreMsg.StartLoadSaveCommand`. It carries `LoadSavePath`, `PeerActorName`, and `LoadSaveCommandRequest`. A Desk Load or Save runs no Actor. Its handler logs the start, then `Notice.ActorStopped` of its focus and `ActorSucceeded`. That stays as [80 — Place Actor starts through PlaceOrder](issues/80-place-actor-orders-through-placeorder.md) has it until a Desk start and stop runs through an Actor.
6. **Load and Save** — `Order.Load` of `NodeId` replaces `CoreMsg.LoadNode`. `Order.Save` of `NodeId` is git Save ([79 — Place git Save as Order.Save](issues/79-place-git-save-as-order-save.md)).
7. **Cancel** — `Order.Cancel` of the focus `NodeId` replaces `CoreMsg.CancelActor` and the old History `EventBody.Cancel`. A Cancel of a live run logs `Notice.ActorStopped` of that focus and `ActorCancelled`. Names follow `Order.Cancel*`. Open decision: `Order.CancelSearch` of the root `NodeId` is not added. Its future consumers are the Cancel button of the server-mode Search dialog and the cancel of an Amble search-query run. Neither exists yet.
8. **Notice** — The six Node cases are `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, `NodePersisting`, and `NodePersisted`. `NodePersisted` carries the node, the version of the graph written to file, and the update time. `PersistFailed` carries the node and the write error. `Notice.ActorStopped` of the focus `NodeId` and the `ActorResult` replaces `CoreMsg.ActorStop`. An Actor sends no stop. It just stops. The pool sees the Actor's work end and posts `Notice.ActorStopped` with the notice function. A Cancel logs it too. Exactly one stop is logged per run. `Notice.SearchStopped` of the root `NodeId` replaces `CoreMsg.RecordSearchStop`. The server posts it when a search ends. Names follow `Notice.*Stopped`. `MarkUnparsed` is not a `Notice`. Open decision: `Notice.NodeUnpersisted`. [45 — SetDocumentState leaves; NodeUnpersisted replaces it](issues/45-remove-document-state-set-ops.md) still adds it. : `CoreEventDispatch.pushChangeSpecials` already marks Unpersisted and logs `Notice.NodePersisting` for Change, Undo, and Redo.
9. **EventBody** — History `EventBody` is `Order of Order | Notice of Notice`. The seven old cases (`Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, `Load`, `Cancel`) leave one at a time. `Ev` keeps `id`, `submissionId`, `authority`, and `commandName` (choice (c)). For an order, accept assigns `id`, sets `submissionId` from `orderId`, `authority` from the `Caller`'s `Authority`, and `commandName` from the `PlacedOrder` (`None` is `""`). A `Notice` `Ev` has a fresh `submissionId`, an empty `authority`, and an empty `commandName`. `MailboxEventBody` and `EventBody` are one type, and `MailboxLog.Ev` leaves ([116 — One EventBody type, and MailboxLog.Ev leaves](issues/116-one-eventbody-type-and-mailboxlog-ev-leaves.md)).
10. **Mailbox** — `Mailbox` is the mailbox. It receives `PlaceOrder`, `Ask`, and `Notice`, and logs orders and notices. `PlaceOrder` carries the Shared `PlacedOrder`, with the server `Caller` and the reply beside it. The reply is `Result<EventId, string>`. `Ask` is a query: `State`, `LastEventId`, and `EventsSince`. `Ask` is not logged. `Mailbox.Notice` replaces `CoreQueue.Notice` ([115 — Notice is a Mailbox case](issues/115-notice-is-a-mailbox-case.md)). Only the notice function builds it.
11. **Who sends** — A client or an Actor places an `Order`. A client sends an Order, such as `Order.Cancel`. It receives a Notice. Only a holder of the notice function posts a `Notice`. The pool and the search code hold it for the stops.
12. **One Ev** — Each accepted `PlaceOrder` becomes one `Ev`. Each `Notice` becomes one `Ev`. That `Ev` is what clients receive. A Notice starts no client catch-up ([77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](issues/77-rename-snapshotdone-and-notices-start-no-catch-up.md)). Stops are no exception.
13. **Old logs** — Old-form log rows are dropped. A row of an old body kind does not decode, and the log reader skips it.
14. **Leaves** — `CoreMsg`, `Mailbox.CoreMsg`, `TrackingDetails`, `TrackingOrder`, `CoreQueue.Notice`, `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, and `toEv` leave. `MarkUnparsed` leaves `Notice`. `Notice.SnapshotDone` became `Notice.NodePersisted`. §6 **SnapshotDone**.

## 3. Decisions

1. **Logged as placed** — The placed `order` and the logged `order` are the same value. History `EventBody.Order` holds that `Order` unchanged.
2. **Load is logged** — `Load n` is logged as an `Order`. Its effects follow as ordinary entries: `NodeParsing n`, Changes to `n` and descendants, then `NodeParsed n`. `NodeParsing` stays a later entry.
3. **Ask waits** — A query stays on the mailbox as `Ask`. It waits until any prior `PlaceOrder` has completed.
4. **One Change** — §2 item 1 **Change**.
5. **SnapshotDone** — There is no whole-graph snapshot on the log or on the wire. The trace and the target are §6 **SnapshotDone**.
6. **PlaceOrder reply** — . An accepted order is `Ok` of that order's `EventId`. A rejected order is `Error` of a `string` reason. `CoreChangesAccepted` is not part of it.
7. **Ask reply** — . Each `Query` case carries its own reply channel. `Ask` carries the `Query` and no reply of its own. `GetEventsSince`, `EventsSince`, and `GetEventHistory` are that one `EventsSince`. History is events since zero.
8. **Session leaves** — . `Login`, `Logout`, and `Admit` touch neither the graph nor actors. [RouteRegistration.fs](../../src/Server/RouteRegistration.fs) stops admitting that caller. Earlier orders on that queue run first, including while the database startup prelude is still serving `Ask`. `Session.waitForAdmittedRoutes` is how a caller can observe open routes, and the logout reply does not call it. `Login` and `Admit` need no barrier. The reply is `Error` "authority dropped". A `CoreMsg` order uses the same reason and skips apply and the log. An Actor order stays held by the live pool secret. An order already ahead of `DropAuthority` still runs.
9. **Admission at the entry** — . Exactly two kinds of poster may place an order: an Actor and a logged-in user. Permission is checked once, at the entry point. `RouteRegistration` checks a logged-in user before the order. An Actor gets its permission when it starts (admitted from the pool at start). `admitCaller` inside `CoreEventDispatch.postEvent` is the call that moves.
10. **Order, not Command** — . A Command is the user operation a person invokes. [GLOSSARY](../../GLOSSARY.md) keeps that meaning. A Command is the source of a message, not the message. The client payload type is `Order`.
11. **commandName** — . `commandName` comes from `CommandEntry.name`. Only an order that a Command started has one.
12. **NodeUnpersisted** — Open decision: §2 item 8 **Notice**.
13. **PeerActorName** — §2.
14. **Stored replay** — . Stored replay skips the Node notices and marks Unpersisted. If `Notice.NodeUnpersisted` is added (decision 12 **NodeUnpersisted**), replay skips it too. [55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md).
15. **Notice has its own door** — . Today's `CoreMailboxApi.postNotice` is that function. The core hands it out, for example to the parse thread, the persist thread, reconcile, the Actor pool, the search code, and an Actor given a `Notice` handle. Until 115, it posts `CoreQueue.Notice`.
16. **Order carries its envelope fields** — , revised 2026-10-24. `authority`, the order id, and `commandName` belong to an order, not to its body. Choice (c): they sit on `PlacedOrder` while placed, and on `Ev` when logged.
17. **Expand inside Mailbox** — . Expand added `Mailbox` with today's `CoreMsg` as a case, beside `PlaceOrder` and `Ask`. Change moves messages one at a time from `CoreMsg` cases to `PlaceOrder`, queries to `Ask`, and stops to Notices. Collapse removes the `CoreMsg` case and the `CoreMsg` type.
18. **ChangeOrigin** — . `ParseThread.postParseOps`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks` place `Change` with origin `Parse`. Every other `Change` poster places origin `Edit`. `CoreEventDispatch` writes document files for origin `Edit`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. Parse posts its Changes, then `NodeParsed`.
19. **One case each** — . Each `CoreMsg` case moves to its own case. The two stops are `Notice` cases. Every other case is an `Order` case. Cases are not merged on the move.
20. **Type cleanup** — . Existing `EventBody` cases are not renamed. `Notice.ActorStopped` tells the client it can drop its hold on the Actor. The same-focus race in [117 — The pool posts Notice.ActorStopped when an Actor ends](issues/117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) is a known limit, accepted for now.

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
