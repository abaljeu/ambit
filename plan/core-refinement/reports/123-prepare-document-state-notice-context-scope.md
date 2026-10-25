# Scope — [123 — Every Notice states its document-state write](../issues/123-prepare-document-state-notice-context.md)

Scope mode is implement for [123 — Every Notice states its document-state write](../issues/123-prepare-document-state-notice-context.md). The slice aligns owner tickets and named tests with the `DocumentState` write each `Notice` case applies in code. This report does not change source or tests.

## Signatures

[ ] `type DocumentState` in [Model.fs](../../src/Shared/Model.fs) is `{ parseState: ParseState; persistState: PersistState; pullingFile: bool; persistedVersion: EventId; changedVersion: EventId }`.
[ ] `type Notice` in [History.fs](../../src/Shared/History.fs) declares `NodeParsed`, `NodePersisted`, `MarkUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, and `PersistFailed`.
[ ] `DocumentState.persistStateForWritten : DocumentState -> EventId -> PersistState` in [Model.fs](../../src/Shared/Model.fs).
[ ] `GraphMutate.setParseState : NodeId -> ParseState -> Graph -> Result<Graph, string>` in [GraphMutate.fs](../../src/Shared/GraphMutate.fs).
[ ] `GraphMutate.setPersistState : NodeId -> PersistState -> Graph -> Result<Graph, string>` in [GraphMutate.fs](../../src/Shared/GraphMutate.fs).
[ ] `Node.withParseState : ParseState -> Node -> Node` in [Model.fs](../../src/Shared/Model.fs) sets `parseState` and sets `pullingFile` to false.
[ ] `Node.withDocumentState : DocumentStateEnum -> Node -> Node` in [Model.fs](../../src/Shared/Model.fs) sets `parseState` from the enum and sets `pullingFile` when the enum is `PullingFile`.
[ ] `type PersistHandlers` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) includes `replaceGraph: Notice -> Graph -> unit` and `noteWriteFailed: unit -> unit`.
[ ] `CoreMailboxLoop.applyNotice : PersistHandlers -> Notice -> Result<unit, string>` is internal in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs).
[ ] `CoreEventDispatch.writeAxis : PersistHandlers -> Notice -> (Graph -> Result<Graph, string>) -> Result<unit, string>` is internal in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs).
[ ] `CoreEventDispatch.logNotice : Context -> Notice -> Result<Ev, string>` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs).
[ ] `CoreMailboxFilesAxis.writeParseState : PersistHandlers -> Notice -> NodeId -> ParseState -> Result<unit, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxFilesAxis.writeUnpersisted : NodeId -> Graph -> Result<Graph, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxFilesAxis.writePullingFileUnpersisted : PersistHandlers -> NodeId -> Result<unit, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxFilesAxis.writeReceived : PersistHandlers -> NodeId -> Result<unit, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxFilesAxis.applyNodeParsed : PersistHandlers -> NodeId -> Result<unit, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxFilesAxis.writePersisted : PersistHandlers -> NodeId -> EventId -> DateTime -> Result<unit, string>` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] `CoreMailboxApi.postNotice : MailboxHost -> Notice -> unit` is internal in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs).
[ ] `Database.writeNoticeRow : string -> Notice -> Graph -> Task` in [Database.fs](../../src/Server/Database.fs).

## Match sites

### `Notice` case definition

[ ] [History.fs](../../src/Shared/History.fs) `type Notice` declares all eight cases.

### `CoreMailboxLoop.applyNotice`

[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodeParsed nodeId` calls `CoreMailboxFilesAxis.applyNodeParsed`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodeUploading nodeId` calls `CoreMailboxFilesAxis.writePullingFileUnpersisted`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodeReceived nodeId` calls `CoreMailboxFilesAxis.writeReceived`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodeParsing _` returns `Ok ()` with no axis write.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodePersisting nodeId` calls `CoreEventDispatch.writeAxis` with `CoreMailboxFilesAxis.writeUnpersisted nodeId`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.PersistFailed _` calls `persist.noteWriteFailed ()` then `Ok ()`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.MarkUnparsed nodeId` calls `CoreMailboxFilesAxis.writeParseState` with `ParseState.Unparsed`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice` — `Notice.NodePersisted (nodeId, persistedVersion, updateTime)` calls `persist.noteDiffDone`, then `CoreMailboxFilesAxis.writePersisted`, then `persist.notePersisted` on success.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `dispatchItem` — `CoreQueue.Notice msg` calls `CoreEventDispatch.logNotice` then `applyNotice context.persist msg`.

### `CoreMailboxFilesAxis` helpers

[ ] [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) `writePullingFileUnpersisted` passes `Notice.NodeUploading nodeId` into `writeAxis`, then `writeUnpersisted` and private `withPullingFile`.
[ ] [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) `writeReceived` passes `Notice.NodeReceived nodeId` into `writeAxis`, then `GraphMutate.setParseState` with `ParseState.Unparsed` and `GraphMutate.setPersistState` with `PersistState.Persisted`.
[ ] [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) `applyNodeParsed` calls `writeParseState` with `Notice.NodeParsed nodeId` and `ParseState.Parsed`, then `pushParsedUnpersisted` and `persist.noteParsed`.
[ ] [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) `markUnparsedThenPush` calls `writeParseState` with `Notice.MarkUnparsed nodeId` and `ParseState.Unparsed` without calling `applyNotice`.

### JSON and database

[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `encodeNotice` matches each `Notice` case.
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `noticeFrom` maps tags `nodeParsed`, `markUnparsed`, `nodeUploading`, `nodeReceived`, `nodeParsing`, `nodePersisting`; `persistFailed` uses `decodePersistFailed`.
[ ] [Database.fs](../../src/Server/Database.fs) `writeNoticeRow` matches each `Notice` case for row projection or no-op.

### Producers (callers that construct or post a `Notice`)

[ ] [Api.fs](../../src/Server/Api.fs) `inferredNodeEvents` — `Some (Notice.NodeUploading id)`, `Some (Notice.NodeReceived id)`, `Some (Notice.NodeParsed id)` from document-state transitions on a Change.
[ ] [Api.fs](../../src/Server/Api.fs) `postEvents` — `List.iter (CoreMailboxApi.postNotice handle.host)` on inferred notices after a successful Change post.
[ ] [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs) `reconcileReceivedFiles` — posts `Notice.NodeReceived` beside reconcile ops.
[ ] [ParseThread.fs](../../src/Server/ParseThread.fs) — posts `Notice.MarkUnparsed` and `Notice.NodeParsed` through `CoreMailboxApi.postNotice`.
[ ] [PersistThread.fs](../../src/Server/Core/PersistThread.fs) `afterWrite` — posts `Notice.NodePersisted` or `Notice.PersistFailed` through `deps.postNotice`.
[ ] [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) `pushChangeSpecials` — builds `Notice.NodePersisting nodeId` inside `writeAxis` with `GraphMutate.setUnpersistedAt` on Change completion (edit path, not the `applyNotice` `NodePersisting` arm alone).

## Existing tests

[ ] ``NodeUploading applies PullingFile`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `CoreMailboxLoop.applyNotice` with `Notice.NodeUploading`; asserts `pullingFile`, `PersistState.Unpersisted`, projection row `unparsed` / `unpersisted`, and unchanged node count.
[ ] ``NodeReceived applies Unparsed`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodeReceived`; asserts `ParseState.Unparsed`, `pullingFile` false, `PersistState.Persisted`, and projection row strings.
[ ] ``NodeParsing changes nothing`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) posts `Notice.NodeParsing` through `CoreMailboxApi.postNotice`; reads graph via `CoreMailboxApi.getState`; asserts parse and persist state unchanged, no push, and EventLog holds `EventBody.Notice`.
[ ] ``ParseFinished sets Parsed and leaves PersistState`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodeParsed`; asserts `ParseState.Parsed`, `pullingFile` false, `PersistState.Unpersisted` unchanged from setup.
[ ] ``ParseFinished leaves a pullingFile File Node Unparsed`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodeParsed` on a `PullingFile` node; asserts parse stays `Unparsed`, `pullingFile` true, persist unchanged.
[ ] ``NodePersisting matches the Unpersisted Set`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodePersisting`; compares full `documentState` to `Graph.setPersistState` with `PersistState.Unpersisted` from the same initial graph.
[ ] ``NodePersisted sets Persisted and calls the diff hook`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodePersisted`; asserts `PersistState.Persisted`, `node.updateTime`, parse unchanged, and `noteDiffDone` recorded.
[ ] ``NodePersisted for an older version leaves the node Unpersisted`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.NodePersisted`; asserts `persistedVersion`, `persistState`, `changedVersion`, and `updateTime` for late, equal, and zero-version cases.
[ ] ``PersistFailed does not change persist state`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.PersistFailed`; asserts persist and parse state unchanged on the graph.
[ ] ``MarkUnparsed sets Unparsed and leaves PersistState`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) calls `applyNotice` with `Notice.MarkUnparsed`; asserts `ParseState.Unparsed` and unchanged `PersistState.Unpersisted`.

