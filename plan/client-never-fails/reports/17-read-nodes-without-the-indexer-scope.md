# 17 — Read Nodes without the indexer — scope

This slice replaces every remaining `graph.nodes.[id]`-style Map indexer read on `Graph.nodes` in `src/Client` and `src/Shared` with `Map.tryFind`. A missing Header is a value. A guard refuses a new indexer use on that map. Story order is in [Client never fails on server data architecture](plan/client-never-fails/arch.md).

### Indexer sweep

[ ] Two reads inside `collectSubtree` in `src/Shared/Paste.fs` belong to 04 — Use a placeholder and are omitted below.
[ ] `onCut` in `src/Client/Controller.fs` has no Node-map indexer read.
[ ] `src/Client/Commands.fs` — 1
[ ] `src/Client/UpdateHelpers.fs` — 2
[ ] `src/Client/UpdateMove.fs` — 1
[ ] `src/Client/UpdatePaste.fs` — 1
[ ] `src/Client/UpdateRename.fs` — 1
[ ] `src/Shared/DocumentPathMove.fs` — 1
[ ] `src/Shared/ExportText.fs` — 1
[ ] `src/Shared/ExprDialog.fs` — 1
[ ] `src/Shared/ExprRun.fs` — 2
[ ] `src/Shared/GraphQuery.fs` — 1
[ ] `src/Shared/RefExprMatch.fs` — 2
[ ] `src/Shared/ResidentProjection.fs` — 1
[ ] `src/Shared/Serialization.fs` — 1
[ ] `src/Shared/Snapshot.fs` — 1
[ ] `src/Shared/ViewModelJoinOps.fs` — 3
[ ] `src/Shared/ViewModelSearch.fs` — 1
[ ] `src/Shared/WorkspaceUploadStructure.fs` — 2
[ ] `src/Shared/documents/AmbDocument.fs` — 1
[ ] `src/Shared/documents/DocumentColdParse.fs` — 1
[ ] `src/Shared/dotnet/LazyLoadReconciliation.fs` — 2
[ ] `src/Shared/dotnet/LazyLoadReconciliationApply.fs` — 3
[ ] `src/Shared/dotnet/LazyLoadReconciliationPath.fs` — 1
[ ] `src/Shared/dotnet/LazyLoadReconciliationReport.fs` — 1
[ ] Sweep total: 32 locations across 23 files.

### Signatures

[ ] `Graph.nodes: Map<NodeId, Node>` on record `Graph` in `src/Shared/Model.fs`.
[ ] `Map.tryFind: 'Key -> Map<'Key, 'T> -> 'T option` for a Node-map read.
[ ] `CommandRequest.nodeText: Graph -> NodeId -> string option` in `src/Shared/CommandRequest.fs`.
[ ] `Paste.serializeSubtree: Graph -> SiteMap -> NodeId list -> string` in `src/Shared/Paste.fs`.
[ ] `Paste.collectSubtree: Graph -> SiteMap -> ChildNode list -> ClipboardContent` in `src/Shared/Paste.fs`.

### Match sites

