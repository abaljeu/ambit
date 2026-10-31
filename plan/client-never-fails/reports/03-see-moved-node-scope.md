# 03 — See a moved Node — implement scope

### Signatures

[ ] ``ResidentProjection.installWantAnswer: Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>`` in `src/Shared/ResidentProjection.fs`.
[ ] ``GraphMutate.replace: EventId option -> NodeId -> int -> ChildNode list -> ChildNode list -> Graph -> Result<Graph, string>`` in `src/Shared/GraphMutate.fs`.
[ ] ``Graph.replace: NodeId -> int -> ChildNode list -> ChildNode list -> Graph -> Result<Graph, string>`` forwards to `GraphMutate.replace (Some EventId.zero) …` in `src/Shared/GraphOps.fs`.
[ ] ``ViewModelDomPlan.planPatchDOM: VM -> VM -> Set<SiteId> -> RowMutation list`` in `src/Shared/ViewModelDomPlan.fs`.
[ ] ``ViewModelChildrenIndicator.rowChildrenIndicator: Graph -> Node -> RowChildrenIndicator`` in `src/Shared/ViewModelChildrenIndicator.fs`.
[ ] ``RowView.Layout.buildRowElement: VM -> int -> SiteEntry -> HTMLElement * HTMLElement * HTMLElement * bool * RowChildrenIndicator`` in `src/Client/RowView.fs`.
[ ] ``GraphBuild.fromNodes: NodeId -> Map<NodeId, Node> -> Map<NodeId, ChildNode list> -> Graph`` in `src/Shared/GraphBuild.fs`.
[ ] ``GraphChildren.isLoaded: Graph -> NodeId -> bool`` gates `GraphMutate.replace` via `src/Shared/GraphMutate.fs`.

### Match sites

[ ] `ResidentProjection.installWantAnswer` folds `nodes` into `mergedNodes`, then `Map.exists` on `edges` with `List.exists (fun child -> not (Map.containsKey child.id mergedNodes))` — `src/Shared/ResidentProjection.fs`, `installWantAnswer`.
[ ] `installWantAnswer` returns `Error "dangling edge"` when `dangling` is true — `src/Shared/ResidentProjection.fs`, `installWantAnswer`.
[ ] `installWantAnswer` returns `Ok (Graph.fromNodes graph.root mergedNodes mergedChildMap)` on the non-dangling path — `src/Shared/ResidentProjection.fs`, `installWantAnswer`.
[ ] `SyncLogic.graphAfterWant` calls `ResidentProjection.installWantAnswer response.childMap response.nodes graph` — `src/Shared/SyncLogic.fs`, `graphAfterWant`.
[ ] `SyncLogic.applySyncResponse` calls `graphAfterWant` when `response.nodes` or `response.childMap` is non-empty — `src/Shared/SyncLogic.fs`, `applySyncResponse`.
[ ] `GraphMutate.replace` matches `Some _ when not (GraphChildren.isLoaded graph parentId)` with `Error "parent children not loaded"` — `src/Shared/GraphMutate.fs`, `replace`.
[ ] `GraphMutate.replace` matches when `newChildren |> List.exists (fun child -> not (graph.nodes.ContainsKey child.id))` with `Error "new child not found"` — `src/Shared/GraphMutate.fs`, `replace`.
[ ] `GraphMutate.replace` uses `graph.nodes.[child.id]` in `introducedChildren` placement scan after the missing-child gate — `src/Shared/GraphMutate.fs`, `replace`.
[ ] `History.applyAllowed` `Op.Replace` arm calls `GraphMutate.replace changedVersion parentId 0 oldChildren newChildren state.graph` — `src/Shared/History.fs`, `applyAllowed`.
[ ] `GraphOps` `Graph.replace` static member forwards to `GraphMutate.replace` — `src/Shared/GraphOps.fs`.
[ ] `ViewModelDomPlan.planPatchDOM` upsert branch binds `let newNode = newModel.graph.nodes.[entry.nodeId]` when `Set.contains instId cachedInstIds` — `src/Shared/ViewModelDomPlan.fs`, `planPatchDOM`.
[ ] `planPatchDOM` calls `rowChildrenIndicator newModel.graph newNode` on that cached upsert path — `src/Shared/ViewModelDomPlan.fs`, `planPatchDOM`.
[ ] `planPatchDOM` yields `CreateRow instId` when the visible `instId` is not in `cachedInstIds` — `src/Shared/ViewModelDomPlan.fs`, `planPatchDOM`.
[ ] `ViewModelDomPlan.selectionClassPatches` uses `Map.tryFind entry.nodeId newModel.graph.nodes` and returns `[]` on `None` — `src/Shared/ViewModelDomPlan.fs`, `selectionClassPatches`.
[ ] `ViewModelChildrenIndicator.rowChildrenIndicator` returns `SlashedHollowCircle` when `Node.documentStateEnum node = Unparsed` — `src/Shared/ViewModelChildrenIndicator.fs`, `rowChildrenIndicator`.
[ ] `rowChildrenIndicator` matches `GraphChildren.tryGet graph node.id` with `None -> HollowCircle`, `Some [] -> SolidCircle`, `Some _ -> FoldChevron` — `src/Shared/ViewModelChildrenIndicator.fs`, `rowChildrenIndicator`.
[ ] `RowView.Layout.buildRowElement` binds `let node = model.graph.nodes.[nodeId]` — `src/Client/RowView.fs`, `buildRowElement`.
[ ] `buildRowElement` adds class `amb-fold-toggle` on fold chevron and bullet spans — `src/Client/RowView.fs`, `buildRowElement`.
[ ] `src/Server/wwwroot/style.css` sets `.amb-row-owned .amb-fold-toggle { color: #000; }` — `style.css`.
[ ] `src/Server/wwwroot/style.css` sets `.amb-row-ref .amb-fold-toggle { color: #44f; }` — `style.css`.
[ ] ``installWantAnswer refuses a dangling edge`` calls `ResidentProjection.installWantAnswer edges [] graph` and asserts `Error "dangling edge"` — `tests/Shared.Tests/WantTests.fs`.
[ ] ``applySyncResponse refuses dangling Want edges`` calls `SyncLogic.applySyncResponse` with `childMap` listing a missing child id and asserts `Error "dangling edge"` — `tests/Shared.Tests/SyncLogicTests.fs`.

