# 05 — Fill a placeholder scope

Slice: **Fill a placeholder** — `installWantAnswer` adds the Node when its Header arrives. The next plan draws that row as a normal row. Assumes [03 — See a moved Node](arch.md) already draws a missing Header as a placeholder row. This slice does not remove `ChildrenStatus`. This slice does not change Bullet color `#444444` (that stays in 03). Spec: [5. Fill a placeholder](spec.md).

## Signatures

- [ ] `ResidentProjection.installWantAnswer: Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>` in [[src/Shared/ResidentProjection.fs]] — merges `nodes` into `graph.nodes` with `Map.add`, merges `edges` into `graph.childMap`, and today refuses when any target in the **incoming** `edges` map lacks a Header in `mergedNodes`.
- [ ] `SyncLogic.graphAfterWant: SyncResponse -> Graph -> Result<Graph, string>` in [[src/Shared/SyncLogic.fs]] — calls `installWantAnswer response.childMap response.nodes graph` when either `response.nodes` or `response.childMap` is non-empty.
- [ ] `ViewModelDomPlan.planPatchDOM: VM -> VM -> Set<SiteId> -> RowMutation list` in [[src/Shared/ViewModelDomPlan.fs]] — for cached visible rows, reads `newModel.graph.nodes.[entry.nodeId]` and `Map.tryFind entry.nodeId oldModel.graph.nodes`; `RecreateRow` when `oldKind <> Some newKind`, children-indicator change, edit-mode change, or remote workspace arrival.
- [ ] `ViewModelDomPlan.RowMutation` and `ViewModelDomPlan.RowPatch` in [[src/Shared/ViewModelDomPlan.fs]] — `CreateRow`, `RecreateRow`, `PatchRow` with `SetClassName`, `SetText`, `SetNodeName`, `SetFileIndicator`, `SetTextClasses`, `SetFoldArrow`.
- [ ] `ChildrenStatus` with cases `Loaded` and `Unloaded` in [[src/Shared/Model.fs]] — unchanged in this slice; callers still derive Loaded from a present `childMap` key.

## Match sites

- [ ] `ResidentProjection.installWantAnswer` body — `mergedNodes` fold, `dangling` check on incoming `edges`, `Graph.fromNodes graph.root mergedNodes mergedChildMap` in [[src/Shared/ResidentProjection.fs]].
- [ ] `SyncLogic.graphAfterWant` — sole production caller of `installWantAnswer` in [[src/Shared/SyncLogic.fs]].
- [ ] `SyncLogic.applySyncResponse` and related Poll/post apply paths — invoke `graphAfterWant` after Events fold in [[src/Shared/SyncLogic.fs]].
- [ ] `ViewModelDomPlan.planPatchDOM` upsert branch — `oldNode` / `newNode` comparison and `RecreateRow` vs `PatchRow` in [[src/Shared/ViewModelDomPlan.fs]].
- [ ] `ViewModelOps.planPatchDOM` — re-export in [[src/Shared/ViewModelOps.fs]].
- [ ] `View.render` (patch path) — `ViewModel.planPatchDOM oldModel newModel cachedInstIds` in [[src/Client/View.fs]].

## Existing tests

- [ ] ``installWantAnswer refuses a dangling edge`` in [[tests/Shared.Tests/WantTests.fs]] — expects `Error "dangling edge"` when an incoming edge names a child id with no Header in `nodes` (03 removes this refusal; 05 adds fill behavior beside it).
- [ ] ``installWantAnswer accepts a Child already Resident`` in [[tests/Shared.Tests/WantTests.fs]] — `installWantAnswer` with edges and empty `nodes` when the child Header is already in `graph.nodes`.
- [ ] ``stale installWantAnswer after split drops suffix and restores hello text`` in [[tests/Shared.Tests/SplitOriginTraceTests.fs]] — merges stale Headers from a Want `nodes` list over an existing graph.
- [ ] ``applySyncResponse refuses dangling Want edges`` in [[tests/Shared.Tests/SyncLogicTests.fs]] — Poll/post path surfaces `installWantAnswer` dangling refusal end-to-end.
- [ ] ``applySyncResponse Load Fetch answer installs through installWantAnswer`` in [[tests/Shared.Tests/SyncLogicTests.fs]] — Load Fetch answer installs edges and Nodes through `graphAfterWant`.
- [ ] ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] — `RecreateRow` when `rowChildrenIndicator` changes after the graph gains a child list.
- [ ] ``planPatchDOM text change produces SetText patch and no CreateRow`` in [[tests/Shared.Tests/ViewModelTests.fs]] — `PatchRow` when the same instance keeps a Resident Header and text changes.

