# 122 — Document state moves by Notice — Lazy load reconciliation scope

Scope for [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md). Slice: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) SetDocumentState sites in `src/Shared/dotnet/LazyLoadReconciliation.fs` and `src/Shared/dotnet/LazyLoadReconciliationApply.fs` also send an equivalent Notice through Mailbox. Keep SetDocumentState.

## 1 — Signatures

### Signatures

[ ] `Op.SetDocumentState` in `src/Shared/History.fs`: `SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum`.

[ ] `Notice` in `src/Shared/History.fs`: `NodeParsed`, `MarkUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, `NodePersisted`, `PersistFailed`.

[ ] `Ev.documentStateFromNotice` in `src/Shared/History.fs`: `Notice -> (NodeId * DocumentStateEnum) option`.

[ ] `DocumentOpImpact.noticesBeside` in `src/Shared/DocumentOpImpact.fs`: `Op list -> Notice list`.

[ ] `LazyLoadReconciliationApply.applyOps` in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`: `Graph -> Op list -> Result<Graph, string>`.

[ ] `LazyLoadReconciliationApply.markUnparsed` in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`: `Graph -> NodeId -> Op list`.

[ ] `LazyLoadReconciliationApply.markParentCurrent` (private) in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`: `Graph -> NodeId -> Op list`.

[ ] `LazyLoadReconciliationApply.markAddedDocumentsUnparsed` in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`: `Graph -> NodeId -> LazyLoadReconciliationPath.PathInfo list -> Op list -> Result<Graph * Op list, string>`.

[ ] `LazyLoadReconciliationApply.parseDirInfoIfPresent` in `src/Shared/dotnet/LazyLoadReconciliationApply.fs`: `Graph -> NodeId -> Map<string, string> -> LazyLoadReconciliationPath.PathInfo -> Result<Graph * Op list, string>`.

[ ] `LazyLoadReconciliation.unparsedStateOp` (private) in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> NodeId -> Op`.

