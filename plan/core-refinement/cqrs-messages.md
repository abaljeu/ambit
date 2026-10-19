# Message shape

Updated: 2026-10-19

Alan, 2026-10-19. This note is the target shape of the mailbox and the event log. Scope: this note, and one pointer from [core-refinement architecture](arch.md). The map, [Document-state events](spec.md), the tickets in §5 **Tickets 44–57**, and code stay.

[core-refinement architecture](arch.md) §10 **Core loop** stays the record of today's queue. [core-refinement](map.md) decision 15 **InMsg on the mailbox queue**, decision 16 **SnapshotDone with Persisted**, and decision 19 **Node events replace document-state Set ops** stay that same record.

## 1. Problem

`CoreMsg` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) mixes commands, reads, and session. A command case and a session case each carry a `Caller` and an `AsyncReplyChannel`. A read carries an `AsyncReplyChannel` and no `Caller`.

1. **Commands** — `PostEvent`, `PostGraphOnly`, `StartActor`, `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `LoadNode`, `ActorStop`, `CancelActor`.
2. **Reads** — `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, `EventsSince`.
3. **Session** — `Login`, `Logout`, `AdmitCaller`.

`InMsg` is `NodeParsed`, `SnapshotDone`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `QueueSum` is `Core of CoreMsg | In of InMsg`. `CoreMailboxApi.addInMsg` is a second door onto that same queue.

`EventBody` in [History.fs](../../src/Shared/History.fs) is `Change of Op list`, `Undo of EventId * Op list`, `Redo of EventId * Op list`, `ActorStart of ActorStart`, `ActorStop of NodeId * ActorResult`, and `Cancel of NodeId`. `Ev` holds `id`, `submissionId`, `authority`, `commandName`, and `body`.

`EventBody = InMsg | CoreMsg` is not a valid log type. `CoreMsg.PostEvent` holds an `Ev`. That `Ev` holds an `EventBody`. A body that includes `CoreMsg` includes that `Ev` again. The `Caller` and the reply are the envelope. The envelope is never logged. The payload is logged.

## 2. Target shape

`Command`, `Notice`, and `EventBody` are Shared. `Mailbox` is server-only. `PeerActorName` is server-only today (`Gambol.Server` in [CoreActorNames.fs](../../src/Server/Core/CoreActorNames.fs)). §7 **Open** asks where that name lives.

```fsharp
type Command =
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
    | SetDocumentState
    | ActorStop of focusId: NodeId * result: ActorResult

type EventBody =
    | Command of Command
    | Notice of Notice

type Ev = {
    id: EventId
    submissionId: System.Guid
    authority: Authority
    commandName: string
    body: EventBody }

type Query =
    | GetState
    | GetEventId
    | GetEventsSince of after: EventId
    | GetEventHistory
    | EventsSince of after: EventId

type Session =
    | Login
    | Logout
    | AdmitCaller

type Mailbox =
    | Submit of
        caller: Caller *
        submissionId: System.Guid *
        body: EventBody *
        reply: AsyncReplyChannel<_>
    | Ask of query: Query * reply: AsyncReplyChannel<_>
    | Session of caller: Caller * Session * reply: AsyncReplyChannel<_>
```

The reply types are not chosen. §7 **Open** asks what each reply carries.

