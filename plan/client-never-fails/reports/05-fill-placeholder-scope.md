# 05 — Fill a placeholder — scope

Story constraint from [Client never fails on server data architecture](plan/client-never-fails/arch.md) slice **Fill a placeholder**: `installWantAnswer` adds the Node when its Header arrives, and the next plan draws that row as a normal row. Spec story: the Browser draws an Absent child as a normal row when its Node arrives.

## Signatures

[ ] `ChildNode` in `src/Shared/Model.fs`: `{ ref: Ownership; id: NodeId }` with `ChildNode.owner`, `ChildNode.reference`, `ChildNode.owners`.

[ ] `RowChildrenIndicator` in `src/Shared/ViewModelChildrenIndicator.fs`: `FoldChevron | SolidCircle | HollowCircle | SlashedHollowCircle`.

[ ] `ViewModelDomPlan.RowPatch` in `src/Shared/ViewModelDomPlan.fs`: `SetClassName | SetText | SetTextClasses | SetFoldArrow | SetNodeName | SetFileIndicator`.

[ ] `ViewModelDomPlan.RowMutation` in `src/Shared/ViewModelDomPlan.fs`: `RemoveRow of SiteId | CreateRow of SiteId | RecreateRow of SiteId | PatchRow of SiteId * RowPatch list`.

[ ] `SyncResponse` in `src/Shared/ApiResponses.fs`: `{ events: Ev list; nodes: Node list; childMap: Map<NodeId, ChildNode list> }`.

[ ] `VM` in `src/Shared/ViewModel.fs`: client state record with `graph`, `siteMap`, `zoomRoot`, and related fields.

[ ] `ResidentProjection.installWantAnswer` in `src/Shared/ResidentProjection.fs`:

```fsharp
let installWantAnswer
    (edges: Map<NodeId, ChildNode list>)
    (nodes: Node list)
    (graph: Graph)
    : Result<Graph, string>
```

[ ] `SyncLogic.graphAfterWant` in `src/Shared/SyncLogic.fs`:

```fsharp
let private graphAfterWant
    (response: SyncResponse)
    (graph: Graph)
    : Result<Graph, string>
```

[ ] `ViewModelDomPlan.planPatchDOM` in `src/Shared/ViewModelDomPlan.fs`:

```fsharp
let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) : RowMutation list
```

[ ] `ViewModelDomPlan.ResidentRowCompare` (private) in `src/Shared/ViewModelDomPlan.fs`: `{ oldModel; newModel; entry; instId; newNode; oldNode; oldEntry }`.

## Match sites

### `installWantAnswer`

[ ] Definition and merge: `ResidentProjection.installWantAnswer` in `src/Shared/ResidentProjection.fs` folds `nodes` into `graph.nodes` and `edges` into `graph.childMap`, then `Ok (Graph.fromNodes graph.root mergedNodes mergedChildMap)` with no `Error` arm.

[ ] Production caller: `SyncLogic.graphAfterWant` in `src/Shared/SyncLogic.fs` calls `ResidentProjection.installWantAnswer response.childMap response.nodes graph` when the response carries any `nodes` or `childMap`.

[ ] `graphAfterWant` is the only production caller of `installWantAnswer` in `src/` (confirmed by repository search under `src/`).

[ ] `graphAfterWant` is invoked from `SyncLogic.applySyncResponse` and `SyncLogic.applyWantPreservingPending` in `src/Shared/SyncLogic.fs`.

[ ] `SyncLogic.applyWantPreservingPending` is called from `src/Client/SyncAnswer.fs` for post-acknowledgement Want install.

[ ] Test callers: `ResidentProjection.installWantAnswer` is invoked directly in `tests/Shared.Tests/WantTests.fs`, `tests/Shared.Tests/SplitOriginTraceTests.fs`, and indirectly in `tests/Shared.Tests/SyncLogicTests.fs` through `SyncLogic.applySyncResponse`.

### `planPatchDOM` path for `RecreateRow` when row kind / chrome changes

[ ] `ViewModelDomPlan.planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` delegates visible rows to `visibleRowMutation`.

[ ] `visibleRowMutation` in `src/Shared/ViewModelDomPlan.fs` matches `Map.tryFind entry.nodeId newModel.graph.nodes`: `None` → `placeholderCachedMutation`; `Some newNode` → `residentCachedMutation`.

