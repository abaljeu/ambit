# 45 — SetDocumentState leaves; NodeUnpersisted replaces it — implement scope

Scope for [45 — SetDocumentState leaves; NodeUnpersisted replaces it](plan/core-refinement/issues/45-remove-document-state-set-ops.md). Source is unchanged in this pass.

## Signatures

[ ] `Op.SetDocumentState` in `src/Shared/History.fs`: `SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum` on the `Op` discriminated union.

[ ] `Notice.MarkUnparsed` in `src/Shared/History.fs`: `MarkUnparsed of nodeId: NodeId` on the `Notice` discriminated union.

[ ] `Notice.NodeParsed` in `src/Shared/History.fs`: `NodeParsed of nodeId: NodeId` on the `Notice` discriminated union.

[ ] `Notice.NodeUnpersisted` is not declared in `src/` today.

[ ] `EventBody` in `src/Shared/History.fs` has `Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, `Load`, `Cancel`, and `Notice of Notice`; there is no `SetDocumentState` case.

[ ] `CoreEventDispatch.postEvent` in `src/Server/Core/CoreEventDispatch.fs`: `postEvent (context: Context) (caller: Caller) (event: Ev) (graphOnly: bool) : Result<Ev * CoreChangesAccepted option, string>`.

[ ] `CoreEventDispatch.pushChangeSpecials` in `src/Server/Core/CoreEventDispatch.fs`: private; runs from `persist` when `graphOnly` is false after a successful `applyEvent`.

[ ] `CoreEventDispatch.markForPersist` in `src/Server/Core/CoreEventDispatch.fs`: private; `(persist: PersistHandlers) (version: EventId) (nodeId: NodeId) : Result<unit, string>`.

[ ] `CoreEventDispatch.writeAxis` in `src/Server/Core/CoreEventDispatch.fs`: internal; `(persist: PersistHandlers) (notice: Notice) (mutate: Graph -> Result<Graph, string>) : Result<unit, string>`.

[ ] `CoreEventDispatch.logNotice` in `src/Server/Core/CoreEventDispatch.fs`: `logNotice (context: Context) (notice: Notice)` appends an `Ev` with `EventBody.Notice notice`.

[ ] `CoreMailboxLoad.applyNotice` in `src/Server/Core/CoreMailboxLoad.fs`: `applyNotice (persist: PersistHandlers) (msg: Notice) : Result<unit, string>`.

[ ] `CoreMailboxApi.postNotice` in `src/Server/Core/CoreMailboxApi.fs`: `postNotice (host: MailboxHost) (msg: Notice)` enqueues `CoreQueue.Notice msg`.

[ ] `Api.inferredNodeEvents` in `src/Server/Api.fs`: private; `inferredNodeEvents (event: Ev) : Notice list`.

[ ] `Api.postEvents` in `src/Server/Api.fs`: HTTP handler; after `CoreDoor.postEvents` it `List.collect inferredNodeEvents` and `List.iter (CoreMailboxApi.postNotice handle.host)`.

[ ] `Serialization.encodeOp` / `Serialization.decodeOp` in `src/Shared/Serialization.fs` include the `"SetDocumentState"` tag for `Op.SetDocumentState`.

[ ] `EventJson.encodeNotice` / decode path in `src/Shared/EventJson.fs` include `"markUnparsed"` for `Notice.MarkUnparsed`.

## Match sites

### `Op.SetDocumentState` / `SetDocumentState`

[ ] Constructor on `Op` in `src/Shared/History.fs` module `Op` (union definition).

[ ] Match in `Op.involvedNodeIds` in `src/Shared/History.fs` module `Op`.

[ ] Match in `Op.applyAllowed` in `src/Shared/History.fs` module `Op` (calls `Graph.setDocumentState`).

[ ] Match in `Op.invert` in `src/Shared/History.fs` module `Op`.

[ ] Match in `Op` undo path (`undoAllowed`) in `src/Shared/History.fs` module `Op`.

[ ] Match in `Op.opChangesGraphShape` in `src/Shared/History.fs` module `Op`.

[ ] Encode match in `Serialization.encodeOp` in `src/Shared/Serialization.fs`.

[ ] Decode match on `"SetDocumentState"` in `Serialization.decodeOp` in `src/Shared/Serialization.fs`.

[ ] Match in `DocumentOpImpact` in `src/Shared/DocumentOpImpact.fs` (impact and `owningSpecials`-related paths).

[ ] Match in `DatabaseProjection` op handler in `src/Server/DatabaseProjection.fs`.

[ ] Match in `ResidentProjection` in `src/Shared/ResidentProjection.fs`.

[ ] Match in `DocumentColdParse` in `src/Shared/documents/DocumentColdParse.fs`.

[ ] Op list constructor in `WorkspaceUploadStructure` in `src/Shared/WorkspaceUploadStructure.fs` (multiple helpers).

[ ] Op list constructor in `ImportDocument` in `src/Shared/dotnet/ImportDocument.fs`.

[ ] Op list constructor in `ImportText` in `src/Shared/ImportText.fs`.

[ ] Op list constructor in `DirectoryReconcile` in `src/Shared/dotnet/DirectoryReconcile.fs`.

[ ] Op list constructor in `LazyLoadReconciliationApply` in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`.

