# Message shape

Updated: 2026-10-19

Alan, 2026-10-19. This note is the target shape of the mailbox and the event log. Scope: this note, and one pointer from [core-refinement architecture](arch.md). The map, [Document-state events](spec.md), the tickets in §5 **Tickets 44–57**, and code stay. §7 **Tickets 59–68** indexes the §4 migration.

[core-refinement architecture](arch.md) §10 **Core loop** stays the record of today's queue. [core-refinement](map.md) decision 15 **InMsg on the mailbox queue**, decision 16 **SnapshotDone with Persisted**, and decision 19 **Node events replace document-state Set ops** stay that same record.

## 1. Problem

`CoreMsg` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) mixes orders and queries. An order carries a `Caller` and an `AsyncReplyChannel`. A query carries an `AsyncReplyChannel` and no `Caller`. `Login`, `Logout`, and `AdmitCaller` sit on that same queue today. They leave it. Decision 8 **Session leaves**.

1. **Orders** — `PostEvent`, `PostGraphOnly`, `StartActor`, `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `LoadNode`, `ActorStop`, `CancelActor`.
2. **Queries** — `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, `EventsSince`.

`Notice` is `NodeParsed`, `SnapshotDone`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` is a second door onto that same queue.

`EventBody` in [History.fs](../../src/Shared/History.fs) is `Change of Op list`, `Undo of EventId * Op list`, `Redo of EventId * Op list`, `ActorStart of ActorStart`, `ActorStop of NodeId * ActorResult`, and `Cancel of NodeId`. `Ev` holds `id`, `submissionId`, `authority`, `commandName`, and `body`.

`EventBody = Notice | CoreMsg` is not a valid log type. `CoreMsg.PostEvent` holds an `Ev`. That `Ev` holds an `EventBody`. A body that includes `CoreMsg` includes that `Ev` again. The `Caller` and the reply are the envelope. The envelope is never logged. The payload is logged.

## 2. Target shape

`Order`, `PlacedOrder`, `Notice`, `EventBody`, and `PeerActorName` are Shared. `Mailbox` is the public mailbox and is server-only. `CoreQueue` is server-internal.

```fsharp
type Order =
    | Change of ops: Op list
    | Undo of target: EventId
    | Redo of target: EventId
    | StartActor of ActorStart
    | StartPeerActor of peerName: PeerActorName * request: ActorStart
    | LoadSave of
        path: LoadSavePath *
        peerName: PeerActorName *
        request: LoadSaveCommandRequest
    | Load of nodeId: NodeId
    | Cancel of focusId: NodeId

type Notice =
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodeParsed of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | NodePersisted of nodeId: NodeId * version: EventId
    | NodeUnpersisted of nodeId: NodeId
    | ActorStop of focusId: NodeId * result: ActorResult

type PlacedOrder = {
    orderId: System.Guid
    authority: Authority
    commandName: string option
    order: Order }

type EventBody =
    | Order of PlacedOrder
    | Notice of Notice

type Ev = {
    id: EventId
    body: EventBody }

type Query =
    | State of AsyncReplyChannel<Result<State, string>>
    | LastEventId of AsyncReplyChannel<Result<EventId, string>>
    | EventsSince of after: EventId * AsyncReplyChannel<Result<EventLog, string>>

type Mailbox =
    | PlaceOrder of
        caller: Caller *
        orderId: System.Guid *
        commandName: string option *
        order: Order *
        reply: AsyncReplyChannel<Result<EventId, string>>
    | Ask of Query

type internal CoreQueue =
    | Public of Mailbox
    | Notice of Notice
    | DropAuthority of stopped: StoppedAdmission
