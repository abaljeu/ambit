# [133 — Received file container drops its SetDocumentState](../issues/133-received-file-container-drops-setdocumentstate.md) scope

Scope mode: implement. Ticket: [133 — Received file container drops its SetDocumentState](../issues/133-received-file-container-drops-setdocumentstate.md). Related rounds: [130 — Reconcile markUnparsed drops its SetDocumentState](../issues/130-reconcile-markunparsed-drops-setdocumentstate.md), [131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md), [132 — Dir info parse drops its SetDocumentState](../issues/132-dir-info-parse-drops-setdocumentstate.md).

### Signatures

[ ] `LazyLoadReconciliationReport.Report` in `src/Shared/dotnet/LazyLoadReconciliationReport.fs`: `{ ops: Op list; failures: Failure list; parsedParentIds: NodeId list; unparsedIds: NodeId list; parsedDirIds: NodeId list }`.

[ ] `LazyLoadReconciliationReport.planChangedPathsWithArtifacts` in `src/Shared/dotnet/LazyLoadReconciliationReport.fs`: `Graph -> string -> LazyLoadReconciliation.ChangedPath list -> Map<string, string> -> Result<Report, string>`.

[ ] `LazyLoadReconciliationReport.planReceivedFiles` in `src/Shared/dotnet/LazyLoadReconciliationReport.fs`: `Graph -> string -> LazyLoadReconciliation.ChangedPath list -> Map<string, string> -> Result<Report, string>`.

[ ] `LazyLoadReconciliationReport.renamedFileUnparsedOps` (private) in `src/Shared/dotnet/LazyLoadReconciliationReport.fs`: `Graph -> string -> LazyLoadReconciliation.ChangedPath list -> Failure list -> Op list * NodeId list`.

[ ] `LazyLoadReconciliation.containingDirectoryMarkOps` in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> string -> LazyLoadReconciliation.ChangedPath list -> Result<Op list, string>`.

[ ] `LazyLoadReconciliation.unparsedStateOp` (private) in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> NodeId -> Op`.

[ ] `LazyLoadReconciliation.containingDirectoryIds` (private) in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> NodeId -> LazyLoadReconciliation.ChangedPath list -> NodeId list`.

[ ] `LazyLoadReconciliation.nearestExistingDirectory` (private) in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `Graph -> NodeId -> string list -> NodeId`.

[ ] `LazyLoadReconciliation.receivedFileChanges` in `src/Shared/dotnet/LazyLoadReconciliation.fs`: `LazyLoadReconciliation.ChangedPath list -> LazyLoadReconciliation.ChangedPath list`.

[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs`: `WorkspaceReconcile -> LazyLoadReconciliation.ChangedPath list -> Async<Result<LazyLoadReconciliationReport.Failure list, string>>`.

[ ] `LazyLoadReconciliationServer.postReconcileReport` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `ListedReconcile -> LazyLoadReconciliationReport.Report -> Async<Result<LazyLoadReconciliationReport.Failure list, string>>`.

[ ] `LazyLoadReconciliationServer.postParsedParents` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `MailboxHost -> NodeId list -> unit`.

[ ] `LazyLoadReconciliationServer.postCurrentUnparsed` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `MailboxHost -> Graph -> NodeId list -> unit`.

[ ] `LazyLoadReconciliationServer.postNotices` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `MailboxHost -> (NodeId -> Notice) -> NodeId list -> unit`.

[ ] `LazyLoadReconciliationServer.receivedFileIds` (private) in `src/Server/LazyLoadReconciliationServer.fs`: `Graph -> string -> LazyLoadReconciliation.ChangedPath list -> Op list -> NodeId list`.

[ ] Target shape (ticket): `Report` gains a `NodeId list` for containing Directory or Workspace ids from the same choice as `containingDirectoryMarkOps` (name unstated on ticket; mirror private `containingDirectoryIds`).

[ ] Target shape (ticket): `containingDirectoryMarkOps` stops returning `SetDocumentState` ops; `unparsedStateOp` is deleted.

[ ] Target shape (ticket): `postReconcileReport` posts `Notice.NodeUnparsed` for each Current id in that new list on the core mailbox, after `postParsedParents` and before or alongside existing `postCurrentUnparsed` for `unparsedIds`, with no `DocumentOpImpact.noticesBeside` and no op scan.

