# 03 — See a moved Node scope

Slice: Loaded and Unloaded stay in place for this slice. The Graph accepts a child id with no Header so the row can draw. `installWantAnswer` returns `Ok` when a child id has no Header. `replace` returns `Ok` for a new child id with no Header. `planPatchDOM` reads a Header with `Map.tryFind`. A missing Header is a value. A child id with no Header becomes a row. That row's Bullet is drawn in color #444444. Do not remove `ChildrenStatus`. Do not change Want filters. Do not delete Loaded or Unloaded.

## Signatures

[ ] [`ResidentProjection.installWantAnswer`](../../src/Shared/ResidentProjection.fs): `Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>`.
[ ] [`GraphMutate.replace`](../../src/Shared/GraphMutate.fs): `EventId option -> NodeId -> int -> ChildNode list -> ChildNode list -> Graph -> Result<Graph, string>`.
[ ] [`Graph.replace`](../../src/Shared/GraphOps.fs): same args via `GraphMutate.replace` with `Some EventId.zero`.
[ ] [`ViewModelDomPlan.planPatchDOM`](../../src/Shared/ViewModelDomPlan.fs): `VM -> VM -> Set<SiteId> -> RowMutation list`.
[ ] [`ViewModelChildrenIndicator.rowChildrenIndicator`](../../src/Shared/ViewModelChildrenIndicator.fs): `Graph -> Node -> RowChildrenIndicator`.
[ ] [`ViewModelRowState.rowOwnershipClass`](../../src/Shared/ViewModelRowState.fs): `VM -> SiteEntry -> string`.
[ ] [`RowView.Layout.buildRowElement`](../../src/Client/RowView.fs): `VM -> int -> SiteEntry -> HTMLElement * HTMLElement * HTMLElement * bool * RowChildrenIndicator`.
[ ] [`GraphBuild.fromNodes`](../../src/Shared/GraphBuild.fs): `NodeId -> Map<NodeId, Node> -> Map<NodeId, ChildNode list> -> Graph` (already keeps child ids in `parentByChild` without a Header in `nodes`).

## Match sites

[ ] [`ResidentProjection.installWantAnswer`](../../src/Shared/ResidentProjection.fs) `dangling` fold returns `Error "dangling edge"` when any edge target id is missing from `mergedNodes`.
[ ] [`SyncLogic.graphAfterWant`](../../src/Shared/SyncLogic.fs) calls `installWantAnswer` on Poll and post `childMap` / `nodes`.
[ ] [`GraphMutate.replace`](../../src/Shared/GraphMutate.fs) returns `Error "new child not found"` when any `newChildren` id is absent from `graph.nodes`.
[ ] [`GraphMutate.replace`](../../src/Shared/GraphMutate.fs) `introducedChildren` uses `graph.nodes.[child.id]` and `Node.childOwnership` for placement checks.
[ ] [`Op.applyAllowing`](../../src/Shared/History.fs) `Op.Replace` calls `GraphMutate.replace` at index `0`.
[ ] [`ResidentProjection.applyOpAllowing`](../../src/Shared/ResidentProjection.fs) `Op.Replace` runs only when the parent Header is present and `GraphChildren.isLoaded` is true.
[ ] [`ViewModelDomPlan.planPatchDOM`](../../src/Shared/ViewModelDomPlan.fs) upsert branch binds `newNode` with `newModel.graph.nodes.[entry.nodeId]`.
[ ] [`ViewModelDomPlan.rowClassName`](../../src/Shared/ViewModelDomPlan.fs) and [`selectionClassPatches`](../../src/Shared/ViewModelDomPlan.fs) already use `Map.tryFind entry.nodeId` for some paths and skip when `None`.
[ ] [`ViewModelSiteMap.walkReconciled`](../../src/Shared/ViewModelSiteMap.fs) and [`expandEntry`](../../src/Shared/ViewModelSiteMap.fs) already insert `SiteEntry` rows from `GraphChildren.get` child ids without a Header lookup.
[ ] [`RowView.Layout.buildRowElement`](../../src/Client/RowView.fs) binds `node` with `model.graph.nodes.[nodeId]` before bullet and text layout.
[ ] [`ViewModelChildrenIndicator.rowChildrenIndicator`](../../src/Shared/ViewModelChildrenIndicator.fs) assumes a resident `Node` and uses `GraphChildren.tryGet` on `node.id`.
[ ] [`src/Server/wwwroot/style.css`](../../src/Server/wwwroot/style.css) sets bullet color via `.amb-row-owned .amb-fold-toggle { color: #000; }` and `.amb-row-ref .amb-fold-toggle { color: #44f; }` with `.amb-bullet-*::before { background/border: currentColor }`.

## Existing tests

[ ] [`WantTests.fs`](../../tests/Shared.Tests/WantTests.fs) ``installWantAnswer refuses a dangling edge`` expects `Error "dangling edge"`.
[ ] [`SyncLogicTests.fs`](../../tests/Shared.Tests/SyncLogicTests.fs) ``applySyncResponse refuses dangling Want edges`` expects `Error "dangling edge"`.
[ ] [`WantTests.fs`](../../tests/Shared.Tests/WantTests.fs) ``installWantAnswer accepts a Child already Resident`` covers a present Header on the edge target.
[ ] [`ViewModelRowStateTests.fs`](../../tests/Shared.Tests/ViewModelRowStateTests.fs) ``rowChildrenIndicator is HollowCircle when children are Unloaded`` covers Unloaded, not Absent Header.
[ ] [`ViewModelTests.fs`](../../tests/Shared.Tests/ViewModelTests.fs) ``planPatchDOM expand inserts correct child count`` covers `CreateRow` when the site map gains visible children with Headers present.
[ ] No Shared test today asserts `Error "new child not found"` on [`Graph.replace`](../../src/Shared/GraphOps.fs).