```

`Session` is outside the core mailbox. Login registers an authority and opens admission. Admit checks whether that caller may start a route. Logout stops admitting and queues `CoreQueue.DropAuthority` behind orders already on the queue. The route returns at that post. The core removes the authority when it processes that case. At Receive, `CoreMailboxLoad` rejects an order whose current generation `DropAuthority` closed. `PlaceOrder` replies `Error` with a reason. Nothing is applied or logged. A caller with an open generation still runs. Login returns `Ok`. A login while admission is closed opens a new generation, and the queued drop closes only the generation logout queued.

1. **Change** — `Order.Change` of an `Op` list replaces `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly`.
2. **Undo and Redo** — `Order.Undo` and `Order.Redo` carry the target `EventId`. They carry no ops. Today's `EventBody.Undo` and `EventBody.Redo` each carry `EventId * Op list`.
3. **StartActor** — `Order.StartActor` of `ActorStart` replaces `CoreMsg.StartActor` and `CoreMsg.RecordSearchStart`. Both of those cases carry `ActorStart` today. Today `RecordSearchStart` records the start and does not run the walk.
4. **StartPeerActor** — `Order.StartPeerActor` replaces `CoreMsg.StartPeerActor`. It carries `PeerActorName` and `ActorStart`.
5. **LoadSave** — `Order.LoadSave` replaces `CoreMsg.StartLoadSaveCommand`. It carries `LoadSavePath`, `PeerActorName`, and `LoadSaveCommandRequest`.
6. **Load** — `Order.Load` of `NodeId` replaces `CoreMsg.LoadNode`.
7. **Cancel** — `Order.Cancel` of the focus `NodeId` replaces `CoreMsg.CancelActor` and `EventBody.Cancel`.
8. **Notice** — The seven Node cases are `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, `NodePersisting`, `NodePersisted`, and `NodeUnpersisted`. `NodePersisted` carries the launch version as an `EventId`. `NodeUnpersisted` carries `NodeId`. `Notice.ActorStop` of `NodeId * ActorResult` replaces `CoreMsg.ActorStop`, `EventBody.ActorStop`, and `CoreMsg.RecordSearchStop`. `RecordSearchStop` stores `ActorStop` of the root id and `ActorSucceeded` today.
9. **EventBody** — `EventBody` is `Order of PlacedOrder | Notice of Notice`. `EventBody.ActorStart` and `EventBody.ActorStop` leave. `Ev` keeps `id` and `body`. `authority`, `orderId`, and `commandName` belong to the `PlacedOrder`. A `Notice` has none of them. Accept assigns `id`. The `PlacedOrder` copies `orderId` and `commandName` from the `PlaceOrder`, and `authority` from the `Caller`'s `Authority`.
10. **Mailbox** — `Mailbox` is the public mailbox. `PlaceOrder` carries `orderId`, `commandName: string option`, an `order`, the `Caller`, and the reply. The reply is `Result<EventId, string>`. `Ask` is a query: `State`, `LastEventId`, and `EventsSince`. A `Notice` is not a `Mailbox` case. The notice function posts a `Notice` onto `CoreQueue`.
11. **Who sends** — A client or an Actor places an `Order`. Only a holder of the notice function posts a `Notice` onto `CoreQueue`.
12. **One Ev** — Each accepted `PlaceOrder` becomes one `Ev`. Each `Notice` on `CoreQueue` becomes one `Ev`. That `Ev` is what clients receive.
13. **Leaves** — `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` is the notice function. The core hands that function out. `MarkUnparsed` leaves `Notice`. `Notice.SnapshotDone` becomes `Notice.NodePersisted`. The `Graph` payload leaves. §6 **SnapshotDone**.

## 3. Decisions

