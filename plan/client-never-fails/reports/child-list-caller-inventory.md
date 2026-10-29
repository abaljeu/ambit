# Child list caller inventory

## Production callers

[ ] [[src/Shared/ResidentProjection.fs]], `applyOpAllowing`, applies `Op.Replace` only when `GraphChildren.isLoaded` on the parent; header ops apply when the node exists regardless of child-list state.

[ ] [[src/Shared/ResidentProjection.fs]], `wantAnswer`, builds Want-answer edges via `GraphChildren.tryGet`; a parent with no `childMap` key is omitted from the answer (Unloaded).

[ ] [[src/Shared/ResidentProjection.fs]], `visibleClosureWantAnswer`, collects edges only for parents in the visible closure where `GraphChildren.tryGet` succeeds (Loaded parents).

[ ] [[src/Shared/ResidentProjection.fs]], `installWantAnswer`, merges answer edges into `childMap`; absent keys stay Unloaded until an edge is added; `[]` is Loaded.

[ ] [[src/Shared/ResidentProjection.fs]], `wantAnswerForTargets`, when `LoadTarget.includeWorkspace` is true, includes that target id in the list passed to `wantAnswer`; otherwise only workspace-package targets from selection drive the answer.

[ ] [[src/Server/Api.fs]], `nodesForRequest`, maps requested graph ids to resident `Node` headers only; does not read or send child-list Loaded/Unloaded state.

[ ] [[src/Shared/ApiResponses.fs]], `UniversalResponse`, carries `nodes`, `events`, and `latestId` only; no `childMap` and no per-node child-list state on the type.

[ ] [[src/Shared/ApiResponses.fs]], `LoadTarget`, `includeWorkspace` is documented as controlling whether the owning workspace package is needed for Load; semantics tie to Unloaded workspace headers vs Loaded answers.

[ ] [[src/Shared/ApiResponseSerialization.fs]], `encodeUniversalResponse` / `decodeUniversalResponseDecoder`, round-trip `nodes` via `Serialization.encodeNode` / `decodeNode` (nodes omit wire `childrenStatus`); no `childMap` field.

[ ] [[src/Shared/ApiResponseSerialization.fs]], `encodeLoadTarget` / `decodeLoadTargetDecoder`, round-trip `targetId` and `includeWorkspace` on Load requests.

[ ] [[src/Shared/GraphMutate.fs]], `replace`, returns `Error "parent children not loaded"` when `GraphChildren.isLoaded` is false on the parent.

[ ] [[src/Shared/History.fs]], `validateOwnershipSemantics` (`isProvenMissingOwner`), treats a claimed owner as not proven missing when `GraphChildren.isLoaded` is false on that owner (Unloaded owner elsewhere under selective load).

[ ] [[src/Shared/WorkspaceUploadStructure.fs]], `planStubOps`, returns `Ok []` without inventing stubs when the workspace node exists and `GraphChildren.isLoaded` is false.

[ ] [[src/Shared/ViewModelChildrenIndicator.fs]], `rowChildrenIndicator`, uses `GraphChildren.tryGet`: `None` → hollow (Unloaded), `Some []` → solid leaf (Loaded empty), `Some (_ :: _)` → chevron (Loaded with children); Unparsed bypasses child-list lookup.

[ ] [[src/Shared/ViewModelRowState.fs]], `residencyText`, formats tooltip residency from `GraphChildren.status` as `"Loaded"` or `"Unloaded"` plus document state.

[ ] [[src/Shared/Serialization.fs]], `encodeChildrenStatus` / `decodeChildrenStatus`, wire strings `"loaded"` / `"unloaded"` for legacy node wires; `decodeNodeWire` defaults missing field to Loaded; rejects Unloaded with non-empty `children`; `childMapFromWires` drops Unloaded nodes from `childMap`; modern `encodeGraph` uses top-level `childMap` without per-node `childrenStatus`.

[ ] [[src/Shared/Want.fs]], `compose`, filters the Included walk to ids where `GraphChildren.isLoaded` is false (Unloaded want targets).

[ ] [[src/Client/UpdateHelpers.fs]], `loadTargetIntent`, sets `includeWorkspace = true` when the target node exists and `Graph.isLoaded` is false.

[ ] [[src/Shared/Model.fs]], `ChildrenStatus` (`Unloaded` | `Loaded`) and `GraphChildren` (`tryGet`, `get`, `isLoaded`, `status`) derive state from `childMap` key presence.

[ ] [[src/Shared/GraphBuild.fs]], `isLoaded` and `childrenStatus` forward to `GraphChildren`; bootstrap paths use private `withLoadedEmpty` so canonical nodes get Loaded empty lists.

[ ] [[src/Shared/GraphOps.fs]], `Graph.isLoaded` and `Graph.childrenStatus` static members forward to `GraphBuild`.

## Comments only