[ ] `placeholderCachedMutation` in `src/Shared/ViewModelDomPlan.fs` matches `Map.tryFind entry.nodeId oldModel.graph.nodes`: `Some _` → `RecreateRow instId` (Header lost on old model); `None` → `PatchRow` with placeholder class patches.

[ ] `residentCachedMutation` in `src/Shared/ViewModelDomPlan.fs` calls `residentNeedsRecreate`; when true, emits `RecreateRow row.instId`.

[ ] `residentNeedsRecreate` in `src/Shared/ViewModelDomPlan.fs` compares edit mode, `rowChildrenIndicatorForId` vs `rowChildrenIndicator`, `oldNode.kind` vs `newNode.kind`, and `workspaceRemoteArrived`.

[ ] `rowChildrenIndicatorForId` in `src/Shared/ViewModelChildrenIndicator.fs` uses `Map.tryFind nodeId graph.nodes`: missing Header → `placeholderBulletIndicator` (`SolidCircle`); present Header → `rowChildrenIndicator graph node`.

[ ] `ViewModel.planPatchDOM` in `src/Shared/ViewModelOps.fs` is a re-export of `ViewModelDomPlan.planPatchDOM`.

[ ] `View.render` in `src/Client/View.fs` calls `ViewModel.planPatchDOM oldModel newModel cachedInstIds`.

[ ] `RowMutation` `RecreateRow` is matched in `src/Client/View.fs` and `src/Client/RowView.fs` for DOM upsert.

## Existing tests

[ ] ``installWantAnswer accepts a Child already Resident`` in `tests/Shared.Tests/WantTests.fs` builds `zoomWithChildren [ "a"; "b" ]`, installs an edge for an already-resident child with empty `nodes`, expects `Ok`, loaded parent, and `installed.nodes.ContainsKey residentChild`.

[ ] ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` unloads a leaf, then adds a resident child list and child Header on the new graph; expects a `RecreateRow` for the leaf instance id.

[ ] ``applySyncResponse Load Fetch answer installs through installWantAnswer`` in `tests/Shared.Tests/SyncLogicTests.fs` uses unloaded want-parent and workspace parents, applies a Want answer with `nodes` and `childMap`, expects loaded children through `SyncLogic.applySyncResponse` → `graphAfterWant`.

[ ] ``stale installWantAnswer after split drops suffix and restores hello text`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` calls `installWantAnswer` with edges and a `nodes` list carrying stale Headers; expects child order and restored hello text.

[ ] ``installWantAnswer adds Header for Absent child already on parent list`` is not present in `tests/Shared.Tests/WantTests.fs` (named only in [05 — Fill a placeholder ticket](plan/client-never-fails/issues/05-fill-placeholder/05-fill-placeholder-ticket.md)).

[ ] ``planPatchDOM recreates row when placeholder gains Header`` is not present in `tests/Shared.Tests/ViewModelTests.fs` (named only in the ticket).

[ ] ``applySyncResponse nodes-only answer fills Absent child Header`` is not present in `tests/Shared.Tests/SyncLogicTests.fs` (named only in the ticket).

## Copy-from

[ ] `ResidentProjection.installWantAnswer` in `src/Shared/ResidentProjection.fs` is the existing merge of `nodes` into `graph.nodes` and `edges` into `graph.childMap` via `List.fold` / `Map.fold` then `Graph.fromNodes`.

[ ] `ResidentProjection.mergePackageNodes` in `src/Shared/ResidentProjection.fs` is a related merge helper for bootstrap packages (nodes plus `childMap` with Loaded-key rules); Want install uses the simpler `installWantAnswer` fold instead.

[ ] `ViewModelDomPlan.placeholderCachedMutation` in `src/Shared/ViewModelDomPlan.fs` is the [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md) `Map.tryFind` on the old model versus Header presence: `Some _` on old → `RecreateRow`.

[ ] ``planPatchDOM CreateRow for visible Absent child id`` in `tests/Shared.Tests/ViewModelTests.fs` is the fixture copy-from for Absent child id on `childMap` without Header: `Graph.fromNodes` with `[ ChildNode.owner absentId ]`, `emptyModelAt`, `buildCacheSet`, then `planPatchDOM`.

[ ] ``stale installWantAnswer after split drops suffix and restores hello text`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` is the copy-from for installing Headers through `installWantAnswer` with a `nodes` list.

## implement

### Ticket Working inputs vs code

[ ] Match: ``ResidentProjection.installWantAnswer`` in `src/Shared/ResidentProjection.fs` merges `nodes` and `edges` (`childMap` parameter) into the graph.

[ ] Match: ``SyncLogic.graphAfterWant`` in `src/Shared/SyncLogic.fs` calls `installWantAnswer` with `response.childMap` and `response.nodes`.

[ ] Match: ``ViewModelDomPlan.planPatchDOM`` in `src/Shared/ViewModelDomPlan.fs` emits `RecreateRow` when row chrome or kind changes through `residentNeedsRecreate` and through `placeholderCachedMutation` when the old model had a Header.

[ ] Match: `graphAfterWant` is the production caller of `installWantAnswer` (only `src/Shared/SyncLogic.fs` references it under `src/`).

[ ] Match: `planPatchDOM` uses `Map.tryFind` on the old model graph nodes in `placeholderCachedMutation` after [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md).

[ ] Match: `View.render` in `src/Client/View.fs` calls `planPatchDOM`; the ticket leaves that file unchanged.

[ ] Match: ``installWantAnswer accepts a Child already Resident`` exists and passes (see Existing tests).

[ ] Match: ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` exists and checks `RecreateRow`.