1. **Logged as placed** — The log stores the simpler message as placed. `Undo target` and `Redo target` are logged without ops. The client and the server resolve that target the same way. The placed `order` and the logged `order` are the same value.
2. **Load is logged** — `Load n` is logged as an `Order`. Its effects follow as ordinary entries: `NodeParsing n`, Changes to `n` and descendants, then `NodeParsed n`. Load does more than push onto the parse stack. The Load `Order` itself does not post a Node notice. `NodeParsing` stays a later entry.
3. **Ask waits** — A query stays on the mailbox as `Ask`. It waits until any prior `PlaceOrder` has completed.
4. **One Change** — `PostGraphOnly` and `PostEvent` merge into one `Change`. Document state is not a flag on that `Change`. It is `Notice.NodeUnpersisted` on the log, because document state does not propagate through event undo. After an ordinary edit, the server's `Change` handler posts `Notice.NodeUnpersisted`. Parse posts its Changes, then `NodeParsed`. The client never sends document state.
5. **SnapshotDone** — There is no whole-graph snapshot on the log or on the wire. The trace and the target are §6 **SnapshotDone**. The target case is `Notice.NodePersisted` of `NodeId * EventId`, the same shape as [46 — Record the persist version](issues/46-record-the-persist-version.md).
6. **PlaceOrder reply** — Alan, 2026-10-19. `PlaceOrder` replies `Result<EventId, string>`. An accepted order is assigned a number (`Ok` of `EventId`). A rejected order gives a reason (`Error` of `string`). Nothing else rides on the reply. `CoreChangesAccepted` is not part of it.
7. **Ask reply** — Alan, 2026-10-19. Each `Query` case carries its own reply channel. `Ask` carries the `Query` and no reply of its own. The three queries are `State`, `LastEventId`, and `EventsSince`. `GetEventsSince`, `EventsSince`, and `GetEventHistory` are that one `EventsSince`. History is events since zero.
8. **Session leaves** — Alan, 2026-10-19. `Login`, `Logout`, and `Admit` touch neither the graph nor actors. Login and Admit stay off the core mailbox. [RouteRegistration.fs](../../src/Server/RouteRegistration.fs) stops admitting that caller and queues `CoreQueue.DropAuthority` behind orders already on the queue. The logout route returns at that post. `CoreMailboxLoad` removes the authority when it processes `DropAuthority`. Earlier orders on that queue run first, including while the database startup prelude is still serving `Ask`. `Session.waitForAdmittedRoutes` is how a caller can observe open routes, and the logout reply does not call it: orders already queued are already ahead of the drop. `Login` and `Admit` need no barrier. Alan, 2026-10-20: the logout route returns when the drop is queued. At Receive, `CoreMailboxLoad` rejects a caller whose current generation `DropAuthority` closed. The reply is `Error` "authority dropped". A caller with an open generation still runs. Login returns `Ok`. A login while admission is closed opens a new generation, and that drop closes only the generation logout queued. `PlaceOrder` uses that reply. A `CoreMsg` order uses the same reason and skips apply and the log. An Actor order stays held by the live pool secret. An order already ahead of `DropAuthority` still runs.
9. **Admission at the entry** — Alan, 2026-10-19. Exactly two kinds of poster may place an order: an Actor and a logged-in user. Permission is checked once, at the entry point. `RouteRegistration` checks a logged-in user before the order. An Actor gets its permission when it starts (admitted from the pool at start). At Receive, `CoreMailboxLoad` rejects an order whose current generation `DropAuthority` closed. `PlaceOrder` replies `Error`. Nothing is applied or logged. The core reads `authority` when it applies an order it still holds. `admitCaller` inside `CoreEventDispatch.postEvent` is the call that moves. The target shape does not include that call. The notice function is the second, private door. Holding that function is the permission to post a `Notice`. The core hands the function out. It does not admit the holder again on each post.
10. **Order, not Command** — Alan, 2026-10-19. A Command is the user operation a person invokes. [GLOSSARY](../../GLOSSARY.md) keeps that meaning. A Command is the source of a message, not the message. The client payload type is `Order`. `EventBody` is `Order of PlacedOrder | Notice of Notice`.
11. **commandName** — Alan, 2026-10-19. `commandName` comes from `CommandEntry.name`. Only an order that a Command started has one. It is not transmitted universally. `PlaceOrder` carries `commandName: string option`. Accept copies `commandName` from the `PlaceOrder` onto the `PlacedOrder`. A `Notice` has no `commandName`.
12. **NodeUnpersisted** — Alan, 2026-10-19. `Notice.SetDocumentState` does not remain. `Notice.NodeUnpersisted` of `nodeId: NodeId` replaces it. After an ordinary edit, the server's `Change` handler posts `Notice.NodeUnpersisted`. There are seven Node notices.
13. **PeerActorName** — Alan, 2026-10-19. `PeerActorName` moves to Shared. `Order.StartPeerActor` and `Order.LoadSave` carry it.
14. **Stored replay** — Alan, 2026-10-19. Stored replay skips `Notice.NodeUnpersisted` like the other Node notices and marks Unpersisted. [55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md).
15. **Notice has its own door** — Alan, 2026-10-19. `Mailbox` is the public mailbox. It does not accept a `Notice`. Only a holder of the notice function can post one. Today's `CoreMailboxApi.postNotice` is that function. The core hands it out, for example to the parse thread, the persist thread, reconcile, and an Actor given a `Notice` handle. The function posts a `Notice` onto the internal queue beside `Mailbox`. `CoreQueue.Public` carries `Mailbox` ([60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)). `PlaceOrder` carries an `order`. The log stays `EventBody = Order of PlacedOrder | Notice of Notice`. Both an `Order` and a `Notice` become an `Ev`.
16. **Order carries its envelope fields** — Alan, 2026-10-19. `authority`, the order id, and `commandName` belong to an `Order`, not to every `Ev`. A `Notice` has none of them. The mailbox case is `PlaceOrder`. Its id field is `orderId`. Its payload field is `order`. The reply stays `Result<EventId, string>`. The logged case is `EventBody.Order` of `PlacedOrder`. Accept assigns `id`. The `PlacedOrder` copies `orderId` and `commandName` from the `PlaceOrder`, and `authority` from the `Caller`'s `Authority`.
17. **Expand inside Mailbox** — Alan, 2026-10-19. Expand adds `Mailbox` with today's `CoreMsg` as a case, beside `PlaceOrder` and `Ask`. `Mailbox` is the public mailbox. A `Notice` is not a `Mailbox` case. `CoreQueue.Public` carries `Mailbox` ([60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)). The notice function posts a `Notice` onto `CoreQueue`. Change moves messages one at a time from `CoreMsg` cases to `PlaceOrder`, and queries to `Ask`. Collapse removes the `CoreMsg` case and the `CoreMsg` type.

