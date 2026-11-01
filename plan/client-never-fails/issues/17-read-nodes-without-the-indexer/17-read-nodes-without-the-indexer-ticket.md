# 17 — Read Nodes without the indexer
**Status**: 
One job: Replace every remaining `graph.nodes.[id]`-style Map indexer read of the Node map in `src/Client` and `src/Shared` with `Map.tryFind`, so a missing Header is a value and never an exception, and add a check that refuses a new indexer use on that Node map. This sweep is 32 locations in 23 files, which exceeds the usual per-ticket file count in [Size limit](plan/context/size-limit.md), and Alan approved that sweep on 2026-11-01. The story Use a placeholder in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[ ] Alan counted 41 locations across 20 files. On this branch, after the two `collectSubtree` reads are set aside, the count is 32 locations in 23 files.
[ ] `Graph.nodes` is `Map<NodeId, Node>` on `Graph` in `src/Shared/Model.fs`.
[ ] `Map.tryFind` returns `Node option`. A missing Header is `None`.
[ ] `CommandRequest.nodeText` in `src/Shared/CommandRequest.fs` already returns `Map.tryFind id graph.nodes |> Option.map (fun node -> node.text)`.
[ ] `Paste.serializeSubtree` in `src/Shared/Paste.fs` already uses `Map.tryFind` and skips a missing Header.
[ ] `src/Client/Commands.fs` — 1, in `openLinkTarget`.
[ ] `src/Client/UpdateHelpers.fs` — 2, in `splitNode` and `moveEditImpl`.
[ ] `src/Client/UpdateMove.fs` — 1, in `restoreInlineMode`.
[ ] `src/Client/UpdatePaste.fs` — 1, in `editingModeAfterPaste`. `cutSelection` is [04 — Use a placeholder](plan/client-never-fails/issues/04-use-placeholder/04-use-placeholder-ticket.md).
[ ] `src/Client/UpdateRename.fs` — 1, in `initialRenameValue`.
[ ] `src/Shared/DocumentPathMove.fs` — 1, in `planRenameNode`.
[ ] `src/Shared/ExportText.fs` — 1, in `serializeOwnedChildren`.
[ ] `src/Shared/ExprDialog.fs` — 1, in `tryHits`.
[ ] `src/Shared/ExprRun.fs` — 2, in `renameOps` and `planExpr`.
[ ] `src/Shared/GraphQuery.fs` — 1, in `trySyncRootByLabel`.
[ ] `src/Shared/RefExprMatch.fs` — 2, in `contentOwner` and `walkOwnerChain`.
[ ] `src/Shared/ResidentProjection.fs` — 1, in `visibleClosureWantAnswer`.
[ ] `src/Shared/Serialization.fs` — 1, in `graphFromDecodedNodes`.
[ ] `src/Shared/Snapshot.fs` — 1, in `write`.
[ ] `src/Shared/ViewModelJoinOps.fs` — 3, in `tryVisibleNeighbor`, `joinWithNextPlan`, and `previousJoinPlan`.
[ ] `src/Shared/ViewModelSearch.fs` — 1, in `searchPickSetRoot`.
[ ] `src/Shared/WorkspaceUploadStructure.fs` — 2, in `planServerFilePresentOps` and `planAlignFileStampOps`.
[ ] `src/Shared/documents/AmbDocument.fs` — 1, in `serializeLines`.
[ ] `src/Shared/documents/DocumentColdParse.fs` — 1, in `planOpsFromGraphs`.
[ ] `src/Shared/dotnet/LazyLoadReconciliation.fs` — 2, in `pathChildIds` and `planMoveNode`.
[ ] `src/Shared/dotnet/LazyLoadReconciliationApply.fs` — 3, in `decidesUnparsedMark`, `skipParseAddedDirInfo`, and `parseDirInfoIfPresent`.
[ ] `src/Shared/dotnet/LazyLoadReconciliationPath.fs` — 1, in `ownedChildNamed`.
[ ] `src/Shared/dotnet/LazyLoadReconciliationReport.fs` — 1, in `isExistingCurrent`.
[ ] `scripts/standards-scan.py` scans added lines. It has no rule for `.nodes.[`.
[ ] ``Copy does nothing when the selected row Header is absent`` in `tests/Shared.Tests/PasteTests.fs` expects `serializeSubtree` to return `""` when the Header is absent.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]
[ ] Scope notes in [17 — Read Nodes without the indexer — scope](plan/client-never-fails/reports/17-read-nodes-without-the-indexer-scope.md)

## Do not load

[ ] Closed: `src/Server`. This ticket leaves Server indexer reads as they are.
[ ] Closed: `collectSubtree` and `onCut`. 04 — Use a placeholder makes Cut a no-op when a Header is absent.
[ ] Closed: `GLOSSARY.md` and the spec. This ticket leaves them unchanged.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change each Node-map indexer read in the 23 files Inputs names to `Map.tryFind`. A missing Header is a value. The function that reads it decides that value.
3. [ ] Change `scripts/standards-scan.py` so an added line under `src/Client` or `src/Shared` that matches `.nodes.[` is a hit.

## Outputs

[ ] The 23 source files named in Inputs
[ ] `scripts/standards-scan.py` — added lines under `src/Client` and `src/Shared` refuse `.nodes.[`
[ ] The suite that already covers these functions is the regression check.

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/17-read-nodes-without-the-indexer-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Copy does nothing when the selected row Header is absent``
[ ] Run `python3 scripts/standards-scan.py` on a diff that adds `.nodes.[` under `src/Client` or `src/Shared`, and see the hit.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/17-read-nodes-without-the-indexer-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