### Existing tests

[ ] ``installWantAnswer refuses a dangling edge`` in `tests/Shared.Tests/WantTests.fs` builds an edge to a fresh `NodeId` not in `nodes` and expects `Error` message `"dangling edge"`.
[ ] ``applySyncResponse refuses dangling Want edges`` in `tests/Shared.Tests/SyncLogicTests.fs` posts a Want answer whose `childMap` edge targets a missing id and expects `Error "dangling edge"`.
[ ] ``planPatchDOM expand inserts correct child count`` in `tests/Shared.Tests/ViewModelTests.fs` expands a row with resident Headers and expects two `CreateRow` mutations.
[ ] ``Graph replace parent missing error ends with last 8 hex of parent id`` in `tests/Shared.Tests/ModelTests.fs` calls `Graph.replace` with an unknown parent id and expects a `parent not found` `Error`.
[ ] ``Graph.replace rejects Unloaded parent`` in `tests/Shared.Tests/ModelTests.fs` calls `Graph.replace` when the parent has no loaded child list and expects `Error "parent children not loaded"`.
[ ] No test in `tests/Shared.Tests/ModelTests.fs` asserts `Graph.replace` returns `Error "new child not found"` for a new child id absent from `graph.nodes`.

### Copy-from

[ ] `GraphBuild.fromNodes` rebuilds parent maps from `childMap` without requiring each child id to appear in `nodes` — `src/Shared/GraphBuild.fs`, `fromNodes`.
[ ] `ViewModelDomPlan.selectionClassPatches` uses `Map.tryFind entry.nodeId newModel.graph.nodes` and skips patches when the Header is missing — `src/Shared/ViewModelDomPlan.fs`, `selectionClassPatches`.
[ ] `ViewModelSiteMap.walkReconciled` `resolveChildInstIds` inserts a `SiteEntry` with `nodeId = child.id` when no old entry matches, without a Header lookup — `src/Shared/ViewModelSiteMap.fs`, `resolveChildInstIds` / `walkReconciled`.

### implement