[ ] Op constructor in `LazyLoadReconciliation` in `src/Shared/dotnet/LazyLoadReconciliation.fs`.

[ ] Match on ops in `LazyLoadReconciliationServer` in `src/Server/LazyLoadReconciliationServer.fs`.

[ ] Match in `Api.inferredNodeEvents` in `src/Server/Api.fs` (reads `Op.SetDocumentState` on `EventBody.Change` ops).

[ ] Test constructors and matches span `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`, `HistoryTests.fs`, `SerializationTests.fs`, `LazyLoadReconciliationTests.fs`, `DirectoryReconcileTests.fs`, `ImportDocumentTests.fs`, `ImportTextTests.fs`, `WorkspaceUploadStructureTests.fs`, `DocumentOpImpactTests.fs`, `ClientHistoryTests.fs`, `ViewModelJoinOpsTests.fs`, and `tests/Server.Tests/NodeEventExpandTests.fs`, `LazyLoadReconciliationServerTests.fs`, `ParseThreadLoadTests.fs`, `StateEndpointTests.fs`, `DatabaseProjectionContractTests.fs`, `DatabaseProjectionTests.fs`, `LoadSaveCommandTests.fs`, `GithubTransportActorTests.fs`, `GithubTransportLoadPullTests.fs`.

### `Notice.MarkUnparsed` / `MarkUnparsed`

[ ] Constructor on `Notice` in `src/Shared/History.fs`.

[ ] Constructor passed to `writeParseState` in `CoreMailboxLoad.markUnparsedThenPush` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Match arm in `CoreMailboxLoad.applyNotice` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Encode and decode in `EventJson.encodeNotice` and `noticeFrom "markUnparsed"` in `src/Shared/EventJson.fs`.

[ ] Match in `Database.writeNoticeRow` in `src/Server/Database.fs`.

[ ] Caller `ParseThread.start` closure `markUnparsed` posts `Notice.MarkUnparsed` via `CoreMailboxApi.postNotice` in `src/Server/ParseThread.fs`.

[ ] Tests: `CoreLoopNoticeTests.fs` ``MarkUnparsed sets Unparsed and leaves PersistState``, `DbAgentNoticeTests.fs` ``MarkUnparsed writes parse state to the database``, `EventJsonTests.fs` ``Notice JSON round trip``, `ParseThreadLoadTests.fs` multiple `postNotice` sites with `Notice.MarkUnparsed`.

### `Notice.NodeUnpersisted` / `NodeUnpersisted`

[ ] No constructor, match, caller, or codec in `src/` or `tests/` today.

[ ] Planning references only (for example `plan/core-refinement/cqrs-messages.md` open decision on `Notice.NodeUnpersisted`).

### `Notice.NodeParsed` / `NodeParsed`

[ ] Constructor on `Notice` in `src/Shared/History.fs`.

[ ] Handler `CoreMailboxLoad.applyNodeParsed` and match arm in `CoreMailboxLoad.applyNotice` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Encode and decode `"nodeParsed"` in `src/Shared/EventJson.fs`.