## 4. Migration

Expand, then change, then collapse. No step breaks upload, import, parse, Load, undo, or persist.

1. [ ] **Expand** — Add `Mailbox` with today's `CoreMsg` as a case. `Mailbox` is the public mailbox. `CoreQueue.Public` carries `Mailbox` ([60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)). The notice function posts a `Notice` onto `CoreQueue` beside `Mailbox`.

```fsharp
type Mailbox =
    | CoreMsg of CoreMsg
    | PlaceOrder of
        caller: Caller *
        orderId: System.Guid *
        commandName: string option *
        order: Order *
        reply: AsyncReplyChannel<Result<EventId, string>>
    | Ask of Query

type internal CoreQueue =
    | Public of Mailbox
    | Notice of Notice
```

2. [ ] **Change** — Move messages one at a time from `CoreMsg` cases to `PlaceOrder` (and queries to `Ask`).
3. [ ] **Collapse** — Remove the `CoreMsg` case and the `CoreMsg` type.

Tickets for these three steps are §7 **Tickets 59–68**.

## 5. Tickets 44–57

These tickets name `Notice` and `CoreMsg`.

1. **[44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md)** — Handlers for `Notice.NodeUploading`, `Notice.NodeReceived`, `Notice.NodeParsing`, `Notice.NodePersisting`, and `Notice.NodePersisted` apply those cases. The effects in [Document-state events](spec.md) stay.
2. **[45 — SetDocumentState leaves; NodeUnpersisted replaces it](issues/45-remove-document-state-set-ops.md)** — `Op.SetDocumentState` leaves. The Unpersisted mark is `Notice.NodeUnpersisted`. No `SetDocumentState` case is added. `Notice.MarkUnparsed` still leaves.
3. **[46 — Record the persist version](issues/46-record-the-persist-version.md)** — `Notice.NodePersisted(nodeId, version)` becomes `Notice.NodePersisted` of `NodeId * EventId`. The version check stays.
4. **[47 — Undo and redo set Unpersisted](issues/47-undo-and-redo-set-unpersisted.md)** — Undo and Redo still set Unpersisted. The logged `order` is `Order.Undo` or `Order.Redo` of the target `EventId`, with no ops. The inverse-ops fill on today's `EventBody` is the shape decision 1 replaces.
5. **[48 — Browser applies Node events](issues/48-browser-applies-node-events.md)** — The Browser applies the `Ev` log. Logged Node notices are `Notice` values. The client receives every `Ev`.
6. **[49 — Rename NoServerFile to PullingFile](issues/49-rename-noserverfile-to-pullingfile.md)** — This rename does not change the message sum.
7. **[50 — Upload emits NodeUploading](issues/50-upload-emits-nodeuploading.md)** — The upload post becomes `Notice.NodeUploading`.
8. **[51 — Stand the persist stack](issues/51-stand-the-persist-stack.md)** — The finish post becomes `Notice.NodePersisted`. `SnapshotDone` is the misnamed persist-finished signal, not a second snapshot. §6 **SnapshotDone**.
9. **[52 — Log stack pulls](issues/52-log-stack-pulls.md)** — The pull posts become `Notice.NodeParsing` and `Notice.NodePersisting`.
10. **[53 — Arrival and reconcile emit NodeReceived](issues/53-arrival-and-reconcile-emit-nodereceived.md)** — Those posts become `Notice.NodeReceived`.
11. **[54 — Parse and import stop the Parsed Set](issues/54-parse-and-import-stop-the-parsed-set.md)** — The finish post becomes `Notice.NodeParsed`. The Parsed `Op.SetDocumentState` still leaves.
12. **[55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md)** — Stored replay skips `Notice.NodeUnpersisted` like the other Node notices and marks Unpersisted. Decision 14 **Stored replay**.
13. **[56 — Remove ParseThread markUnparsed](issues/56-remove-parsethread-markunparsed.md)** — `ParseThread.markUnparsed` still leaves. `MarkUnparsed` is not a `Notice`.
14. **[57 — Upload Actor posts Node notices](issues/57-upload-actor-posts-document-state.md)** — The core gives the upload Actor the notice function. The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. `Api.postEvents` still does not infer those two from the ops of a client `Change`.
15. **[58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md)** — Deletes the dead flush, the `FileAgent` no-op, and the `Graph` payload. Renames the live follow-up file diff. Does not take the version check from [46 — Record the persist version](issues/46-record-the-persist-version.md).

