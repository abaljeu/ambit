# 45 — SetDocumentState leaves the Op list scope

Scope for one expand slice: remove `Op.SetDocumentState` from the `Op` union, from `Op.apply`, from `Op.invert`, and from Op JSON in [Serialization.fs](../../src/Shared/Serialization.fs). Ticket target: [45 — SetDocumentState leaves the Op list](../issues/45-remove-document-state-set-ops.md) (filename stays `45-remove-document-state-set-ops.md`; title becomes **45 — SetDocumentState leaves the Op list**). Mode: write-ticket. Blocked until migrate tickets empty production constructors: [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md), [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md), [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md), [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md). Out of scope for this contract: add `Notice.NodeUnpersisted`; remove `Notice.MarkUnparsed`; any `SetDocumentState` message case (already absent; Parsed comes from `Notice.NodeParsed`).

## Confirmations

[ ] `Notice` in [History.fs](../../src/Shared/History.fs) has no `SetDocumentState` case (cases are `NodeParsed`, `NodePersisted`, `MarkUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, `PersistFailed`).
[ ] `EventBody` in [History.fs](../../src/Shared/History.fs) has no `SetDocumentState` case (cases are `Change`, `Undo`, `Redo`, `ActorStart`, `ActorStop`, `Load`, `Cancel`, `Notice`).
[ ] Op JSON is owned by `Gambol.Shared.Serialization.encodeOp` and `Gambol.Shared.Serialization.decodeOp` in [Serialization.fs](../../src/Shared/Serialization.fs); [EventJson.fs](../../src/Shared/EventJson.fs) `encodeOps` / `decodeOps` map event-body op lists through those functions only.

## Signatures

[ ] `Gambol.Shared.DocumentStateEnum` in [Model.fs](../../src/Shared/Model.fs): `Current | Unparsed | PullingFile`.
[ ] `Gambol.Shared.Op` in [History.fs](../../src/Shared/History.fs) includes `SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum` (this slice removes that case).
[ ] `Gambol.Shared.Op.apply` in [History.fs](../../src/Shared/History.fs): `(op: Op) -> (state: State) -> ApplyResult`.
[ ] `Gambol.Shared.Op.invert` in [History.fs](../../src/Shared/History.fs): `(op: Op) -> Op`.
[ ] `Gambol.Shared.Op.invertAll` in [History.fs](../../src/Shared/History.fs): `(ops: Op list) -> Op list` (calls `Op.invert`).
[ ] `Gambol.Shared.Op.undo` in [History.fs](../../src/Shared/History.fs): `(op: Op) -> (state: State) -> ApplyResult` (private `undoAllowed` matches `Op.SetDocumentState` today).
[ ] `Gambol.Shared.Serialization.encodeOp` in [Serialization.fs](../../src/Shared/Serialization.fs): `(op: Op) -> IEncodable`.
[ ] `Gambol.Shared.Serialization.decodeOp` in [Serialization.fs](../../src/Shared/Serialization.fs): `Decoder<Op>`.
[ ] `Gambol.Shared.GraphMutate.setDocumentState` in [GraphMutate.fs](../../src/Shared/GraphMutate.fs): `(nodeId: NodeId) -> (_oldState: DocumentStateEnum) -> (newState: DocumentStateEnum) -> (graph: Graph) -> Result<Graph, string>`.
[ ] `Gambol.Shared.Graph.setDocumentState` in [GraphOps.fs](../../src/Shared/GraphOps.fs): static member forwarding to `GraphMutate.setDocumentState`.

## Match sites

### `Op` union case (constructor on the type)

[ ] [History.fs](../../src/Shared/History.fs) `type Op` — case `SetDocumentState of nodeId * oldState * newState` (removed in this slice).

### `Op.apply` and undo path

[ ] [History.fs](../../src/Shared/History.fs) `Op.involvedNodeIds` — `| Op.SetDocumentState _ -> []`.
[ ] [History.fs](../../src/Shared/History.fs) `Op.applyAllowed` — `| Op.SetDocumentState(nodeId, oldState, newState) ->` calls `Graph.setDocumentState nodeId oldState newState state.graph`.
[ ] [History.fs](../../src/Shared/History.fs) `Op.invert` — `| Op.SetDocumentState(id, old, new_) -> Op.SetDocumentState(id, new_, old)`.
[ ] [History.fs](../../src/Shared/History.fs) `Op.undoAllowed` — `| Op.SetDocumentState(nodeId, oldState, newState) ->` calls `Graph.setDocumentState nodeId newState oldState state.graph`.
[ ] [History.fs](../../src/Shared/History.fs) `ChangeValidation.opChangesGraphShape` — `| Op.SetDocumentState _` grouped with metadata ops that do not change graph shape.

### Serialization

[ ] [Serialization.fs](../../src/Shared/Serialization.fs) `encodeOp` — `| Op.SetDocumentState(nodeId, oldState, newState) ->` emits `"type": "SetDocumentState"`.
[ ] [Serialization.fs](../../src/Shared/Serialization.fs) `decodeOp` — `| "SetDocumentState" ->` builds `Op.SetDocumentState(...)`.

### Other production `match` arms on `Op.SetDocumentState` (removed or narrowed when the case leaves the union)

[ ] [DocumentOpImpact.fs](../../src/Shared/DocumentOpImpact.fs) `opAnchor` — `| Op.SetDocumentState(id, _, _)`.
[ ] [DocumentOpImpact.fs](../../src/Shared/DocumentOpImpact.fs) `touchedNodeIds` — `| Op.SetDocumentState(nodeId, _, _) -> [ nodeId ]`.
[ ] [ResidentProjection.fs](../../src/Shared/ResidentProjection.fs) `applyOpAllowing` — `| Op.SetDocumentState(nodeId, _, _)` with header-fact apply path.
[ ] [DatabaseProjection.fs](../../src/Server/DatabaseProjection.fs) `nodeIdFromOp` — `| Op.SetDocumentState(nodeId, _, _)`.
[ ] [DocumentColdParse.fs](../../src/Shared/documents/DocumentColdParse.fs) `targetsDocumentRoot` — `| Op.SetDocumentState(id, _, _) when id = documentRootId`.
[ ] [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs) — `| Op.SetDocumentState(found, _, Unparsed) -> found = id` inside reconcile filtering.
[ ] [Api.fs](../../src/Server/Api.fs) `inferredNodeEvents` — `| Op.SetDocumentState(id, oldState, newState) -> Some(id, oldState, newState)`; caller `Api.postEvents` collects notices from `inferredNodeEvents` (arm removed when [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) lands).

### Production `Op.SetDocumentState` constructors (stay until migrate tickets; grouped by ticket that empties them)

#### [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md)

[ ] [WorkspaceUploadStructure.fs](../../src/Shared/WorkspaceUploadStructure.fs) `markPullingFile` — `Op.SetDocumentState(nodeId, Current, PullingFile)` and `Op.SetDocumentState(nodeId, Unparsed, PullingFile)`.
[ ] [WorkspaceUploadStructure.fs](../../src/Shared/WorkspaceUploadStructure.fs) `markNewStubsUnparsed` — calls `markPullingFile` on new File stub ids via `foldStateOps`.

#### [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md)

[ ] [WorkspaceUploadStructure.fs](../../src/Shared/WorkspaceUploadStructure.fs) `planServerFilePresentOps` — `Op.SetDocumentState(fileId, PullingFile, Unparsed)`.
[ ] [WorkspaceUploadStructure.fs](../../src/Shared/WorkspaceUploadStructure.fs) `markUnparsed` — `Op.SetDocumentState(nodeId, Current, Unparsed)` and `Op.SetDocumentState(nodeId, PullingFile, Unparsed)` (used from upload/reconcile stub marking).
[ ] [LazyLoadReconciliationApply.fs](../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `markUnparsed` — same transition pairs as `WorkspaceUploadStructure.markUnparsed`.
[ ] [LazyLoadReconciliationApply.fs](../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `markAddedDocumentsUnparsed` — emits via `markUnparsed` on added stub ids.
[ ] [LazyLoadReconciliation.fs](../../src/Shared/dotnet/LazyLoadReconciliation.fs) `unparsedStateOp` — `Op.SetDocumentState(nodeId, Node.documentStateEnum graph.nodes.[nodeId], Unparsed)`.
[ ] [LazyLoadReconciliation.fs](../../src/Shared/dotnet/LazyLoadReconciliation.fs) `containingDirectoryMarkOps` — maps directory ids through `unparsedStateOp`.

#### [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md)

[ ] [ImportText.fs](../../src/Shared/ImportText.fs) `markDocumentCurrentBeforeParse` — `Op.SetDocumentState(focusId, Unparsed, Current)`.
[ ] [ImportDocument.fs](../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` — `Op.SetDocumentState(fileId, Unparsed, Current)` in the `Unparsed` branch of `markCurrent`.
[ ] [DirectoryReconcile.fs](../../src/Shared/dotnet/DirectoryReconcile.fs) `markCurrent` — `Op.SetDocumentState(directoryId, Unparsed, Current)`.
[ ] [LazyLoadReconciliationApply.fs](../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `parseDirInfoIfPresent` local `markCurrent` — `Op.SetDocumentState(nodeId, Unparsed, Current)` and `Op.SetDocumentState(nodeId, PullingFile, Current)`.
[ ] [WorkspaceUploadStructure.fs](../../src/Shared/WorkspaceUploadStructure.fs) `markParentCurrent` — `Op.SetDocumentState(nodeId, Unparsed, Current)` (parent Directory/Workspace promotion after stubs).

#### [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md)

[ ] [Api.fs](../../src/Server/Api.fs) `inferredNodeEvents` — reads `Op.SetDocumentState` on `EventBody.Change` ops (no constructor; inference removed with the function).

## Existing tests

[ ] [SpecialNodeStateAxesTests.fs](../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs) ``SetDocumentState writes parseState and pullingFile together`` — `Op.apply` on `Op.SetDocumentState` for Current↔Unparsed↔PullingFile; asserts `parseState`, `pullingFile`, and persist axis on the node.
[ ] [DocumentOpImpactTests.fs](../../tests/Shared.Tests/DocumentOpImpactTests.fs) ``SetDocumentState includes the containing parent and filters unwritable root`` — `Op.SetDocumentState(fileAId, Current, PullingFile)`; `documentRootsAffectedByOps` returns the owning Directory, not the File id.
[ ] [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs) ``Op.SetDocumentState round-trip`` — `Serialization.encodeOp` / `Serialization.decodeOp` round-trip for `Op.SetDocumentState`.
[ ] [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs) ``PullingFile round-trip`` — embeds `Op.SetDocumentState(node.id, PullingFile, Unparsed)` in op JSON assertions (`noServerFile` wire label).
[ ] [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs) ``PullingFile node and state op round-trip`` — round-trips `Op.SetDocumentState(node.id, PullingFile, Unparsed)`.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``upload posts NodeUploading beside SetDocumentState`` — client Change with `Op.SetDocumentState` to PullingFile; expects `Notice.NodeUploading` beside the op.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``git changed file posts NodeReceived beside SetDocumentState`` — reconcile Change with PullingFile→Unparsed op; expects `Notice.NodeReceived`.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``incoming file posts NodeReceived beside SetDocumentState`` — arrival Change; expects `Notice.NodeReceived`.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``import logs NodeParsed beside SetDocumentState`` — import Change with Unparsed→Current op; expects `Notice.NodeParsed`.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``planParseFile logs NodeParsed beside SetDocumentState`` — parse-file plan Change; expects `Notice.NodeParsed`.
[ ] [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``planDirectoryReconcile logs NodeParsed beside SetDocumentState`` — directory reconcile Change; expects `Notice.NodeParsed`.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``edit of no-server-file document is rejected`` — applies `Op.SetDocumentState(fileId, Unparsed, PullingFile)` then asserts content edit blocked.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``parse state transition before tree mutation succeeds and reverse order fails`` — parse batch leads with `Op.SetDocumentState(fileId, Unparsed, Current)`.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``marking document unparsed remains legal`` — `Op.apply (Op.SetDocumentState(otherId, Current, Unparsed))`.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``valid parse batch can replay undo and redo`` — batch includes `Op.SetDocumentState(fileId, Unparsed, Current)` with invert/ redo.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``nested file parse under current directory replaces file tree`` — uses `Op.SetDocumentState` on directory and file for parse setup.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``nested file parse still allowed when enclosing directory is unparsed`` — parse batch with `Op.SetDocumentState(fileId, Unparsed, Current)`.
[ ] [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) ``unparsed invariant also applies to directory and workspace documents`` — `Op.SetDocumentState(directoryId, Unparsed, Current)` before rename attempt.
[ ] No existing test is named ``SetDocumentState leaves the Op list``.

## Copy-from

[ ] [GraphMutate.fs](../../src/Shared/GraphMutate.fs) `setDocumentState` — today’s graph write behind `Op.applyAllowed` for document-state transitions (migrate tickets move producers to Notice handlers per [44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md)).
[ ] [GraphOps.fs](../../src/Shared/GraphOps.fs) `Graph.setDocumentState` — same write at the `Graph` API surface tests use in [SpecialNodeStateAxesTests.fs](../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs) via direct `Graph.setPersistState` / apply helpers.

## write-ticket

### Source files this slice would change

[ ] [History.fs](../../src/Shared/History.fs) — remove `SetDocumentState` from `type Op`; remove arms in `Op.involvedNodeIds`, `Op.applyAllowed`, `Op.invert`, `Op.undoAllowed`, and `ChangeValidation.opChangesGraphShape`.
[ ] [Serialization.fs](../../src/Shared/Serialization.fs) — remove `SetDocumentState` from `encodeOp` and `decodeOp`.
[ ] [DocumentOpImpact.fs](../../src/Shared/DocumentOpImpact.fs) — remove `Op.SetDocumentState` arms from `opAnchor` and `touchedNodeIds`.
[ ] [ResidentProjection.fs](../../src/Shared/ResidentProjection.fs) — remove `Op.SetDocumentState` from `applyOpAllowing` header-fact group.
[ ] [DatabaseProjection.fs](../../src/Server/DatabaseProjection.fs) — remove `Op.SetDocumentState` from `nodeIdFromOp`.
[ ] [DocumentColdParse.fs](../../src/Shared/documents/DocumentColdParse.fs) — remove `Op.SetDocumentState` from `targetsDocumentRoot`.

### Tests this slice would add or change

[ ] Add [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) or [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs) test ``SetDocumentState leaves the Op list`` — `Op` has no `SetDocumentState` case; `Serialization.decodeOp` rejects legacy `"SetDocumentState"` JSON; a `CoreMsg` Change does not carry that op (acceptance from [45 — SetDocumentState leaves the Op list](../issues/45-remove-document-state-set-ops.md) Op-list contract).
[ ] Change [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs) — remove or replace ``Op.SetDocumentState round-trip``, ``PullingFile round-trip`` op leg, and ``PullingFile node and state op round-trip``.
[ ] Change [SpecialNodeStateAxesTests.fs](../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs) — replace ``SetDocumentState writes parseState and pullingFile together`` op-based setup with `Graph.setDocumentState` or Notice-driven paths after migrate tickets land.
[ ] Change [DocumentOpImpactTests.fs](../../tests/Shared.Tests/DocumentOpImpactTests.fs) — replace ``SetDocumentState includes the containing parent and filters unwritable root`` if `Op.SetDocumentState` is no longer a dirtied op shape.
[ ] Change [HistoryTests.fs](../../tests/Shared.Tests/HistoryTests.fs) — rewrite unparsed/parse batches that lead with `Op.SetDocumentState` to use graph setup or notices consistent with post-migrate producers.
[ ] Change [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) — drop `Op.SetDocumentState` from Changes once [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) posts notices without inferring from ops (tests may keep notice assertions, rename away “beside SetDocumentState”).