## Copy-from

[ ] The comment block above `CoreMailboxFilesAxis.applyNodeParsed` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) already states each `Notice` axis effect in prose (PullingFile, Received, Parsing, Persisting, Parsed, PersistFailed, MarkUnparsed, NodePersisted).
[ ] [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) lists owner tickets per `Notice` sender group (Load, Parse, Persist) and names the axis helpers in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).

## implement

[ ] [123 — Every Notice states its document-state write](../issues/123-prepare-document-state-notice-context.md) section 6 records no new test is required; existing named tests remain the check for each write.
[ ] The slice changes owner ticket prose only; `CoreMailboxLoop.applyNotice` and [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) already implement the writes [123 — Every Notice states its document-state write](../issues/123-prepare-document-state-notice-context.md) section 5 describes.
[ ] [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) acceptance names PullingFile and Unpersisted on the handler; it does not name `parseState` as `ParseState.Unparsed` or the `pullingFile` field; code sets all three through `writePullingFileUnpersisted`.
[ ] [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md) acceptance already names Unparsed, Persisted, and clearing PullingFile; code clears `pullingFile` through `Node.withParseState` inside `GraphMutate.setParseState`.
[ ] [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md) names `CoreMailboxLoad.applyNotice`; the live handler is `CoreMailboxLoop.applyNotice` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs).
[ ] [100 — Core loop gates Persisted on the written version](../issues/100-core-loop-gates-persisted-on-the-written-version.md) names `CoreMailboxLoad.writePersisted`; the live function is `CoreMailboxFilesAxis.writePersisted` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs).
[ ] [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) **Status** is `defined`; its Context describes the `Notice.NodeParsed` handler and matches `applyNodeParsed` and `GraphMutate.setParseState` when `pullingFile` is set.
[ ] [121 — MarkUnparsed leaves Notice](../issues/121-markunparsed-leaves-notice.md) plans removal of `Notice.MarkUnparsed`; [123 — Every Notice states its document-state write](../issues/123-prepare-document-state-notice-context.md) still documents the write while the case exists.