## 6. SnapshotDone

The server writes individual document files. It does not write a whole-graph snapshot. `Database.initSchema` drops the `snapshots` table. `DocumentPersistChange.persistGraphChange` diffs two graphs and writes the document files that differ. `persistGraphOps` writes the document files for an op list. Both jobs share `PersistThread.finishWhenBound`.

1. **finishWhenBound** — Live. Misnamed. After a successful Ops or Change write it posts `Notice.SnapshotDone(nodeId, Some graph)`. A failed write does not post. The graph is the stamped in-memory graph from the file writer.
2. **applyNotice** — Live. The `SnapshotDone` case calls `persist.noteDiffDone`, then `GraphMutate.setPersistState` with `PersistState.Persisted`. This is the only core-loop writer of Persisted. `DocumentPersistPath.stampNode` also sets Persisted on the graph the writer returns, and both agents store that stamped graph as state. A missing persist field decodes as Persisted. That decode is not a post-write set.
3. **handleSnapshotDone** — The batch count is live. The graph match is dead. It copies the payload into `baselineGraph` only when `GraphProjection.graphEquals` matches the live graph, then calls `countDiffMark`. On the ops path `commitPostChange` already stored that graph. On the Change path the stamped graph has new `updateTime` values, so `graphEquals` fails and the copy does not run.
4. **countDiffMark** — Live. Misnamed. It counts down `diffIds` and `diffMarksLeft`. At zero it clears `diffInProgress` and may start another batch.
5. **requestFileDiff** — Live. Misnamed. It does nothing until `bindPersistFinished` fills `persistFinished`. Otherwise it calls `startFileDiff`.
6. **startFileDiff** — Live. Misnamed. It queues `PersistKind.Change` for workspace ids the ops write did not already notify and that are not Unparsed. That job calls `persistGraphChange`. It does not write a snapshot blob.
7. **Batch fields** — Live. Misnamed. `diffIds`, `diffMarksLeft`, `diffWorkspaces`, `diffNeeded`, and `diffInProgress` are that follow-up file diff. `opsNotifiedIds` is the same batch, and that name already says what it holds.
8. **baselineGraph** — Live. Misnamed. It is the baseline graph for the next file diff. Ops commit, a graph-only post, and projection reload update it. It is not a snapshot file.
9. **enqueueFileDiff** — Live. Misnamed. `startFileDiff` uses it for the workspace diff. `catchUpParsed` uses it for one node that is Parsed and Unpersisted.
10. **FileAgent noteDiffDone** — Dead. The filling sets `noteDiffDone` to `fun _ _ -> ()`.
11. **flushSnapshot** — Dead. `DbAgent` and `FileAgent` return `Ok ()`. `SavePrep.syncDataDir` calls it only when `DbStatus` is not `Ok`. When the database is `Ok`, save reads the state event id and does not call it. `RouteRegistration.prepareGitSave` and `RouteRegistration.flushForGit` only pass the function in.
12. **bindPersistFinished** — Live. Misnamed. `CoreMailbox.hostPublishing` passes `CoreMailboxApi.postNotice`. Each agent stores that poster in `persistFinished`. `finishWhenBound` posts only when the slot is filled.
13. **PersistHandlers.noteDiffDone** — Live slot. Misnamed. `DbAgent` sets `handleSnapshotDone`. `FileAgent` sets the no-op. `CoreMailboxLoad.failedPersist` replaces it with a no-op while writes are refused.
14. **Graph payload** — Dead. `applyNotice` sets Persisted from the node id. `FileAgent` ignores the graph. The only reader is the equality copy in `handleSnapshotDone`, and that copy does not keep a graph the ops path has not already stored.