[ ] Ticket Working fact 1 (`installWantAnswer` returns `Error "dangling edge"` for a missing edge target) matches `Map.containsKey child.id mergedNodes` refusal in `src/Shared/ResidentProjection.fs`, `installWantAnswer`.
[ ] Ticket Working fact 2 (`GraphMutate.replace` returns `Error "new child not found"` and keeps the parent `isLoaded` gate) matches `GraphChildren.isLoaded` and `graph.nodes.ContainsKey` checks in `src/Shared/GraphMutate.fs`, `replace`.
[ ] Ticket Working fact 3 (`planPatchDOM` binds `newModel.graph.nodes.[entry.nodeId]` on the cached upsert path) matches line 170 in `src/Shared/ViewModelDomPlan.fs`, `planPatchDOM`.
[ ] Ticket Working fact 4 (`buildRowElement` reads `model.graph.nodes.[nodeId]`) matches `src/Client/RowView.fs`, `buildRowElement`.
[ ] Ticket Working fact 5 (Bullet color via `.amb-fold-toggle` `#000` and `#44f`) matches `src/Server/wwwroot/style.css` rules for `.amb-row-owned` and `.amb-row-ref`.
[ ] Ticket Working fact 6 (`walkReconciled` makes a row from a child id with no Header lookup) matches the `None` branch that `Map.add`s `nodeId = child.id` in `src/Shared/ViewModelSiteMap.fs`, `resolveChildInstIds`.
[ ] Ticket Working fact 7 (`GraphBuild.fromNodes` keeps a child id when `nodes` has no Header) matches `buildParentMaps childT` in `src/Shared/GraphBuild.fs`, `fromNodes`.
[ ] Ticket Working fact 8 (`selectionClassPatches` uses `Map.tryFind`) matches `Map.tryFind entry.nodeId newModel.graph.nodes` in `src/Shared/ViewModelDomPlan.fs`, `selectionClassPatches`.
[ ] Ticket Output: "`installWantAnswer` returns `Ok` when a child id has no Header" — today `installWantAnswer` returns `Error "dangling edge"` when the edge lists an id absent from `mergedNodes` (`src/Shared/ResidentProjection.fs`, `installWantAnswer`).
[ ] Ticket Output: "`replace` returns `Ok` for a new child id with no Header" — today `GraphMutate.replace` returns `Error "new child not found"` when any `newChildren` id is missing from `graph.nodes` (`src/Shared/GraphMutate.fs`, `replace`).
[ ] Ticket Output: "`planPatchDOM` uses `Map.tryFind`. A missing Header is a placeholder row" — today the cached upsert path indexes `newModel.graph.nodes.[entry.nodeId]` (`src/Shared/ViewModelDomPlan.fs`, `planPatchDOM`).
[ ] Ticket Output: "`ViewModelChildrenIndicator` — an Absent Header has a Bullet path that can take the #444444 color" — today `rowChildrenIndicator` takes a resident `Node` and has no Absent-only branch (`src/Shared/ViewModelChildrenIndicator.fs`, `rowChildrenIndicator`).
[ ] Ticket Output: "`buildRowElement` draws that row" — today `buildRowElement` requires `model.graph.nodes.[nodeId]` (`src/Client/RowView.fs`, `buildRowElement`).
[ ] Ticket Output: "the placeholder Bullet uses color #444444" — today owned bullets use `#000` and ref bullets use `#44f` on `.amb-fold-toggle` (`src/Server/wwwroot/style.css`).
[ ] New test ``Graph.replace accepts new child id with no Header`` should call public `Graph.replace` with a loaded parent and a `newChildren` id absent from `graph.nodes`, assert `Ok`, and assert the child id appears on the parent list while the Header may still be absent (per [See a moved Node](plan/client-never-fails/spec.md) story 3).
[ ] New test ``planPatchDOM CreateRow for visible Absent child id`` should build `oldModel` / `newModel` where `newModel.siteMap` lists a visible `instId` for a child id with no Header, call `planPatchDOM`, and assert a `CreateRow` for that `instId` (row plan seam in `src/Shared/ViewModelDomPlan.fs`).
[ ] Changed test ``installWantAnswer refuses a dangling edge`` should assert `Ok` (not `Error "dangling edge"`) when the Want edge lists a child id with no Header in `nodes`, and assert the edge remains on the graph via `GraphChildren.get` or `childMap` (`ResidentProjection.installWantAnswer` public seam).
[ ] Changed test ``applySyncResponse refuses dangling Want edges`` should assert `SyncLogic.applySyncResponse` returns `Ok` for the same absent-child Want package and the graph keeps the parent edge with the absent child id (`SyncLogic.applySyncResponse` through `graphAfterWant`).