[ ] Match: ``applySyncResponse Load Fetch answer installs through installWantAnswer`` exists and checks install through `applySyncResponse`.

[ ] Match: ``stale installWantAnswer after split drops suffix and restores hello text`` exists and merges Headers from `nodes`.

[ ] Match (intended behavior already in code): `installWantAnswer` stores a Header delivered for a child id already on a parent list — a nodes-only call `installWantAnswer Map.empty [ header ] graph` returns `Ok` and `Map.containsKey` for that id (verified with F# Interactive against the built assembly).

[ ] Match (intended behavior already in code): `planPatchDOM` emits `RecreateRow` when a placeholder row gains a Header — fixture mirroring ``planPatchDOM CreateRow for visible Absent child id`` plus `Graph.addDetachedNode` on the new graph yields `RecreateRow` for the absent instance id (verified with F# Interactive).

[ ] Differ: the three Output tests named in [05 — Fill a placeholder ticket](plan/client-never-fails/issues/05-fill-placeholder/05-fill-placeholder-ticket.md) are not in the test projects yet.

[ ] `ResidentProjection.fs` line count is 547; a small edit for this slice stays under the 800-line rule.

[ ] `ViewModelDomPlan.fs` line count is 340; a small edit for this slice stays under the 800-line rule.

### Function bodies for implementer

[ ] `installWantAnswer` full body in `src/Shared/ResidentProjection.fs` (no branch refuses a Header for an id already on a parent list):

```207:221:src/Shared/ResidentProjection.fs
    let installWantAnswer
        (edges: Map<NodeId, ChildNode list>)
        (nodes: Node list)
        (graph: Graph)
        : Result<Graph, string> =
        let mergedNodes =
            nodes
            |> List.fold
                (fun acc node -> Map.add node.id node acc)
                graph.nodes
        let mergedChildMap =
            edges
            |> Map.fold (fun acc key kids -> Map.add key kids acc)
                graph.childMap
        Ok(Graph.fromNodes graph.root mergedNodes mergedChildMap)
```

[ ] `planPatchDOM` row dispatch and `RecreateRow` branches in `src/Shared/ViewModelDomPlan.fs`:

```62:86:src/Shared/ViewModelDomPlan.fs
    let private placeholderCachedMutation
        (oldModel: VM) (newModel: VM) (entry: SiteEntry) (instId: SiteId) : RowMutation =
        match Map.tryFind entry.nodeId oldModel.graph.nodes with
        | Some _ -> RecreateRow instId
        | None ->
            ...
            PatchRow (instId, patches)

    ...

    let private residentNeedsRecreate (row: ResidentRowCompare) =
        ...
        let oldKind = row.oldNode |> Option.map (fun n -> n.kind)
        wasEditing <> nowEditing
        || oldIndicator <> newIndicator
        || oldKind <> Some row.newNode.kind
        || workspaceRemoteArrived row.oldModel row.newModel row.newNode

    let private residentCachedMutation (row: ResidentRowCompare) : RowMutation =
        if residentNeedsRecreate row then
            RecreateRow row.instId
        else
            PatchRow (row.instId, residentRowPatches row)

    let private visibleRowMutation
        (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) (instId: SiteId)
        : RowMutation =
        let entry = newModel.siteMap.entries.[instId]
        if not (Set.contains instId cachedInstIds) then
            CreateRow instId
        else
            match Map.tryFind entry.nodeId newModel.graph.nodes with
            | None -> placeholderCachedMutation oldModel newModel entry instId
            | Some newNode ->
                residentCachedMutation { ... oldNode = Map.tryFind entry.nodeId oldModel.graph.nodes ... }
```

[ ] Fill-a-placeholder path: when the new model has a Header and the old model does not, `visibleRowMutation` takes the `Some newNode` arm; `oldNode` is `None`, so `oldKind` is `None` and `oldKind <> Some row.newNode.kind` is true, so `residentNeedsRecreate` emits `RecreateRow` even when both old and new bullet indicators are `SolidCircle`.

### How each named new test should observe the change

[ ] ``installWantAnswer adds Header for Absent child already on parent list`` — public seam: `ResidentProjection.installWantAnswer`. Fixture shape: graph like ``installWantAnswer refuses a dangling edge`` (parent list contains `ChildNode.owner absentId`, no Header in `graph.nodes`); call with `edges = Map.empty` and `nodes = [ Node.Create(absentId, ...) ]`. Observable: `Ok`, `Map.containsKey absentId` on result, text or owner on stored Header. Copy setup from ``installWantAnswer refuses a dangling edge`` and ``stale installWantAnswer after split drops suffix and restores hello text`` (`Node.Create`, `owned`, `requireOk`).

[ ] ``planPatchDOM recreates row when placeholder gains Header`` — public seam: `ViewModelDomPlan.planPatchDOM` (or `ViewModel.planPatchDOM`). Fixture shape: copy ``planPatchDOM CreateRow for visible Absent child id`` in `tests/Shared.Tests/ViewModelTests.fs` for old model and cache; new model same site map rebuild with `Graph.addDetachedNode` for the absent Header (`Node.Create(absentId, text = ..., owner = parentId)`). Observable: mutation list contains `RecreateRow absentInstId`. Row kind derivation: missing Header → `placeholderCachedMutation` / `amb-row-absent`; present Header → `rowClassName` without `amb-row-absent`.

[ ] ``applySyncResponse nodes-only answer fills Absent child Header`` — public seam: `SyncLogic.applySyncResponse` → `graphAfterWant` → `installWantAnswer`. Fixture shape: graph with absent child id on parent `childMap`, no Header; `ClientSyncState` with that graph; `SyncResponse` with `{ events = []; nodes = [ headerNode ]; childMap = Map.empty }`. Observable: `Ok` state, Header stored for absent id. Copy from ``applySyncResponse Load Fetch answer installs through installWantAnswer`` for state record shape and from dangling-edge graph setup in `tests/Shared.Tests/SyncLogicTests.fs` for absent-on-list without Header.

[ ] Header construction pattern in tests: `Node.Create(nodeId, text = "...", owner = parentId)` (optional `name`, `kind` for special nodes).

[ ] Edge / child list construction: `[ ChildNode.owner childId ]` or `owned [ id1; id2 ]` from `ChildNode.owners` (`tests/Shared.Tests/WantTests.fs`, `GraphChildMapHelpers`).

[ ] Want answer / sync response construction: `{ events = []; nodes = nodeList; childMap = map }` as `SyncResponse` (`src/Shared/ApiResponses.fs`).

[ ] Row kind / bullet for Absent vs resident: `ViewModelChildrenIndicator.rowChildrenIndicatorForId` returns `placeholderBulletIndicator` when `Map.tryFind` misses; `rowChildrenIndicator graph node` when Header is present (`src/Shared/ViewModelChildrenIndicator.fs`).