### Public seam for named tests (no new test)

[ ] ``NodeUploading applies PullingFile`` observes `CoreMailboxLoop.applyNotice` on `PersistHandlers.replaceGraph` (private `apply` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs)).
[ ] ``NodeReceived applies Unparsed`` observes the same `applyNotice` seam.
[ ] ``NodeParsing changes nothing`` observes `CoreMailboxApi.postNotice`, `CoreMailboxApi.getState`, and `CoreMailboxApi.eventsSince` on a live mailbox host.
[ ] ``ParseFinished sets Parsed and leaves PersistState`` and ``ParseFinished leaves a pullingFile File Node Unparsed`` observe `CoreMailboxLoop.applyNotice` with `Notice.NodeParsed`.
[ ] ``NodePersisting matches the Unpersisted Set`` observes `applyNotice` with `Notice.NodePersisting`.
[ ] ``NodePersisted sets Persisted and calls the diff hook`` and ``NodePersisted for an older version leaves the node Unpersisted`` observe `applyNotice` with `Notice.NodePersisted` and `PersistHandlers.noteDiffDone`.
[ ] ``PersistFailed does not change persist state`` observes `applyNotice` with `Notice.PersistFailed`.
[ ] ``MarkUnparsed sets Unparsed and leaves PersistState`` observes `applyNotice` with `Notice.MarkUnparsed`.