[ ] `LazyLoadReconciliation.containingDirectoryMarkOps` in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> string -> ChangedPath list -> Result<Op list, string>`.

[ ] `LazyLoadReconciliationReport.Report` in `src/Shared/dotnet/LazyLoadReconciliationReport.fs`: `{ ops: Op list; failures: Failure list }`.

[ ] `LazyLoadReconciliationServer.postReconcileReport` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `ListedReconcile -> LazyLoadReconciliationReport.Report -> Async<Result<Failure list, string>>`.

[ ] `CoreMailboxApi.postNotice` in `src/Server/Core/CoreMailboxApi.fs`: `MailboxHost -> Notice -> unit`.

[ ] `FileAgent.postGraphOnlyNotices` (private) in `src/Server/Core/FileAgent.fs`: after `postGraphOnly`, iterates `DocumentOpImpact.noticesBeside` on `EventBody.Change` ops when `persistFinished` is bound.

[ ] `DbAgent.commitPostChange` in `src/Server/Core/DbAgent.fs`: same pattern via `DatabaseProjection.noticesBeside` when `graphOnly`.

## 2 — SetDocumentState sites (both slice files)

[ ] `LazyLoadReconciliationApply.fs` line 23, function `markUnparsed`: `Op.SetDocumentState(nodeId, Current, Unparsed)` when current enum is `Current`.

[ ] `LazyLoadReconciliationApply.fs` line 25, function `markUnparsed`: `Op.SetDocumentState(nodeId, PullingFile, Unparsed)` when current enum is `PullingFile`.

[ ] `LazyLoadReconciliationApply.fs` line 32, function `markParentCurrent`: `Op.SetDocumentState(nodeId, Unparsed, Current)` when node is `Special (Directory | Workspace)` and legacy state is `Unparsed`.

[ ] `LazyLoadReconciliationApply.fs` line 145, function `parseDirInfoIfPresent` (`markCurrent` local): `Op.SetDocumentState(nodeId, Unparsed, Current)` when `docState` is `Unparsed` before artifact parse.

[ ] `LazyLoadReconciliationApply.fs` lines 147–150, function `parseDirInfoIfPresent` (`markCurrent` local): `Op.SetDocumentState(nodeId, PullingFile, Current)` when `docState` is `PullingFile` before artifact parse.

[ ] `LazyLoadReconciliation.fs` lines 370–373, function `unparsedStateOp`: `Op.SetDocumentState(nodeId, Node.documentStateEnum graph.nodes.[nodeId], Unparsed)` (old state read from graph; new state always `Unparsed`).

[ ] No other `SetDocumentState` occurrences exist in `LazyLoadReconciliation.fs` or `LazyLoadReconciliationApply.fs`.

## 3 — Match sites

### Match sites

[ ] Constructor `Op.SetDocumentState` applied in `Op.apply` / `Op.applyAllowing` in `src/Shared/History.fs` via `Graph.setDocumentState nodeId oldState newState`.

[ ] Constructor `Notice.MarkUnparsed`, `Notice.NodeReceived`, `Notice.NodeParsed`, `Notice.NodeUploading` applied in `Ev.apply` in `src/Shared/History.fs` via `applyNotice` and `documentStateFromNotice`.

[ ] `markUnparsed` caller `foldDocumentStateOps` in `LazyLoadReconciliationApply.fs` calls `applyOps` then accumulates ops; used by `markAddedDocumentsUnparsed`.

[ ] `markUnparsed` caller `LazyLoadReconciliation.invalidateOrParseModified` in `LazyLoadReconciliation.fs` (alias to `LazyLoadReconciliationApply.markUnparsed`) when modified path is not dir-info with artifact text.

[ ] `markUnparsed` caller `LazyLoadReconciliationReport.renamedFileUnparsedOps` in `LazyLoadReconciliationReport.fs` for successful file renames on pull plan.

[ ] `markParentCurrent` caller `markAddedDocumentsUnparsed` in `LazyLoadReconciliationApply.fs` after stub `markUnparsed` on parent ids from `ownerParentByChild`.

[ ] `parseDirInfoIfPresent` callers: `LazyLoadReconciliation.invalidateOrParseModified`, `LazyLoadReconciliationReport.finalizeAdded`, `LazyLoadReconciliationApply.parseDirInfoInfos`; orchestrated from `LazyLoadReconciliation.planChangedPathsWithArtifacts` and `LazyLoadReconciliationReport.planChangedPathsWithArtifacts`.

[ ] `unparsedStateOp` caller `containingDirectoryMarkOps` in `LazyLoadReconciliation.fs`; `containingDirectoryMarkOps` caller `LazyLoadReconciliationReport.planReceivedFiles` only.

[ ] Server caller `LazyLoadReconciliationServer.reconcileListed` plans via `LazyLoadReconciliationReport`, then `postReconcileReport` posts `report.ops` with `GraphOnlyChangePost.postChunks` and `CoreDoor.postGraphOnly` command name `"Parse"`.

[ ] Server `ListedReconcile.beside` callback: `ignore` for most reconcile entry points; `reconcileReceivedFiles` sets `beside` to post `Notice.NodeReceived` per id from `receivedFileIds` after successful post.

[ ] `receivedFileIds` in `LazyLoadReconciliationServer.fs` matches `Op.SetDocumentState(found, _, Unparsed)` in the posted op list to detect rename unparsed marks.

## 4 — Notice equivalence per site

[ ] Site `markUnparsed` `Current` → `Unparsed`: equivalent Notice is `Notice.MarkUnparsed nodeId` (`DocumentOpImpact.noticesBeside` and `Ev.documentStateFromNotice`).

[ ] Site `markUnparsed` `PullingFile` → `Unparsed`: equivalent Notice is `Notice.NodeReceived nodeId` (arrival rule in `DocumentOpImpact.noticesBeside`).

[ ] Site `markParentCurrent` `Unparsed` → `Current`: equivalent Notice is `Notice.NodeParsed nodeId` when no other op in the same batch ends in `Unparsed` or `PullingFile` (`uploadOrArrival` gate in `DocumentOpImpact.noticesBeside`).

[ ] Site `parseDirInfoIfPresent` `Unparsed` → `Current`: same as `markParentCurrent` — `Notice.NodeParsed nodeId`, subject to batch `uploadOrArrival` suppression when combined with stub unparsed marks in one post chunk.

[ ] Site `parseDirInfoIfPresent` `PullingFile` → `Current`: graph write matches `Notice.NodeParsed nodeId`, but `DocumentOpImpact.noticesBeside` does not emit any Notice for `(PullingFile, Current)` (only `(Unparsed, Current)` matches `NodeParsed`).

[ ] Site `unparsedStateOp` any → `Unparsed`: if old is `Current`, `Notice.MarkUnparsed`; if old is `PullingFile`, `Notice.NodeReceived`; if old is already `Unparsed`, op still emitted and Notice should be idempotent no-op on mailbox (`MarkUnparsed` / `NodeReceived` re-apply same enum via `applyNotice`).

[ ] `markUnparsed` when already `Unparsed` returns `[]` (idempotent at op level); other sites guard with state match before emitting `SetDocumentState`.

## 5 — Mailbox call chain (design)

[ ] `LazyLoadReconciliation.fs` and `LazyLoadReconciliationApply.fs` have no `MailboxHost`, `CoreDoor`, or `CoreMailboxApi` references.

[ ] Posted reconcile ops reach Mailbox only through `LazyLoadReconciliationServer.postReconcileReport` → `CoreDoor.postGraphOnly` → agent `postGraphOnlyNotices` → `DocumentOpImpact.noticesBeside` → `CoreMailboxApi.postNotice` (when persist slot is bound).

[ ] `reconcileReceivedFiles` additionally posts `Notice.NodeReceived` in `beside` after post, for ids from `receivedFileIds` (including modified files where `noticesBeside` would emit `MarkUnparsed` instead).

[ ] Local `applyOps` inside planners simulates graph for planning; it does not post Notices. Only ops that appear in `Report.ops` and are posted through the server door affect Mailbox document-state Notices today.

[ ] Implement slice cannot call `postNotice` inside Shared/dotnet without new dependencies; Notices must be collected at SetDocumentState sites and posted by the server owner (`beside`, extended `Report`, or equivalent), or rely on agent `noticesBeside` with known gaps for `PullingFile` → `Current` and batched `Unparsed` → `Current`.

## 6 — Copy-from

### Copy-from

[ ] Primary helper: `DocumentOpImpact.noticesBeside` in `src/Shared/DocumentOpImpact.fs` — maps each `Op.SetDocumentState` in an op list to zero or one document-state Notice (`Notice.NodeUploading`, `Notice.NodeReceived`, `Notice.MarkUnparsed`, `Notice.NodeParsed`).

[ ] Mailbox post pattern: `FileAgent.postGraphOnlyNotices` in `src/Server/Core/FileAgent.fs` — runs `DocumentOpImpact.noticesBeside` on posted `EventBody.Change` ops and calls the bound `postNotice` slot (`CoreMailboxApi.postNotice`).

[ ] Explicit parse post pattern: `Api.postParseFile` in `src/Server/Api.fs` — after `CoreDoor.postGraphOnly`, calls `CoreMailboxApi.postNotice handle.host (Notice.NodeParsed fileId)`.

[ ] Lazy-load pull pattern: `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs` — `beside` iterates `CoreMailboxApi.postNotice workspace.handle.host (Notice.NodeReceived id)`.

[ ] `DirectoryReconcile.fs` (ticket Look-at checked) still only plans `Op.SetDocumentState`; equivalence is inferred at post time via the same `noticesBeside` path, not in the planner file.

## 7 — Existing tests

### Existing tests

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs` — ``upload-built directories become current when members are added`` — `planAddedPathsWithArtifacts` leaves Directory/Workspace stubs `Current` after adds (exercises `markAddedDocumentsUnparsed` / `markParentCurrent` ops via graph enum asserts).