`SnapshotDone` becomes `Notice.NodePersisted` of `nodeId * version: EventId`. That matches [46 — Record the persist version](issues/46-record-the-persist-version.md). The `Graph` leaves the message. The dead flush, the `FileAgent` no-op, and the equality copy leave. The follow-up file diff stays, under names that say file diff. [58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md) does the deletion and the renames. [46 — Record the persist version](issues/46-record-the-persist-version.md) adds the version check. Upload, import, parse, Load, undo, and persist stay working.

## 7. Tickets 59–68

These tickets are the §4 migration. Each one keeps upload, import, parse, Load, undo, and persist working. [45 — SetDocumentState leaves; NodeUnpersisted replaces it](issues/45-remove-document-state-set-ops.md) and [57 — Upload Actor posts Node notices](issues/57-upload-actor-posts-document-state.md) stay as written.

1. **[59 — Rename InMsg to Notice](issues/59-rename-inmsg-to-notice.md)** — Expand. `Notice` keeps the case names. `CoreQueue` is `Public of CoreMsg | Notice of Notice`. `CoreMailboxApi.postNotice` posts a `Notice`.
2. **[60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md)** — Expand. `Mailbox` is `CoreMsg of CoreMsg | PlaceOrder | Ask`. `Notice` is not a `Mailbox` case. `Order`, `Query`, `PlacedOrder`, the new `EventBody`, and `Ev = { id; body }` sit beside today's types, with mapping functions. Callers stay on today's types. `PeerActorName` moves to Shared. Blocked by [59 — Rename InMsg to Notice](issues/59-rename-inmsg-to-notice.md).
3. **[61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md)** — Change. `Login`, `Logout`, and `Admit` move to `Session`. Logout stops admitting the caller and queues `CoreQueue.DropAuthority`. The core removes the authority when it processes that case. Blocked by [63 — Move queries to Ask](issues/63-move-queries-to-ask.md) and [62 — Admit at the entry](issues/62-admit-at-the-entry.md).
4. **[62 — Admit at the entry](issues/62-admit-at-the-entry.md)** — Change. The route checks a logged-in user before an order. An Actor is admitted when it starts. `CoreEventDispatch.postEvent` does not call `admitCaller`. The core reads authority. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md) only. The order is [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md), then [63 — Move queries to Ask](issues/63-move-queries-to-ask.md), then [62 — Admit at the entry](issues/62-admit-at-the-entry.md), then [61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md).
5. **[63 — Move queries to Ask](issues/63-move-queries-to-ask.md)** — Change. `GetState`, `GetEventId`, and the three event reads become `Query.State`, `Query.LastEventId`, and `Query.EventsSince`. History is events since zero. Each query has its own reply. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
6. **[64 — Merge PostEvent and PostGraphOnly into Change](issues/64-merge-postevent-and-postgraphonly-into-change.md)** — Change. Those two doors become `Order.Change`. After an ordinary edit the `Change` handler posts `Notice.NodeUnpersisted`. A `Parse` change keeps the file skip. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
7. **[65 — Log Undo and Redo as the target](issues/65-log-undo-and-redo-as-the-target.md)** — Change. `Order.Undo` and `Order.Redo` carry the target `EventId` only. The client and the server resolve that target the same way. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
8. **[66 — Move Actor orders onto Order and Notice](issues/66-move-actor-orders-onto-order-and-notice.md)** — Change. `StartActor` and `RecordSearchStart` become `Order.StartActor`. `StartPeerActor` becomes `Order.StartPeerActor`. `StartLoadSaveCommand` becomes `Order.LoadSave`. `CancelActor` becomes `Order.Cancel`. `ActorStop` and `RecordSearchStop` become `Notice.ActorStop`. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md), [63 — Move queries to Ask](issues/63-move-queries-to-ask.md), and [62 — Admit at the entry](issues/62-admit-at-the-entry.md).
9. **[67 — Log Load as Order.Load](issues/67-log-load-as-order-load.md)** — Change. `LoadNode` becomes `Order.Load` and is logged. The handler pushes the parse stack and posts no notice. Blocked by [60 — Add Mailbox beside CoreMsg](issues/60-add-mailbox-beside-coremsg.md).
10. **[68 — Remove the CoreMsg case](issues/68-remove-the-coremsg-case.md)** — Collapse. The `CoreMsg` case and the `CoreMsg` type leave. Today's `EventBody` leaves. `submissionId` leaves `Ev`. `Ev` is `{ id; body }`. Blocked by [61 — Move Session off the core mailbox](issues/61-move-session-off-the-core-mailbox.md), [62 — Admit at the entry](issues/62-admit-at-the-entry.md), [63 — Move queries to Ask](issues/63-move-queries-to-ask.md), [64 — Merge PostEvent and PostGraphOnly into Change](issues/64-merge-postevent-and-postgraphonly-into-change.md), [65 — Log Undo and Redo as the target](issues/65-log-undo-and-redo-as-the-target.md), [66 — Move Actor orders onto Order and Notice](issues/66-move-actor-orders-onto-order-and-notice.md), and [67 — Log Load as Order.Load](issues/67-log-load-as-order-load.md).