1. **Change** — `Command.Change` of an `Op` list replaces `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly`.
2. **Undo and Redo** — `Command.Undo` and `Command.Redo` carry the target `EventId`. They carry no ops. Today's `EventBody.Undo` and `EventBody.Redo` each carry `EventId * Op list`.
3. **StartActor** — `Command.StartActor` of `ActorStart` replaces `CoreMsg.StartActor` and `CoreMsg.RecordSearchStart`. Both of those cases carry `ActorStart` today. Today `RecordSearchStart` records the start and does not run the walk.
4. **StartPeerActor** — `Command.StartPeerActor` replaces `CoreMsg.StartPeerActor`. It carries `PeerActorName` and `ActorStart`.
5. **LoadSave** — `Command.LoadSave` replaces `CoreMsg.StartLoadSaveCommand`. It carries `LoadSavePath`, `PeerActorName`, and `LoadSaveCommandRequest`.
6. **Load** — `Command.Load` of `NodeId` replaces `CoreMsg.LoadNode`.
7. **Cancel** — `Command.Cancel` of the focus `NodeId` replaces `CoreMsg.CancelActor` and `EventBody.Cancel`.
8. **Notice** — `Notice` replaces `InMsg`. The six Node cases are `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodeParsed`, `NodePersisting`, and `NodePersisted`. `NodePersisted` carries the launch version as an `EventId`. `Notice.SetDocumentState` is the case [45 — SetDocumentState leaves CoreMsg](issues/45-remove-document-state-set-ops.md) adds. `Notice.ActorStop` of `NodeId * ActorResult` replaces `CoreMsg.ActorStop`, `EventBody.ActorStop`, and `CoreMsg.RecordSearchStop`. `RecordSearchStop` stores `ActorStop` of the root id and `ActorSucceeded` today.
9. **EventBody** — `EventBody` is `Command of Command | Notice of Notice`. `EventBody.ActorStart` and `EventBody.ActorStop` leave. `Ev` keeps `id`, `submissionId`, `authority`, `commandName`, and `body`. Accept assigns `id`. The accepted `Ev` copies `submissionId` from the `Submit`. Its `authority` is the `Caller`'s `Authority`.
10. **Mailbox** — `Submit` carries a payload. `Ask` is a read: `GetState`, `GetEventId`, `GetEventsSince`, `GetEventHistory`, `EventsSince`. `Session` is `Login`, `Logout`, or `AdmitCaller`. The `Caller` and the reply stay on the envelope.
11. **Who sends** — A client sends only `Command`. An Actor or the backend sends `Command` or `Notice`.
12. **One Ev** — Each accepted `Submit` becomes one `Ev`. That `Ev` is what clients receive.
13. **Leaves** — `QueueSum` leaves. `CoreMailboxApi.addInMsg` leaves. `InMsg.MarkUnparsed` does not become a `Notice`. `InMsg.SnapshotDone` becomes `Notice.NodePersisted`. The `Graph` payload leaves. §6 **SnapshotDone**.

## 3. Decisions

1. **Logged as submitted** — The log stores the simpler message as submitted. `Undo target` and `Redo target` are logged without ops. The client and the server resolve that target the same way. The submitted body and the logged body are the same value.
2. **Load is logged** — `Load n` is logged as a command. Its effects follow as ordinary entries: `NodeParsing n`, Changes to `n` and descendants, then `NodeParsed n`. Load does more than push onto the parse stack. The Load command itself does not post a Node notice. `NodeParsing` stays a later entry.
3. **Ask waits** — A query stays on the mailbox as `Ask`. It waits until any prior `Submit` has completed.
4. **One Change** — `PostGraphOnly` and `PostEvent` merge into one `Change`. Document state is not a flag on that `Change`. It is a separate `SetDocumentState` or Node notice on the log, because document state does not propagate through event undo. After an ordinary edit, the server's `Change` handler posts Unpersisted `SetDocumentState`. Parse posts its Changes, then `NodeParsed`. The client never sends document state.
5. **SnapshotDone** — There is no whole-graph snapshot on the log or on the wire. The trace and the target are §6 **SnapshotDone**. The target case is `Notice.NodePersisted` of `NodeId * EventId`, the same shape as [46 — Record the persist version](issues/46-record-the-persist-version.md).

## 4. Migration

Expand, then change, then collapse. No step breaks upload, import, parse, Load, or undo.

1. [ ] **Expand** — Add `Command`, `Notice`, and the new `EventBody` beside `CoreMsg`, `InMsg`, `QueueSum`, and today's `EventBody`. Add mapping functions. Callers stay on the current types.
2. [ ] **Change** — Move callers one at a time: client posts, then Actors, then the backend.
3. [ ] **Collapse** — Remove the `CoreMsg` command cases, `InMsg`, and `QueueSum`. Collapse also removes `CoreMsg`. `Ask` and `Session` hold the reads and the session cases.

## 5. Tickets 44–57

These tickets stay worded with `InMsg` and `CoreMsg`. This note does not retitle them. On the Change step, an `InMsg` case becomes the `Notice` case of the same name.