[ ] Same file — ``exact amb add marks directory stub unparsed`` — added `.amb` path leaves directory stub `Unparsed` (`markUnparsed` / stub path).

[ ] Same file — ``exact amb add with text parses outline immediately`` — artifact text on add produces parse ops and `Current` directory (`parseDirInfoIfPresent`).

[ ] Same file — ``exact amb modify with text reparses instead of leaving unparsed`` — modified dir-info with artifact re-parses (`invalidateOrParseModified` → `parseDirInfoIfPresent` or `markUnparsed`).

[ ] Same file — ``modified file becomes unparsed without losing identity or placement`` — modified file plan marks file `Unparsed` (`markUnparsed` via `invalidateOrParseModified`).

[ ] Same file — ``received file add marks the containing directory Unparsed`` — `planReceivedFiles` / `containingDirectoryMarkOps` marks parent directory `Unparsed`.

[ ] Same file — ``received file modification leaves the containing directory Current`` — modified file marks file not directory (`containingDirectoryMarkOps` not applied to directory for Modified-only pull semantics in test setup).

[ ] Same file — ``received file plan marks top and nested files and leaves the directory`` — file-level unparsed marks from pull plan without marking directory `Current`.

[ ] Same file — ``rediscovered Unparsed Directory File still parses on Added`` — `parseDirInfoIfPresent` promotes `Unparsed` → `Current` when parsing.