### `Notice.NodeUploading`

[ ] Axis write function: `CoreMailboxFilesAxis.writePullingFileUnpersisted` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs), reached from `CoreMailboxLoop.applyNotice`.
[ ] Code today sets `parseState` to `ParseState.Unparsed` via `Node.withDocumentState PullingFile`, sets `persistState` to `PersistState.Unpersisted` via `writeUnpersisted`, and sets `pullingFile` to true via the same `withDocumentState` call.
[ ] Code today leaves `persistedVersion` and `changedVersion` unchanged; ``NodeUploading leaves the Edit's changedVersion`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) asserts `changedVersion`.
[ ] Code today leaves `Node.updateTime` unchanged on this path (no `NodeUpdateTime.touch` in `writePullingFileUnpersisted`).
[ ] Owner ticket: [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md); acceptance states PullingFile and Unpersisted and does not list `parseState` or `pullingFile` field names.
[ ] Named test: ``NodeUploading applies PullingFile`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); checks `pullingFile`, `PersistState.Unpersisted`, and row `unparsed`; does not assert `persistedVersion` or `changedVersion`.

### `Notice.NodeReceived`

[ ] Axis write function: `CoreMailboxFilesAxis.writeReceived` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs), reached from `CoreMailboxLoop.applyNotice`.
[ ] Code today sets `parseState` to `ParseState.Unparsed`, sets `persistState` to `PersistState.Persisted`, and sets `pullingFile` to false through `GraphMutate.setParseState` and `Node.withParseState`.
[ ] Code today leaves `persistedVersion` and `changedVersion` unchanged on this path.
[ ] Code today touches `Node.updateTime` through `GraphMutate.setParseState` when parse state changes.
[ ] Owner ticket: [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md); acceptance states Unparsed, Persisted, and clearing PullingFile.
[ ] Named test: ``NodeReceived applies Unparsed`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); checks parse, persist, `pullingFile`, and projection row; does not assert version fields or `updateTime`.

### `Notice.NodeParsing`