[ ] [[src/Shared/ApiResponses.fs]], doc comments on `ChangeSuccessResponse.childMap`, `SyncResponse.childMap`, and `LoadResponse`-related shapes describe absent keys as Unloaded and `[]` as Loaded leaf.

[ ] [[src/Shared/Snapshot.fs]], comment that outline snapshot nodes are Loaded and absent keys stay Unloaded.

[ ] [[src/Shared/SyncLogic.fs]], comment that sync apply runs under Loaded rules (delegates to `ResidentProjection`).

[ ] [[src/Shared/ExprWalk.fs]], doc comments that Unloaded parents yield empty answer streams (implementation uses `GraphChildren.tryGet`, not `isLoaded`).

[ ] [[src/Shared/dotnet/WorkspaceCloudUpload.fs]], success string mentions Unloaded workspace stub skip.

[ ] [[src/Shared/ViewModelChildrenIndicator.fs]], module doc maps indicator shapes to Unloaded / Loaded / Unparsed (code uses `tryGet` only).

## Tests that assert child-list Loaded or Unloaded

[ ] ``Graph absent childMap key is Unloaded empty is Loaded``, [[tests/Shared.Tests/ModelTests.fs]], asserts `Graph.childrenStatus` is Unloaded without a key and Loaded after `addDetachedNode`, with empty `Graph.children` in both cases.

[ ] ``Graph.fromNodes keeps Unloaded when parent key is absent``, [[tests/Shared.Tests/ModelTests.fs]], asserts Unloaded status and empty children for a node without a `childMap` entry.

[ ] ``Graph childMap Unloaded is absent after round-trip``, [[tests/Shared.Tests/SerializationTests.fs]], asserts decoded graph is Unloaded and `childMap` lacks the id after graph JSON round-trip.

[ ] ``legacy Graph decode without childrenStatus defaults to Loaded``, [[tests/Shared.Tests/SerializationTests.fs]], asserts legacy embedded `children` wire decodes to Loaded empty children.

[ ] ``legacy package decode rejects Unloaded with non-empty children``, [[tests/Shared.Tests/SerializationTests.fs]], asserts decode error mentions Unloaded when `childrenStatus` is unloaded with children present.

[ ] ``StateResponse decode accepts Alan sample node shapes``, [[tests/Shared.Tests/SerializationTests.fs]], asserts Loaded status on a legacy-wired special node after state JSON decode.

[ ] ``graphRoundTrip rebuilds Unloaded as Loaded``, [[tests/Shared.Tests/GraphProjectionTests.fs]], asserts persistence round-trip turns an absent-key node into Loaded and `Graph.isLoaded` for all nodes in the result graph.

[ ] ``compose then installWantAnswer loads wanted parents and children``, [[tests/Shared.Tests/WantTests.fs]], asserts `GraphChildren.isLoaded` on installed parent true and grandchild false before second compose.

[ ] ``installWantAnswer keeps absent key Unloaded and [] Loaded``, [[tests/Shared.Tests/WantTests.fs]], asserts only the answered id becomes Loaded with `[]` and sibling stays Unloaded across repeat install.

[ ] ``installWantAnswer accepts a Child already Resident``, [[tests/Shared.Tests/WantTests.fs]], asserts parent becomes Loaded after installing an edge to an already-resident child.