[ ] `tests/Server.Tests/LazyLoadReconciliationServerTests.fs` — ``server reconciler applies planner ops through active agent`` — end-to-end post of planner ops through live agent graph.

[ ] Same file — ``git pull marks received files at any depth and does not discover`` — `reconcileReceivedFiles` posts plan including directory mark ops.

[ ] Same file — ``git pull add marks the containing directory Unparsed`` — server pull adds mark containing directory `Unparsed` in agent graph after reconcile.

[ ] No test file references `LazyLoadReconciliationApply` by module name; coverage is through `LazyLoadReconciliation` and `LazyLoadReconciliationReport` public planners and server reconcile.

[ ] No existing test asserts Mailbox Notice events for lazy-load SetDocumentState transitions (tests assert graph `documentState` after `applyOps` or server graph state).

## 8 — implement

### implement

[ ] [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) claims each SetDocumentState site also sends an equivalent Notice through Mailbox; the two slice files today emit only `Op.SetDocumentState` in op lists.

[ ] Agent `postGraphOnlyNotices` already sends `DocumentOpImpact.noticesBeside` for posted reconcile ops, so many transitions get Mailbox Notices without code in Shared/dotnet; that path does not satisfy the ticket checkbox on the planner files until explicit notice pairing exists at those sites or the report/plumbing carries notices from them.

[ ] `DocumentOpImpact.noticesBeside` omits `Notice.NodeParsed` for `PullingFile` → `Current` (`parseDirInfoIfPresent` line 147 site); implement must post `Notice.NodeParsed` explicitly for that transition to match `SetDocumentState`.

[ ] `DocumentOpImpact.noticesBeside` suppresses all `Notice.NodeParsed` in a batch when any SetDocumentState in the batch has new state `Unparsed` or `PullingFile` (`uploadOrArrival`); `markAddedDocumentsUnparsed` batches stub `MarkUnparsed` ops with parent `Unparsed` → `Current` in one post — parent `NodeParsed` notices require explicit posting outside `noticesBeside`.

[ ] `reconcileReceivedFiles` `beside` posts `Notice.NodeReceived` for modified files where planner emits `Current` → `Unparsed` (`MarkUnparsed` would be the `noticesBeside` mapping); implement must preserve or reconcile that intentional difference when adding site-level notices.

[ ] [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) Tests section: no new test is required.

[ ] No named new test is specified; observation at a public seam would be Mailbox Notice history or resident projection after `reconcileListed` / `postGraphOnly`, not covered by current lazy-load tests.