### Match sites

[ ] `LazyLoadReconciliationReport.Report` record definition: `src/Shared/dotnet/LazyLoadReconciliationReport.fs`, type `Report`.

[ ] `Report` constructor in `planChangedPathsWithArtifacts`: `src/Shared/dotnet/LazyLoadReconciliationReport.fs`, function `planChangedPathsWithArtifacts`, `Ok { ops; failures; parsedParentIds; unparsedIds; parsedDirIds }`.

[ ] `Report` update in `planReceivedFiles`: `src/Shared/dotnet/LazyLoadReconciliationReport.fs`, function `planReceivedFiles`, `{ report with ops = …; unparsedIds = … }`.

[ ] `report.parsedParentIds` read: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, inside `postMarks`.

[ ] `report.unparsedIds` read: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, `postCurrentUnparsed host listed.graph report.unparsedIds`.

[ ] `report.parsedDirIds` read: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, `postDirParsing` and `postDirParsed`.

[ ] `report.ops` read: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, `GraphOnlyChangePost.postChunks` branch.

[ ] `report.failures` read: `src/Server/LazyLoadReconciliationServer.fs`, functions `logFailures` and `postReconcileReport` return value.

[ ] `planned.unparsedIds` read: `tests/Server.Tests/NodeEventExpandTests.fs`, fact ``git load reconcile posts NodeUnparsed for a modified file``.

[ ] `unparsedStateOp` definition: `src/Shared/dotnet/LazyLoadReconciliation.fs`, private function `unparsedStateOp`.

[ ] `unparsedStateOp` call: `src/Shared/dotnet/LazyLoadReconciliation.fs`, function `containingDirectoryMarkOps`, `List.map (unparsedStateOp graph)`.

[ ] `containingDirectoryMarkOps` definition: `src/Shared/dotnet/LazyLoadReconciliation.fs`, module `LazyLoadReconciliation`.

[ ] `containingDirectoryMarkOps` caller: `src/Shared/dotnet/LazyLoadReconciliationReport.fs`, function `planReceivedFiles`, `Result.bind` after `planChangedPathsWithArtifacts`.

[ ] `containingDirectoryIds` call: `src/Shared/dotnet/LazyLoadReconciliation.fs`, function `containingDirectoryMarkOps`, inside `Result.map`.

[ ] `containingDirectoryIds` helpers: `changedFileParents`, `fileParentParts`, `nearestExistingDirectory` in `src/Shared/dotnet/LazyLoadReconciliation.fs`.

[ ] `planReceivedFiles` caller: `src/Server/LazyLoadReconciliationServer.fs`, function `reconcileReceivedFiles`, `reconcileListed LazyLoadReconciliationReport.planReceivedFiles`.

[ ] `planReceivedFiles` caller: `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, helper `requireReceivedPlan`.

[ ] `planReceivedFiles` caller: `tests/Server.Tests/NodeEventExpandTests.fs`, fact ``git load reconcile posts NodeUnparsed for a modified file``.

[ ] `reconcileReceivedFiles` callers: `src/Server/GithubTransportActor.fs`; `tests/Server.Tests/LazyLoadReconciliationServerTests.fs` (two facts); `tests/Server.Tests/NodeEventExpandTests.fs` (`pullReceived` and direct calls).

[ ] `postParsedParents` caller: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, `postMarks`.

[ ] `postCurrentUnparsed` caller: `src/Server/LazyLoadReconciliationServer.fs`, function `postReconcileReport`, `postMarks`.

[ ] `DocumentOpImpact.noticesBeside`: no match under `LazyLoadReconciliation*` on staging (ticket forbids using it for this slice).

### Existing tests

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file changes keep files at any depth and drop directory paths``: `receivedFileChanges` keeps only non-directory-path file changes and drops directory-file paths.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file plan marks top and nested files and leaves the directory``: `planChangedPaths`-style received reconcile marks modified files Unparsed and leaves the containing Directory Current when the change is Modified only on files.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file add marks the containing directory Unparsed``: `planReceivedFiles` ops applied with `applyOps` set the containing Directory to Unparsed on Added file.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file add leaves an Unparsed directory Unparsed``: Added file under an already Unparsed Directory keeps that Directory Unparsed.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file removal marks the containing directory Unparsed``: Deleted file marks containing Directory Unparsed; sibling file stays Current.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file rename marks both directories Unparsed``: cross-directory file rename marks both source and destination Directory Unparsed.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file at the workspace top marks the workspace Unparsed``: Added file at workspace root marks the Workspace Special Unparsed.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file at the workspace top leaves an Unparsed workspace Unparsed``: Added top-level file leaves an already Unparsed Workspace Unparsed.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file modification leaves the containing directory Current``: Modified file marks the file Unparsed and leaves the Directory Current.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file add marks the nearest existing ancestor directory``: Added deep path marks nearest existing ancestor Directory Unparsed, not higher ancestors.

[ ] `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, ``received file add leaves an Unparsed nearest ancestor Unparsed``: Added deep path leaves an already Unparsed nearest ancestor Unparsed.