[ ] Match in `Database.writeNoticeRow` in `src/Server/Database.fs`.

[ ] Caller `ParseThread.start` `finishParse` posts `Notice.NodeParsed` via `CoreMailboxApi.postNotice` in `src/Server/ParseThread.fs`.

[ ] Caller `Api.postParseFile` posts `Notice.NodeParsed` via `CoreMailboxApi.postNotice` in `src/Server/Api.fs`.

[ ] Producer pick in `Api.inferredNodeEvents` for `Unparsed, Current` transitions in `src/Server/Api.fs`.

[ ] Tests include `CoreLoopNoticeTests.fs` (``NodeParsed pushes a persist request``, ``ParseFinished sets Parsed and leaves PersistState``, and related), `DbAgentNoticeTests.fs` ``NodeParsed writes parse state to the database``, `NodeEventExpandTests.fs` expand tests beside `SetDocumentState`, `MailboxExpandTests.fs`, `PersistApplyTests.fs`, `FileAgentFailureTests.fs`, `PersistThreadTests.fs`, and many `ParseThreadLoadTests.fs` `postNotice` calls.

### `Api.inferredNodeEvents`

[ ] Definition and `Op.SetDocumentState` match in `src/Server/Api.fs` module `Api`.

[ ] Caller `Api.postEvents` `List.collect inferredNodeEvents` in `src/Server/Api.fs`.

### `CoreMailboxApi.postNotice`

[ ] Definition in `src/Server/Core/CoreMailboxApi.fs` module `CoreMailboxApi`.

[ ] Caller `Api.postEvents` after client Change accept in `src/Server/Api.fs`.

[ ] Caller `Api.postParseFile` after graph-only parse in `src/Server/Api.fs`.

[ ] Caller `ParseThread.start` for `markUnparsed` and `finishParse` in `src/Server/ParseThread.fs`.

[ ] Caller `LazyLoadReconciliationServer` git reconcile `beside` callback in `src/Server/LazyLoadReconciliationServer.fs`.

[ ] Wiring `CoreMailbox.host` `bindPersistFinished (CoreMailboxApi.postNotice created)` in `src/Server/Core/CoreMailbox.fs`.

[ ] Indirect caller `PersistThread.afterWrite` through `deps.postNotice` in `src/Server/Core/PersistThread.fs` (bound to `CoreMailboxApi.postNotice` in `DbAgent.fs` and `FileAgent.fs`).

[ ] Tests call `CoreMailboxApi.postNotice` from `CoreLoopNoticeTests.fs`, `DbAgentNoticeTests.fs`, `MailboxExpandTests.fs`, `PersistApplyTests.fs`, `FileAgentFailureTests.fs`, `ParseThreadLoadTests.fs`, and others.

### `CoreMailboxLoad.applyNotice`

[ ] Definition in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Caller `CoreMailboxLoad.dispatchItem` on `CoreQueue.Notice` after `CoreEventDispatch.logNotice` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Direct test calls in `tests/Server.Tests/CoreLoopNoticeTests.fs` and `PersistThreadTests.fs`.

### `CoreEventDispatch.postEvent`

[ ] Definition in `src/Server/Core/CoreEventDispatch.fs`.

[ ] Caller `CoreMailboxLoad.replyEdit` for `Order.Change (ops, ChangeOrigin.Edit)` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Caller `CoreMailboxEvents.dispatchPostGraphOnly` with `graphOnly = true` in `src/Server/Core/CoreMailboxEvents.fs`.

[ ] Caller `CoreMailboxActors` post path with `graphOnly = false` in `src/Server/Core/CoreMailboxActors.fs`.

[ ] Public mailbox wrapper `CoreMailboxApi.postEvent` in `src/Server/Core/CoreMailboxApi.fs` (async; reads stored `Ev` from log).

[ ] `postEvent` does not call `CoreMailboxApi.postNotice` today; unpersisted marking on Change runs inside `pushChangeSpecials` via `writeAxis` and `Notice.NodePersisting`.

## Existing tests