1. **[44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md)** — Handlers for `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` become handlers for those `Notice` cases. The effects in [Document-state events](spec.md) stay.
2. **[45 — SetDocumentState leaves CoreMsg](issues/45-remove-document-state-set-ops.md)** — `InMsg.SetDocumentState` becomes `Notice.SetDocumentState`. The planned `EventBody.SetDocumentState` item is that same `Notice`, not a third case beside `Command` and `Notice`. `Op.SetDocumentState` still leaves. `InMsg.MarkUnparsed` still leaves and is not a `Notice`.
3. **[46 — Record the persist version](issues/46-record-the-persist-version.md)** — `InMsg.NodePersisted(nodeId, version)` becomes `Notice.NodePersisted` of `NodeId * EventId`. The version check stays.
4. **[47 — Undo and redo set Unpersisted](issues/47-undo-and-redo-set-unpersisted.md)** — Undo and Redo still set Unpersisted. The logged body is `Command.Undo` or `Command.Redo` of the target `EventId`, with no ops. The inverse-ops fill on today's `EventBody` is the shape decision 1 replaces.
5. **[48 — Browser applies Node events](issues/48-browser-applies-node-events.md)** — The Browser applies the `Ev` log. Logged Node notices replace logged `InMsg` values. The client receives every `Ev`.
6. **[49 — Rename NoServerFile to PullingFile](issues/49-rename-noserverfile-to-pullingfile.md)** — This rename does not change the message sum.
7. **[50 — Upload emits NodeUploading](issues/50-upload-emits-nodeuploading.md)** — The upload post becomes `Notice.NodeUploading`.
8. **[51 — Stand the persist stack](issues/51-stand-the-persist-stack.md)** — The finish post becomes `Notice.NodePersisted`. `SnapshotDone` is the misnamed persist-finished signal, not a second snapshot. §6 **SnapshotDone**.
9. **[52 — Log stack pulls](issues/52-log-stack-pulls.md)** — The pull posts become `Notice.NodeParsing` and `Notice.NodePersisting`.
10. **[53 — Arrival and reconcile emit NodeReceived](issues/53-arrival-and-reconcile-emit-nodereceived.md)** — Those posts become `Notice.NodeReceived`.
11. **[54 — Parse and import stop the Parsed Set](issues/54-parse-and-import-stop-the-parsed-set.md)** — The finish post becomes `Notice.NodeParsed`. The Parsed `Op.SetDocumentState` still leaves.
12. **[55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md)** — Replay still skips the six Node notices and still marks Unpersisted. Whether it applies `Notice.SetDocumentState` is §7 **Open**.
13. **[56 — Remove ParseThread markUnparsed](issues/56-remove-parsethread-markunparsed.md)** — `ParseThread.markUnparsed` still leaves. `MarkUnparsed` is not a `Notice`.
14. **[57 — Upload Actor posts document state](issues/57-upload-actor-posts-document-state.md)** — The Actor's `InMsg` handle becomes `Submit` of a `Notice`. The Actor posts `Notice.NodeUploading`, `Notice.NodeReceived`, and `Notice.SetDocumentState`. `Api.postEvents` still does not infer those three from the ops of a client `Change`.
15. **[58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md)** — Deletes the dead flush, the `FileAgent` no-op, and the `Graph` payload. Renames the live follow-up file diff. Does not take the version check from [46 — Record the persist version](issues/46-record-the-persist-version.md).

## 6. SnapshotDone

The server writes individual document files. It does not write a whole-graph snapshot. `Database.initSchema` drops the `snapshots` table. `DocumentPersistChange.persistGraphChange` diffs two graphs and writes the document files that differ. `persistGraphOps` writes the document files for an op list. Both jobs share `PersistThread.finishWhenBound`.