[ ] `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`, ``git pull marks received files at any depth and does not discover``: `reconcileReceivedFiles` marks listed files Unparsed, leaves docs Directory Current for dir-info Modified, and does not discover new members.

[ ] `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`, ``git pull add marks the containing directory Unparsed``: `reconcileReceivedFiles` on Added file marks docs Directory and new file Unparsed without expanding discovery.

[ ] `tests/Server.Tests/GithubTransportLoadPullTests.fs`, ``git Load pull marks a received file and its directory Unparsed``: `GithubTransportActor.runLoad` end-to-end marks directory and added file Unparsed after git pull adds `extra.txt`.

[ ] `tests/Server.Tests/NodeEventExpandTests.fs`, ``git load reconcile posts NodeParsed for an Unparsed parent`` ([131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md)): `reconcileReceivedFiles` posts `Notice.NodeParsed` for parent Directory and legacy Current.

[ ] `tests/Server.Tests/NodeEventExpandTests.fs`, ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse`` ([132 — Dir info parse drops its SetDocumentState](../issues/132-dir-info-parse-drops-setdocumentstate.md)): event order has `NodeParsing`, then Change, then `NodeParsed` for dir-info Modified.

[ ] `tests/Server.Tests/NodeEventExpandTests.fs`, ``git load reconcile posts NodeUnparsed for a modified file`` ([130 — Reconcile markUnparsed drops its SetDocumentState](../issues/130-reconcile-markunparsed-drops-setdocumentstate.md)): `planReceivedFiles` `unparsedIds` and mailbox `Notice.NodeUnparsed` for modified and renamed files.

### Copy-from

[ ] `LazyLoadReconciliationReport.parsedDirIds` population and `LazyLoadReconciliationServer.postNotices` for `Notice.NodeParsing` / `Notice.NodeParsed`: same report-field-then-server-post pattern in `src/Shared/dotnet/LazyLoadReconciliationReport.fs` and `src/Server/LazyLoadReconciliationServer.fs` ([132 — Dir info parse drops its SetDocumentState](../issues/132-dir-info-parse-drops-setdocumentstate.md) round).

[ ] `LazyLoadReconciliationReport.parsedParentIds` and `LazyLoadReconciliationServer.postParsedParents`: ids collected in the plan, `Notice.NodeParsed` posted from `postReconcileReport` ([131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md) round).

[ ] `LazyLoadReconciliationServer.postCurrentUnparsed`: posts `Notice.NodeUnparsed` only when legacy state is Current at reconcile time; reuse the Current gate for containing Directory ids, and skip PullingFile containers per ticket.

[ ] `LazyLoadReconciliation.containingDirectoryIds` (private): id choice for Added, Deleted, and Renamed file paths; reuse for the new report field instead of `unparsedStateOp`.

[ ] `tests/Server.Tests/NodeEventExpandTests.fs`, ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse``: `pullReceived`, `CoreMailboxApi.eventsSince`, sort by `EventId.value`, assert notice order around Change.

[ ] `tests/Server.Tests/NodeEventExpandTests.fs`, ``git load reconcile posts NodeParsed for an Unparsed parent``: `pullReceived` on Added file under Unparsed Directory; assert direct `Notice.NodeParsed` and legacy Current.