## Copy-from

- [ ] ``installWantAnswer accepts a Child already Resident`` in [[tests/Shared.Tests/WantTests.fs]] — pattern for idempotent merge when the child id is already on the graph before `installWantAnswer`.
- [ ] ``stale installWantAnswer after split drops suffix and restores hello text`` in [[tests/Shared.Tests/SplitOriginTraceTests.fs]] — pattern for merging a fresh Header from the `nodes` list into an existing graph.
- [ ] ``applySyncResponse Load Fetch answer installs through installWantAnswer`` in [[tests/Shared.Tests/SyncLogicTests.fs]] — pattern for `graphAfterWant` success on a Want answer package.
- [ ] ``planPatchDOM recreates row when Unloaded leaf becomes Loaded with children`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] — pattern for `RecreateRow` when row chrome must change after graph facts arrive.

## write-ticket

- [ ] One job: after 03, when a Poll or post Want answer delivers a Header for a child id that already sits in a Loaded parent list, the graph holds that Node and the next DOM plan treats the row as a normal Resident row (not the `#444444` placeholder).

### Source files

- [ ] [[src/Shared/ResidentProjection.fs]] — `installWantAnswer`: ensure a Want answer whose `nodes` list carries new Headers merges into a graph that already lists those ids under Loaded parents (including nodes-only answers with empty `childMap`), without re-refusing ids that 03 already accepted as Absent on the graph.
- [ ] [[src/Shared/ViewModelDomPlan.fs]] — `planPatchDOM`: when `oldModel` had no Resident Header for `entry.nodeId` (03 placeholder / `Map.tryFind` miss) and `newModel` has a Header, emit `RecreateRow` or patches so the cached instance becomes a normal row (text, class, fold indicator, file indicator) without touching `#444444` styling owned by 03.

### Tests

- [ ] Add ``installWantAnswer adds Header for Absent child already on Loaded parent list`` in [[tests/Shared.Tests/WantTests.fs]] — parent Loaded with child id in `childMap`, no Header in `graph.nodes`; `installWantAnswer Map.empty [ node ] graph` returns `Ok` and `Map.containsKey childId`.
- [ ] Add ``planPatchDOM recreates row when placeholder gains Header`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] or [[tests/Shared.Tests/ViewModelTests.fs]] — same `SiteId` and visible set; `oldModel.graph` lacks the child Header (03 placeholder seam); `newModel.graph` adds the Header; assert `RecreateRow` for that instance (or `PatchRow` with `SetText` plus class change if 03 uses a synthetic placeholder node).
- [ ] Add ``applySyncResponse nodes-only answer fills Absent child Header`` in [[tests/Shared.Tests/SyncLogicTests.fs]] — state graph matches 03 (child id on Loaded parent, no Header); response `{ nodes = [ child ]; childMap = empty; events = [] }`; assert `Ok`, Header present, and no `"dangling edge"`.
- [ ] Change ``applySyncResponse Load Fetch answer installs through installWantAnswer`` in [[tests/Shared.Tests/SyncLogicTests.fs]] only if the fill path shares the same helper — extend with an Absent-then-fill second response on the same child id, or leave unchanged when the three new tests cover the seam.

## Size

- [ ] Source: two modules, roughly 15–40 lines net in `installWantAnswer` (merge/idempotence) and 10–25 lines in `planPatchDOM` (placeholder-to-Resident branch).
- [ ] Tests: three new facts plus zero or one extended fact; stay within four named tests in Outputs.

## Over size

- [ ] Add ``installWantAnswer idempotent when Header already Resident`` in [[tests/Shared.Tests/WantTests.fs]] — second `installWantAnswer` with the same `nodes` entry leaves the graph unchanged (Unchanged/idempotent), if the implementer needs a fourth assertion separate from the Absent-fill case.