[ ] ``Op.SetDocumentState round-trip`` in `tests/Shared.Tests/SerializationTests.fs` checks `Serialization.encodeOp` / `decodeOp` round-trips `Op.SetDocumentState`.

[ ] ``SetDocumentState writes parseState and pullingFile together`` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs` checks `Op.apply` for parse and `pullingFile` on document nodes.

[ ] ``SetDocumentState includes the containing parent and filters unwritable root`` in `tests/Shared.Tests/DocumentOpImpactTests.fs` checks impact ops for a `SetDocumentState` change.

[ ] Multiple `HistoryTests.fs` cases apply and invert `Op.SetDocumentState` on file and directory nodes.

[ ] ``upload posts NodeUploading beside SetDocumentState``, ``git changed file posts NodeReceived beside SetDocumentState``, ``incoming file posts NodeReceived beside SetDocumentState``, ``import logs NodeParsed beside SetDocumentState``, ``planParseFile logs NodeParsed beside SetDocumentState``, and ``planDirectoryReconcile logs NodeParsed beside SetDocumentState`` in `tests/Server.Tests/NodeEventExpandTests.fs` assert inferred or logged Node notices alongside `Op.SetDocumentState` on Changes.

[ ] ``MarkUnparsed sets Unparsed and leaves PersistState`` in `tests/Server.Tests/CoreLoopNoticeTests.fs` calls `CoreMailboxLoad.applyNotice` with `Notice.MarkUnparsed`.

[ ] ``MarkUnparsed writes parse state to the database`` in `tests/Server.Tests/DbAgentNoticeTests.fs` posts `Notice.MarkUnparsed` through the mailbox and reads DB parse state.

[ ] ``Notice JSON round trip`` in `tests/Shared.Tests/EventJsonTests.fs` includes `Notice.MarkUnparsed` in the notice list.

[ ] ``NodeParsed pushes a persist request`` and ``ParseFinished sets Parsed and leaves PersistState`` in `tests/Server.Tests/CoreLoopNoticeTests.fs` call `CoreMailboxLoad.applyNotice` with `Notice.NodeParsed`.

[ ] ``NodeParsed writes parse state to the database`` in `tests/Server.Tests/DbAgentNoticeTests.fs` posts `Notice.NodeParsed` and checks DB.

[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` exercises unpersisted marking on repeated edits via `handlers.applyEvent` (uses `Notice.NodePersisting` in `replaceGraph` seeding, not `postNotice`).

[ ] No existing test named ``SetDocumentState leaves the Op list``, ``no SetDocumentState message case``, ``ordinary edit posts NodeUnpersisted``, ``parsed comes from NodeParsed``, or ``MarkUnparsed leaves Notice`` (those names are acceptance tests in the ticket only).

## Copy-from

[ ] Unpersisted graph write for a single node: `CoreMailboxLoad.writeUnpersisted` in `src/Server/Core/CoreMailboxLoad.fs` (used from `Notice.NodePersisting` and upload/received paths).

[ ] Persist-axis notice apply pattern: `Notice.NodePersisting` arm of `CoreMailboxLoad.applyNotice` calls `CoreEventDispatch.writeAxis` with `writeUnpersisted` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Edit-time unpersisted mark today: `CoreEventDispatch.markForPersist` in `src/Server/Core/CoreEventDispatch.fs` uses `writeAxis` with `Notice.NodePersisting` and `GraphMutate.setUnpersistedAt` inside `pushChangeSpecials`.

[ ] Unparsed-only notice apply pattern: `CoreMailboxLoad.writeParseState` and the `Notice.MarkUnparsed` arm of `applyNotice` in `src/Server/Core/CoreMailboxLoad.fs`.

[ ] Queue-and-log notice pattern: `CoreEventDispatch.logNotice` plus `CoreMailboxLoad.dispatchItem` on `CoreQueue.Notice` in `src/Server/Core/CoreMailboxLoad.fs` (same path `CoreMailboxApi.postNotice` uses).

[ ] Parsed-from-notice pattern: `CoreMailboxLoad.applyNodeParsed` in `src/Server/Core/CoreMailboxLoad.fs` (already the handler for `Notice.NodeParsed`).

## implement

### Ticket claims that differ from the code