[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors``, [[tests/Shared.Tests/WantTests.fs]], asserts reserved ids and workspace path are Loaded and a buried id stays Unloaded via `isLoaded` and `Graph.childrenStatus`.

[ ] ``visibleClosureGraph bad Zoom stays inside reserved set``, [[tests/Shared.Tests/WantTests.fs]], asserts workspace header stays Unloaded when zoom id is missing from the graph.

[ ] ``visibleClosureWantAnswer installs through installWantAnswer``, [[tests/Shared.Tests/WantTests.fs]], asserts directory id is Loaded after installing visible-closure answer.

[ ] ``rootBootstrapGraph includes nested workspace header Unloaded``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts nested workspace header is Unloaded in ROOT bootstrap slice.

[ ] ``rootBootstrapGraph keeps ROOT owned content Loaded``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts ROOT-owned note retains Loaded child-list state in bootstrap slice.

[ ] ``rootBootstrapGraph includes Ref header without children``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts external Ref target header is Unloaded with empty children.

[ ] ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts workspace, directory, and file ids are Loaded in scoped bootstrap.

[ ] ``bootstrapGraph with no zoom is ROOT only``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts workspace header stays Unloaded when zoom is omitted.

[ ] ``bootstrapGraph with zoom inside ROOT does not duplicate residency``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts in-ROOT note stays Loaded without widening to workspace package.

[ ] ``bootstrapGraph with missing zoom falls back to ROOT only``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts workspace header Unloaded when zoom id is absent from graph.

[ ] ``bootstrapGraph zoom Workspace keeps nested Workspace header Unloaded``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts nested named workspace header Unloaded when zoom is outer workspace.

[ ] ``bootstrapGraph zoom Workspace includes Ref header without children``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts Ref target header Unloaded without child list in scoped bootstrap.

[ ] ``sessionBootstrapTarget uses focus when zoom stays in ROOT``, [[tests/Shared.Tests/BootstrapScopeTests.fs]], asserts workspace becomes Loaded in bootstrap when focus selects in-workspace file while zoom stays at workspaces.

[ ] ``truncationGraph drops Load-only nested Workspace children``, [[tests/Shared.Tests/BootCachePollTests.fs]], asserts truncated graph keeps workspace header Unloaded and drops nested directory node.

[ ] ``applyServerTail skips structural Replace on Unloaded parent``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts workspace stays Unloaded after Replace tail is skipped on unloaded parent.

[ ] ``applyServerTail applies header facts on Unloaded resident Node``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts SetText applies on resident header while child list stays Unloaded.

[ ] ``applySyncResponse installs complete child list as Loaded and preserves owner``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts workspace id becomes Loaded after sync installs childMap answer.

[ ] ``applySyncResponse empty Loaded child list marks Loaded without History clear``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts empty childMap entry marks workspace Loaded without clearing history.

[ ] ``applySyncResponse installs Want answer after Event tail``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts parent becomes Loaded with one child after events plus Want answer.

[ ] ``applySyncResponse Want-answer empty list marks Loaded leaf``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts parent Loaded with empty children from Want-only answer.

[ ] ``applySyncResponse Load Fetch answer installs through installWantAnswer``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts both want-parent and workspace ids Loaded after Load-style answer.

[ ] ``applySyncResponse re-applies pending after Want rewind``, [[tests/Shared.Tests/SyncLogicTests.fs]], asserts parent Loaded after Want answer with pending local edit preserved.

[ ] ``nested paste and NewSpecialNode Undo and Redo preserve reachable structure``, [[tests/Shared.Tests/HistoryTests.fs]], reachable-structure equality includes `Graph.childrenStatus` per node in undo/redo cycle.

[ ] ``split-shaped Change Undo and Redo preserve sibling semantics``, [[tests/Shared.Tests/HistoryTests.fs]], reachable-structure equality includes child-list status in undo/redo shape map.

[ ] ``delivered inverse of large paste measures phases without per-created-Node rebuild``, [[tests/Shared.Tests/LargeChangeApplyTests.fs]], reachable-structure comparisons embed `Graph.childrenStatus` for inverse projection checks.

[ ] ``answer-only Load at the same event id preserves ClientHistory``, [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]], asserts workspace becomes Loaded when Load answer installs empty child list at same event id.

[ ] ``planStubOps creates Directory named .agents with file child``, [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]], asserts `.agents` directory id is Loaded after stub planning on Loaded workspace.

[ ] ``Load Unloaded stub plan must not name-conflict on resident server``, [[tests/Shared.Tests/WorkspaceUploadStructureTests.fs]], asserts client bootstrap sees workspace Unloaded before stub planning against server inventory.

[ ] ``rowChildrenIndicator is HollowCircle when children are Unloaded``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts hollow indicator when `childMap` key absent (Unloaded setup).

[ ] ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts solid indicator for Loaded empty leaf.

[ ] ``rowChildrenIndicator is FoldChevron when Loaded with children``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts chevron when Loaded parent has children.

[ ] ``bulletTip minimal node lists guid residency and update time only``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts residency line reads `Residency: Loaded, Current`.

[ ] ``bulletTip disambiguates hollow by Unloaded versus Unparsed``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts Unloaded vs Loaded/Unparsed residency strings differ on tooltip.

[ ] ``bulletTip reads Loaded Current for a parsed leaf``, [[tests/Shared.Tests/ViewModelRowStateTests.fs]], asserts tooltip contains Loaded residency for detached Loaded node.

[ ] ``OUTER is Owned only, strictly below, Unloaded miss, not a tree prune``, [[tests/Shared.Tests/ExprCombinatorTests.fs]], asserts unloaded blue node remains Unloaded in graph during OUTER eval.

[ ] ``owned and ref answers on Unloaded node yield empty sequence``, [[tests/Shared.Tests/ExprEvalTests.fs]], asserts owned/ref answers empty and `Graph.childrenStatus` Unloaded on unloaded parent.

[ ] ``Unloaded input: child, owned, and ref miss and do not Load``, [[tests/Shared.Tests/ExprPipelineTests.fs]], asserts expr misses on Unloaded node and status stays Unloaded.

[ ] ``getState zoom outside ROOT adds owning Workspace``, [[tests/Server.Tests/ApiGetStateTests.fs]], asserts workspace Loaded in GET state response when zoom widens outside ROOT.