### implement

[ ] Staging `Report` has no field for containing Directory or Workspace ids; ticket requires that list beside `parsedParentIds`, `unparsedIds`, and `parsedDirIds`.

[ ] Staging `planReceivedFiles` appends `containingDirectoryMarkOps` ops to `report.ops` and does not store container ids on the report.

[ ] Staging `containingDirectoryMarkOps` maps each id through `unparsedStateOp`, emitting `Op.SetDocumentState(nodeId, priorEnum, Unparsed)` appended after the file plan so it overrides `markParentCurrent` ([133 — Received file container drops its SetDocumentState](../issues/133-received-file-container-drops-setdocumentstate.md) problem).

[ ] Staging `postReconcileReport` posts `Notice.NodeParsed` for `parsedParentIds` then `Notice.NodeUnparsed` for `unparsedIds` via `postCurrentUnparsed`; it posts no `Notice.NodeUnparsed` for containing Directory or Workspace ids.

[ ] Ticket: build `Notice.NodeUnparsed` from the new report ids only; no derivation from `SetDocumentState` ops and no `DocumentOpImpact.noticesBeside`.

[ ] Ticket: delete `unparsedStateOp` and remove its use from `containingDirectoryMarkOps` once tests pass.

[ ] Ticket: post container `Notice.NodeUnparsed` after [131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md) `Notice.NodeParsed` (`postParsedParents`) on the core mailbox in `reconcileReceivedFiles` / `postReconcileReport`.

[ ] Ticket: a node is Unparsed when it changes and Parsed when reconcile succeeds; shared file facts assert graph Unparsed via `applyOps` on plan ops today—after container ops are removed, those facts need `Notice.NodeUnparsed` on container ids (see `applyNodeParsed` / mailbox apply in tests) or an equivalent public apply path so legacy state matches server behavior.

[ ] Ticket: a PullingFile container leaves PullingFile until file arrival or server restart; do not post `Notice.NodeUnparsed` for a containing Directory or Workspace whose legacy state is PullingFile (mirror `postCurrentUnparsed` Current-only filter).

[ ] Ticket: path failures and kind conflicts stay in `report.failures`; `postReconcileReport` logs them and still posts and applies when ops post succeeds (no early return that skips reconcile on non-empty failures).

[ ] Do not change [130 — Reconcile markUnparsed drops its SetDocumentState](../issues/130-reconcile-markunparsed-drops-setdocumentstate.md) sites: `LazyLoadReconciliationApply.markUnparsed`, `LazyLoadReconciliationReport.renamedFileUnparsedOps`, `LazyLoadReconciliationServer.receivedFileIds` (including `planMarksUnparsed` reading `Op.SetDocumentState` for renames).

[ ] New test named on ticket: ``git Load reconcile posts NodeUnparsed for the containing Directory after NodeParsed`` (ticket wording uses “Load” capital L).

[ ] New test public seam: `LazyLoadReconciliationServer.reconcileReceivedFiles` with `WorkspaceReconcile` and `LazyLoadReconciliation.ChangedPath list`, same as [131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md) and [132 — Dir info parse drops its SetDocumentState](../issues/132-dir-info-parse-drops-setdocumentstate.md) server facts.

[ ] New test file: add the fact in `tests/Server.Tests/NodeEventExpandTests.fs` beside ``git load reconcile posts NodeParsed for an Unparsed parent``; use `pullReceived`, `eventsSince`, and order checks like ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse``; assert `Notice.NodeParsed` for an Unparsed parent when applicable, then `Notice.NodeUnparsed` for the containing Current Directory on Added file (e.g. `docs/extra.txt` pattern from ``git pull add marks the containing directory Unparsed``).

[ ] New test observes legacy Unparsed on the containing Directory after reconcile, with `loggedUnparsed` or `documentState`, and event order: `NodeParsed` before `NodeUnparsed` for the directory id when both apply.

[ ] Run ticket checks: `scripts/agent-test.sh` with no arguments; `--filter` for the eight `received file …` shared facts, ``git pull add marks the containing directory Unparsed``, and the new server fact when the default run skips them.