[ ] Axis write: the `Notice.NodeParsing` arm in `CoreMailboxLoop.applyNotice` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) returns `Ok ()` and does not call `writeAxis`.
[ ] Code today leaves `parseState`, `persistState`, `pullingFile`, `persistedVersion`, and `changedVersion` unchanged on the graph.
[ ] Code today leaves `Node.updateTime` unchanged on this path.
[ ] Owner ticket: [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md); finish-up states NodeParsing leaves parse and persist state unchanged.
[ ] Named test: ``NodeParsing changes nothing`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); checks parse, persist, `pullingFile`, empty push list, and EventLog notice body.

### `Notice.NodeParsed`

[ ] Axis write function: `CoreMailboxFilesAxis.applyNodeParsed` calling `writeParseState` with `ParseState.Parsed` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs), reached from `CoreMailboxLoop.applyNotice`.
[ ] When `pullingFile` is false, code sets `parseState` to `ParseState.Parsed` and sets `pullingFile` to false; when `pullingFile` is true, `GraphMutate.setParseState` leaves the graph unchanged so parse stays `Unparsed` and `pullingFile` stays true.
[ ] Code today leaves `persistState`, `persistedVersion`, and `changedVersion` unchanged on this path.
[ ] Code today touches `Node.updateTime` when parse state actually changes through `GraphMutate.setParseState`.
[ ] Owner ticket: [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md); Context states Parsed when PullingFile is clear, Unparsed when PullingFile is set, and unchanged persist state.
[ ] Named test: ``ParseFinished sets Parsed and leaves PersistState`` checks Parsed, cleared `pullingFile`, and unchanged Unpersisted; ``ParseFinished leaves a pullingFile File Node Unparsed`` checks Unparsed parse, `pullingFile` true, and unchanged persist.

### `Notice.NodePersisting`

[ ] Axis write function: `CoreMailboxFilesAxis.writeUnpersisted` inside `CoreEventDispatch.writeAxis` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `applyNotice`.
[ ] Code today sets `persistState` to `PersistState.Unpersisted` and leaves `parseState`, `pullingFile`, `persistedVersion`, and `changedVersion` unchanged.
[ ] Code today leaves `Node.updateTime` unchanged on this path (`writeUnpersisted` uses `Node.withPersistState` only).
[ ] Owner ticket: [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md); finish-up states NodePersisting sets Unpersisted and leaves parse state unchanged.
[ ] Named test: ``NodePersisting matches the Unpersisted Set`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); compares full `documentState` to `Graph.setPersistState` Unpersisted and asserts parse Parsed and `pullingFile` false.

### `Notice.NodePersisted`

[ ] Axis write function: `CoreMailboxFilesAxis.writePersisted` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs), reached from `CoreMailboxLoop.applyNotice` after `noteDiffDone`.
[ ] Code today sets `documentState.persistedVersion` to the notice version, sets `persistState` through `DocumentState.persistStateForWritten`, and sets `Node.updateTime` through private `stampUpdateTime`.
[ ] Code today leaves `parseState` and `pullingFile` unchanged on this path; leaves `changedVersion` unchanged when the written version is older than `changedVersion`.
[ ] Owner ticket: [100 — Core loop gates Persisted on the written version](../issues/100-core-loop-gates-persisted-on-the-written-version.md); section 5 lists `persistedVersion`, `updateTime`, and gated `Persisted`.
[ ] Named test: ``NodePersisted sets Persisted and calls the diff hook`` checks persist, `updateTime`, parse, and diff hook; ``NodePersisted for an older version leaves the node Unpersisted`` checks `persistedVersion`, persist, `changedVersion`, and `updateTime`.

### `Notice.PersistFailed`

[ ] Axis write: the `Notice.PersistFailed` arm in `CoreMailboxLoop.applyNotice` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) calls `persist.noteWriteFailed ()` and does not call `writeAxis`.
[ ] Code today leaves `parseState`, `persistState`, `pullingFile`, `persistedVersion`, `changedVersion`, and `Node.updateTime` unchanged on the graph.
[ ] Owner ticket: [75 — A failed write does not fail the edit](../issues/75-a-failed-write-does-not-fail-the-edit.md); handler item states persist state does not change.
[ ] Named test: ``PersistFailed does not change persist state`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); asserts persist and parse unchanged.

### `Notice.MarkUnparsed`

[ ] Axis write function: `CoreMailboxFilesAxis.writeParseState` with `ParseState.Unparsed` in [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs), reached from `CoreMailboxLoop.applyNotice` (and from `markUnparsedThenPush` on the same helper).
[ ] Code today sets `parseState` to `ParseState.Unparsed` and sets `pullingFile` to false through `Node.withParseState`; leaves `persistState`, `persistedVersion`, and `changedVersion` unchanged.
[ ] Code today touches `Node.updateTime` when parse state changes through `GraphMutate.setParseState`.
[ ] Owner ticket: [121 — MarkUnparsed leaves Notice](../issues/121-markunparsed-leaves-notice.md) documents the direct `writeParseState` write before planned removal; it does not restate unchanged `persistState` in section 5.
[ ] Named test: ``MarkUnparsed sets Unparsed and leaves PersistState`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); checks Unparsed parse and unchanged Unpersisted persist.