1. **finishWhenBound** — Live. Misnamed. After a successful Ops or Change write it posts `InMsg.SnapshotDone(nodeId, Some graph)`. A failed write does not post. The graph is the stamped in-memory graph from the file writer.
2. **applyInMsg** — Live. The `SnapshotDone` case calls `persist.snapshotDone`, then `GraphMutate.setPersistState` with `PersistState.Persisted`. This is the only core-loop writer of Persisted. `DocumentPersistPath.stampNode` also sets Persisted on the graph the writer returns, and both agents store that stamped graph as state. A missing persist field decodes as Persisted. That decode is not a post-write set.
3. **handleSnapshotDone** — The batch count is live. The graph match is dead. It copies the payload into `persistedGraph` only when `GraphProjection.graphEquals` matches the live graph, then calls `countSnapshotMark`. On the ops path `commitPostChange` already stored that graph. On the Change path the stamped graph has new `updateTime` values, so `graphEquals` fails and the copy does not run.
4. **countSnapshotMark** — Live. Misnamed. It counts down `snapshotIds` and `snapshotMarks`. At zero it clears `snapshotInProgress` and may start another batch.
5. **requestSnapshot** — Live. Misnamed. It does nothing until `bindSnapshot` fills `snapshotPost`. Otherwise it calls `startSnapshot`.
6. **startSnapshot** — Live. Misnamed. It queues `PersistKind.Change` for workspace ids the ops write did not already notify and that are not Unparsed. That job calls `persistGraphChange`. It does not write a snapshot blob.
7. **Batch fields** — Live. Misnamed. `snapshotIds`, `snapshotMarks`, `snapshotWorkspaces`, `snapshotNeeded`, and `snapshotInProgress` are that follow-up file diff. `opsNotifiedIds` is the same batch, and that name already says what it holds.
8. **persistedGraph** — Live. Misnamed. It is the baseline graph for the next file diff. Ops commit, a graph-only post, and projection reload update it. It is not a snapshot file.
9. **enqueueSnapshot** — Live. Misnamed. `startSnapshot` uses it for the workspace diff. `catchUpParsed` uses it for one node that is Parsed and Unpersisted.
10. **FileAgent snapshotDone** — Dead. The filling sets `snapshotDone` to `fun _ _ -> ()`.
11. **flushSnapshot** — Dead. `DbAgent` and `FileAgent` return `Ok ()`. `SavePrep.syncDataDir` calls it only when `DbStatus` is not `Ok`. When the database is `Ok`, save reads the state event id and does not call it. `RouteRegistration.prepareGitSave` and `RouteRegistration.flushForGit` only pass the function in.
12. **bindSnapshot** — Live. Misnamed. `CoreMailbox.hostPublishing` passes `CoreMailboxApi.addInMsg`. Each agent stores that poster in `snapshotPost`. `finishWhenBound` posts only when the slot is filled.
13. **PersistHandlers.snapshotDone** — Live slot. Misnamed. `DbAgent` sets `handleSnapshotDone`. `FileAgent` sets the no-op. `CoreMailboxLoad.failedPersist` replaces it with a no-op while writes are refused.
14. **Graph payload** — Dead. `applyInMsg` sets Persisted from the node id. `FileAgent` ignores the graph. The only reader is the equality copy in `handleSnapshotDone`, and that copy does not keep a graph the ops path has not already stored.

`SnapshotDone` becomes `Notice.NodePersisted` of `nodeId * version: EventId`. That matches [46 — Record the persist version](issues/46-record-the-persist-version.md). The `Graph` leaves the message. The dead flush, the `FileAgent` no-op, and the equality copy leave. The follow-up file diff stays, under names that say file diff. [58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md) does the deletion and the renames. [46 — Record the persist version](issues/46-record-the-persist-version.md) adds the version check. Upload, import, parse, Load, undo, and persist stay working.

## 7. Open

1. **Replies** — What does `Submit` reply, what does `Ask` reply, and what does `Session` reply? Today `PostEvent` replies `Result<Ev * CoreChangesAccepted option, string>`. `PostGraphOnly` replies `Result<CoreChangesAccepted, string>`. `StartActor`, `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `LoadNode`, `ActorStop`, and `CancelActor` reply `Result<unit, string>`. `GetState` replies `Result<State, string>`. `GetEventId` replies `Result<EventId, string>`. `GetEventsSince` replies `Result<Ev list, string>`. `GetEventHistory` and `EventsSince` reply `EventLog`. `Login` and `Logout` reply `Result<unit, string>`. `AdmitCaller` replies `bool`.
2. **commandName** — `Ev` keeps `commandName`. `Submit` does not carry it. Where does `commandName` come from?
3. **SetDocumentState fields** — What does `Notice.SetDocumentState` carry? Decision 4 says an ordinary edit posts Unpersisted `SetDocumentState`. The fields are not pinned.
4. **PeerActorName** — Shared `Command.StartPeerActor` and `Command.LoadSave` carry `PeerActorName`. That name is server-only today. Does it move to Shared?
5. **Stored replay** — Does stored replay apply `Notice.SetDocumentState`, or skip it and mark Unpersisted the way [55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md) skips the six Node notices?
6. **Glossary Command** — [GLOSSARY](../../GLOSSARY.md) says a Command is a user operation a person invokes, and that a Command is not a message. This note uses `Command` for the logged payload. Which meaning stays?