[ ] [45 — SetDocumentState leaves; NodeUnpersisted replaces it](plan/core-refinement/issues/45-remove-document-state-set-ops.md) **NodeUnpersisted** contract item names `src/Server/Core/CoreMsg.fs`; the `Notice` union lives in `src/Shared/History.fs` (`CoreMsg.fs` defines `CoreMsg`, `Mailbox`, and `PersistHandlers` only).

[ ] The ticket **Change handler** item says `CoreEventDispatch.postEvent` posts `Notice.NodeUnpersisted` through `CoreMailboxApi.postNotice`; `postEvent` ends in `pushChangeSpecials`, which calls `markForPersist` and `CoreEventDispatch.writeAxis` with `Notice.NodePersisting` synchronously during `applyEvent`, with no `CoreMailboxApi.postNotice` call.

[ ] The ticket **Ordinary edit test** names `CoreEventDispatch.postEvent` as the seam that posts the notice; production edits from the mailbox go `CoreMailboxLoad.replyEdit` → `CoreEventDispatch.postEvent`, while HTTP client edits use `Api.postEvents` → `CoreDoor.postEvents` and a separate `inferredNodeEvents` → `postNotice` path for upload/import parse inference.

[ ] The ticket context says `Api.inferredNodeEvents` still matches `Op.SetDocumentState`; that match is present in `src/Server/Api.fs` (aligned with context, not yet removed until [57 — Upload Actor posts Node notices](plan/core-refinement/issues/57-upload-actor-posts-document-state.md)).

[ ] The ticket **Remove MarkUnparsed** item targets removal; `Notice.MarkUnparsed` remains on `Notice`, in `CoreMailboxLoad.applyNotice`, `EventJson`, `Database.writeNoticeRow`, and `ParseThread.markUnparsed` today.

[ ] The ticket **Leave CoreMsg and the Op list** item targets removal; `Op.SetDocumentState` remains on `Op`, in `Op.apply`, invert/undo, and `Serialization` codec, and in shared planners (`WorkspaceUploadStructure`, import/reconcile/upload paths) today.

[ ] The ticket **Parsed test** describes behavior already implemented: `CoreMailboxLoad.applyNotice` sets Parsed through `applyNodeParsed` on `Notice.NodeParsed` (covered by existing Core-loop notice tests, not by a test named ``parsed comes from NodeParsed``).

[ ] The ticket **No SetDocumentState case** acceptance item matches current code for message shapes: there is no `Notice.SetDocumentState` or `EventBody.SetDocumentState` case in `src/Shared/History.fs` or `EventJson.fs`.

### How each named new test observes the change at a public seam

[ ] ``SetDocumentState leaves the Op list`` — at the public `Gambol.Shared.Op` surface and `Serialization.decodeOp`: decoding a stored `"SetDocumentState"` payload fails or the union has no such case; a `CoreMsg` / `Ev` Change built through public codecs cannot carry that op.

[ ] ``no SetDocumentState message case`` — at `Gambol.Shared.Notice`, `Gambol.Shared.EventBody`, and `EventJson` encode/decode: no `SetDocumentState` notice tag and no extra `EventBody` case beyond `EventBody.Notice of Notice`.

[ ] ``ordinary edit posts NodeUnpersisted`` — at `CoreMailboxApi.postEvent` or end-to-end mailbox after an Edit `Order.Change`: the event log from `CoreMailboxApi.getEventsSince` or a test double on `postNotice` shows `EventBody.Notice (Notice.NodeUnpersisted nodeId)` for the owning special node, then `CoreMailboxLoad.applyNotice` (or `getState`) shows that node `PersistState.Unpersisted`.

[ ] ``parsed comes from NodeParsed`` — at `CoreMailboxLoad.applyNotice` with `Notice.NodeParsed`: graph parse state becomes Parsed (and existing push rules) without any `Op.SetDocumentState` on the Change.

[ ] ``MarkUnparsed leaves Notice`` — at `Gambol.Shared.Notice` and `CoreMailboxLoad.applyNotice`: the type has no `MarkUnparsed` case, `EventJson` has no `"markUnparsed"` tag, and applying a legacy payload is out of scope or fails decode.