[ ] `openLinkTarget` reads `model.graph.nodes.[focusId].text` — `src/Client/Commands.fs`.
[ ] `splitNode` reads `model.graph.nodes.[focusedId].text` — `src/Client/UpdateHelpers.fs`.
[ ] `moveEditImpl` reads `committed.graph.nodes.[targetEntry.nodeId].text` — `src/Client/UpdateHelpers.fs`.
[ ] `restoreInlineMode` reads `model.graph.nodes.[focusedNodeId model.graph newSel].text` — `src/Client/UpdateMove.fs`.
[ ] `editingModeAfterPaste` reads `m.graph.nodes.[focusId].text` — `src/Client/UpdatePaste.fs`.
[ ] `initialRenameValue` reads `model.graph.nodes.[nodeId].name` — `src/Client/UpdateRename.fs`.
[ ] `pathChildIds` matches `graph.nodes.[childId].kind` — `src/Shared/dotnet/LazyLoadReconciliation.fs`.
[ ] `planMoveNode` binds `withParents.nodes.[nodeId]` — `src/Shared/dotnet/LazyLoadReconciliation.fs`.
[ ] `decidesUnparsedMark` matches `Node.documentStateEnum graph.nodes.[nodeId]` — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`.
[ ] `skipParseAddedDirInfo` compares `Node.documentStateEnum graph.nodes.[nodeId] = Current` — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`.
[ ] `parseDirInfoIfPresent` reads `Node.documentStateEnum graph.nodes.[nodeId]` — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`.
[ ] `ownedChildNamed` binds `graph.nodes.[child.id]` — `src/Shared/dotnet/LazyLoadReconciliationPath.fs`.
[ ] `isExistingCurrent` compares `Node.documentStateEnum graph.nodes.[nodeId] = Current` — `src/Shared/dotnet/LazyLoadReconciliationReport.fs`.
[ ] `contentOwner` binds `graph.nodes.[id]` — `src/Shared/RefExprMatch.fs`.
[ ] `walkOwnerChain` binds `graph.nodes.[id]` — `src/Shared/RefExprMatch.fs`.
[ ] `visibleClosureWantAnswer` maps `graph.nodes.[id]` — `src/Shared/ResidentProjection.fs`.
[ ] `graphFromDecodedNodes` reads `g.nodes.[Graph.rootId]` — `src/Shared/Serialization.fs`.
[ ] `write` binds `graph.nodes.[nodeId]` — `src/Shared/Snapshot.fs`.
[ ] `tryVisibleNeighbor` sets `node = model.graph.nodes.[nodeId]` — `src/Shared/ViewModelJoinOps.fs`.
[ ] `joinWithNextPlan` binds `model.graph.nodes.[currentId]` — `src/Shared/ViewModelJoinOps.fs`.
[ ] `previousJoinPlan` matches `model.graph.nodes.[focusedNodeId model.graph sel].kind` — `src/Shared/ViewModelJoinOps.fs`.
[ ] `searchPickSetRoot` binds `model.graph.nodes.[hit.nodeId]` — `src/Shared/ViewModelSearch.fs`.
[ ] `planServerFilePresentOps` matches `Node.documentStateEnum graph.nodes.[fileId]` — `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `planAlignFileStampOps` binds `graph.nodes.[fileId]` — `src/Shared/WorkspaceUploadStructure.fs`.
[ ] `serializeLines` binds `graph.nodes.[nodeId]` — `src/Shared/documents/AmbDocument.fs`.
[ ] `planOpsFromGraphs` calls `createOrUpdateOps before after.nodes.[nodeId]` — `src/Shared/documents/DocumentColdParse.fs`.
[ ] `serializeOwnedChildren` binds `graph.nodes.[nodeId]` — `src/Shared/ExportText.fs`.
[ ] `renameOps` reads `graph.nodes.[focusId].name` — `src/Shared/ExprRun.fs`.
[ ] `planExpr` builds `ExprAnswer.Node graph.nodes.[focusId]` — `src/Shared/ExprRun.fs`.
[ ] `tryHits` builds `ExprAnswer.Node graph.nodes.[zoomRoot]` — `src/Shared/ExprDialog.fs`.
[ ] `trySyncRootByLabel` binds `graph.nodes.[child.id]` — `src/Shared/GraphQuery.fs`.
[ ] `planRenameNode` binds `graph.nodes.[nodeId]` — `src/Shared/DocumentPathMove.fs`.

### Existing tests

[ ] ``Copy does nothing when the selected row Header is absent`` in `tests/Shared.Tests/PasteTests.fs` asserts `serializeSubtree` returns `""` when the Header is absent.
[ ] ``Cut does nothing when the selected row Header is absent`` in `tests/Shared.Tests/PasteTests.fs` belongs to 04 — Use a placeholder.

### Copy-from

[ ] `CommandRequest.nodeText` uses `Map.tryFind` and returns `string option` — `src/Shared/CommandRequest.fs`.
[ ] `Paste.serializeSubtree` uses `Map.tryFind` and writes no text when the Header is absent — `src/Shared/Paste.fs`.

### Guard for new indexer uses

[ ] Extend `scan_added` in `scripts/standards-scan.py` so an added line in `src/Client` or `src/Shared` that matches `.nodes.[` is a hit.

### write-ticket

[ ] Source files this slice would change: the 23 files in the indexer sweep, plus `scripts/standards-scan.py`.
[ ] Tests this slice would add or change: the scan hit is the new check. The existing suite stays the regression check. ``Copy does nothing when the selected row Header is absent`` stays as it is.