[ ] ``getState without zoom keeps nested Workspace Unloaded``, [[tests/Server.Tests/ApiGetStateTests.fs]], asserts nested workspace header Unloaded in default GET state scope.

[ ] ``GET state default returns ROOT closure excluding nested workspace contents``, [[tests/Server.Tests/StateEndpointTests.fs]], asserts workspace Unloaded in live client GET `/ambit/state` default scope.

## Tests outside that list

No other test files in the repository assert `ChildrenStatus`, `Graph.childrenStatus`, `Graph.isLoaded`, `GraphChildren.isLoaded`, `GraphChildren.status`, or residency strings comparing Loaded/Unloaded child-list state.

## Helpers

[ ] [[tests/Shared.Tests/GraphChildMapHelpers.fs]], `setChildren`, rebuilds graph with parent id Loaded to given kids (including `[]`).

[ ] [[tests/Shared.Tests/GraphChildMapHelpers.fs]], `unload`, rebuilds graph with parent id Unloaded (removes `childMap` key).

[ ] [[tests/Shared.Tests/GraphChildMapHelpers.fs]], `addDetachedMany`, attaches nodes as Loaded empty via `Graph.addDetachedNode`.

[ ] [[tests/Shared.Tests/GraphChildMapHelpers.fs]], `setChildMap`, merges Loaded list pairs into existing `childMap`.

## Batches

[ ] ``Graph absent childMap key is Unloaded empty is Loaded``, ``Graph.fromNodes keeps Unloaded when parent key is absent``, ``Graph childMap Unloaded is absent after round-trip``, ``legacy Graph decode without childrenStatus defaults to Loaded``

[ ] ``legacy package decode rejects Unloaded with non-empty children``, ``StateResponse decode accepts Alan sample node shapes``, ``graphRoundTrip rebuilds Unloaded as Loaded``, ``compose then installWantAnswer loads wanted parents and children``

[ ] ``installWantAnswer keeps absent key Unloaded and [] Loaded``, ``installWantAnswer accepts a Child already Resident``, ``visibleClosureGraph loads reserved Children and Zoom ancestors``, ``visibleClosureGraph bad Zoom stays inside reserved set``

[ ] ``visibleClosureWantAnswer installs through installWantAnswer``, ``rootBootstrapGraph includes nested workspace header Unloaded``, ``rootBootstrapGraph keeps ROOT owned content Loaded``, ``rootBootstrapGraph includes Ref header without children``

[ ] ``bootstrapGraph with zoom outside ROOT adds complete owning Workspace``, ``bootstrapGraph with no zoom is ROOT only``, ``bootstrapGraph with zoom inside ROOT does not duplicate residency``, ``bootstrapGraph with missing zoom falls back to ROOT only``

[ ] ``bootstrapGraph zoom Workspace keeps nested Workspace header Unloaded``, ``bootstrapGraph zoom Workspace includes Ref header without children``, ``sessionBootstrapTarget uses focus when zoom stays in ROOT``, ``truncationGraph drops Load-only nested Workspace children``

[ ] ``applyServerTail skips structural Replace on Unloaded parent``, ``applyServerTail applies header facts on Unloaded resident Node``, ``applySyncResponse installs complete child list as Loaded and preserves owner``, ``applySyncResponse empty Loaded child list marks Loaded without History clear``

[ ] ``applySyncResponse installs Want answer after Event tail``, ``applySyncResponse Want-answer empty list marks Loaded leaf``, ``applySyncResponse Load Fetch answer installs through installWantAnswer``, ``applySyncResponse re-applies pending after Want rewind``

[ ] ``nested paste and NewSpecialNode Undo and Redo preserve reachable structure``, ``split-shaped Change Undo and Redo preserve sibling semantics``, ``delivered inverse of large paste measures phases without per-created-Node rebuild``, ``answer-only Load at the same event id preserves ClientHistory``

[ ] ``planStubOps creates Directory named .agents with file child``, ``Load Unloaded stub plan must not name-conflict on resident server``, ``rowChildrenIndicator is HollowCircle when children are Unloaded``, ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children``

[ ] ``rowChildrenIndicator is FoldChevron when Loaded with children``, ``bulletTip minimal node lists guid residency and update time only``, ``bulletTip disambiguates hollow by Unloaded versus Unparsed``, ``bulletTip reads Loaded Current for a parsed leaf``

[ ] ``OUTER is Owned only, strictly below, Unloaded miss, not a tree prune``, ``owned and ref answers on Unloaded node yield empty sequence``, ``Unloaded input: child, owned, and ref miss and do not Load``, ``getState zoom outside ROOT adds owning Workspace``

[ ] ``getState without zoom keeps nested Workspace Unloaded``, ``GET state default returns ROOT closure excluding nested workspace contents``

## Extra batches

None.