## Copy-from

[ ] [`GraphBuild.buildParentMaps`](../../src/Shared/GraphBuild.fs) and [`GraphBuild.fromNodes`](../../src/Shared/GraphBuild.fs) already accept child ids in `childMap` when the id is not in `nodes`.
[ ] [`ResidentProjection.wantAnswer`](../../src/Shared/ResidentProjection.fs) `completeEdge` omits a parent edge when any child Header is missing (opposite install rule; Want filters stay unchanged this slice).
[ ] [`ViewModelDomPlan.selectionClassPatches`](../../src/Shared/ViewModelDomPlan.fs) treats a missing Header with `Map.tryFind entry.nodeId newModel.graph.nodes` as `None` and emits no patch.

## write-ticket

[ ] Current install refusal string is `"dangling edge"` in [`installWantAnswer`](../../src/Shared/ResidentProjection.fs).
[ ] Current replace refusal for an Absent child id is `"new child not found"` in [`GraphMutate.replace`](../../src/Shared/GraphMutate.fs).
[ ] Current bullet color path is [`rowOwnershipClass`](../../src/Shared/ViewModelRowState.fs) choosing `amb-row-owned` or `amb-row-ref`, then CSS `currentColor` on `.amb-fold-toggle` in [`style.css`](../../src/Server/wwwroot/style.css).
[ ] Target bullet color for an Absent Header row is `#444444` per [[arch.md](../arch.md)] story path **See a moved Node**.

### Source files

[ ] [`src/Shared/ResidentProjection.fs`](../../src/Shared/ResidentProjection.fs) — drop or narrow the dangling-edge gate so edges with Absent child ids return `Ok`.
[ ] [`src/Shared/GraphMutate.fs`](../../src/Shared/GraphMutate.fs) — allow `newChildren` ids absent from `nodes`; skip or guard Header-only placement scans for those ids.
[ ] [`src/Shared/ViewModelDomPlan.fs`](../../src/Shared/ViewModelDomPlan.fs) — replace `nodes.[entry.nodeId]` with `Map.tryFind`; treat missing Header as placeholder row patches or `CreateRow` / `RecreateRow`.
[ ] [`src/Shared/ViewModelChildrenIndicator.fs`](../../src/Shared/ViewModelChildrenIndicator.fs) — add an Absent-Header indicator path (new `RowChildrenIndicator` case or id-based entry point).
[ ] [`src/Client/RowView.fs`](../../src/Client/RowView.fs) — draw placeholder rows when Header is absent; apply `#444444` bullet styling.
[ ] [`src/Server/wwwroot/style.css`](../../src/Server/wwwroot/style.css) — class rules for the Absent placeholder bullet color `#444444`.

### Tests

[ ] Change [`WantTests.fs`](../../tests/Shared.Tests/WantTests.fs) ``installWantAnswer refuses a dangling edge`` to expect `Ok` and a Loaded parent edge with an Absent child id still in `childMap`.
[ ] Change [`SyncLogicTests.fs`](../../tests/Shared.Tests/SyncLogicTests.fs) ``applySyncResponse refuses dangling Want edges`` to expect `Ok` after Want install with an Absent child id on the edge.
[ ] Add [`WantTests.fs`](../../tests/Shared.Tests/WantTests.fs) ``Graph.replace accepts new child id with no Header`` (or adjacent Graph mutate test file) expecting `Ok` and the id under the parent in `childMap`.
[ ] Add [`ViewModelTests.fs`](../../tests/Shared.Tests/ViewModelTests.fs) or [`ViewModelRowStateTests.fs`](../../tests/Shared.Tests/ViewModelRowStateTests.fs) ``planPatchDOM CreateRow for visible Absent child id`` expecting `CreateRow` without throwing when `graph.nodes` lacks that id.

## Over size

[ ] Add [`ViewModelRowStateTests.fs`](../../tests/Shared.Tests/ViewModelRowStateTests.fs) coverage for Absent-Header `rowChildrenIndicator` and placeholder bullet class or color hook.
[ ] Re-run or adjust any test that builds a graph only through `installWantAnswer` with intentionally incomplete `nodes` lists once dangling acceptance lands.
[ ] Split Client row DOM proof into a follow-up ticket if Fable UI tests are out of scope for the Shared-first slice.

## Size

[ ] Six source files: [`ResidentProjection.fs`](../../src/Shared/ResidentProjection.fs), [`GraphMutate.fs`](../../src/Shared/GraphMutate.fs), [`ViewModelDomPlan.fs`](../../src/Shared/ViewModelDomPlan.fs), [`ViewModelChildrenIndicator.fs`](../../src/Shared/ViewModelChildrenIndicator.fs), [`RowView.fs`](../../src/Client/RowView.fs), [`style.css`](../../src/Server/wwwroot/style.css).
[ ] [`ViewModelSiteMap.fs`](../../src/Shared/ViewModelSiteMap.fs) likely read-only for this slice because child rows already come from `GraphChildren.get` ids.
[ ] Four test add-or-change items in **Tests** above; extras listed under **Over size**.
[ ] Medium slice: two Result gates, one DOM planner hot path, one Client row paint path, no Want or `ChildrenStatus` deletion.
