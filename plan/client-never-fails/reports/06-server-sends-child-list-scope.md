# Scope — [06 — Server sends each Node with its child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md)

Story [Remove Loaded and Unloaded](plan/client-never-fails/spec.md) step 13 and architecture paths 6.1 and 6.2 in [Client never fails on server data architecture](plan/client-never-fails/arch.md) define this slice.

### Signatures

[ ] `ResidentProjection.wantAnswer` in `src/Shared/ResidentProjection.fs` is `let wantAnswer (graph: Graph) (want: NodeId list) : Map<NodeId, ChildNode list> * Node list`.

[ ] `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs` is `let visibleClosureWantAnswer (savedZoom: NodeId option) (graph: Graph) : Map<NodeId, ChildNode list> * Node list`.

[ ] `Api.nodesForRequest` in `src/Server/Api.fs` is `let private nodesForRequest (graph: Graph) (graphIds: NodeId list)` and returns `Node list` via `List.choose (fun id -> Map.tryFind id graph.nodes)`.

[ ] `UniversalResponse` in `src/Shared/ApiResponses.fs` is `type UniversalResponse = { nodes: Node list; events: Ev list; latestId: EventId }`.

[ ] `SyncResponse.childMap` in `src/Shared/ApiResponses.fs` is field `childMap: Map<NodeId, ChildNode list>` on `SyncResponse` (same element type as `ChangeSuccessResponse.childMap` and `LoadResponse.childMap`).

[ ] `encodeUniversalResponse` in `src/Shared/ApiResponseSerialization.fs` is `let encodeUniversalResponse (response: UniversalResponse) : IEncodable`.

[ ] `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs` is `let decodeUniversalResponseDecoder: Decoder<UniversalResponse>`.

[ ] `Serialization.encodeChildMap` in `src/Shared/Serialization.fs` is `let encodeChildMap (childMap: Map<NodeId, ChildNode list>) : IEncodable`.

[ ] `Serialization.decodeChildMap` in `src/Shared/Serialization.fs` is `let decodeChildMap: Decoder<Map<NodeId, ChildNode list>>`.

[ ] `Api.universalResponseFromHandle` in `src/Server/Api.fs` is `let private universalResponseFromHandle (handle: CoreChangesDoor) (after: EventId) (nodes: Node list) : Async<UniversalResponse>`.

[ ] `Api.wantAnswerFromHandle` in `src/Server/Api.fs` is `let private wantAnswerFromHandle (handle: CoreChangesDoor) (want: NodeId list) : Async<Result<SnapshotAnswer, string>>` where `SnapshotAnswer` holds `{ state; nodes; childMap }`.

### Match sites

[ ] `ResidentProjection.wantAnswer` defines `completeEdge`, which uses `List.choose (fun child -> Map.tryFind child.id graph.nodes)` and returns `None` for the whole parent when `List.length nodes <> List.length children` (`src/Shared/ResidentProjection.fs`, function `wantAnswer`).

[ ] `ResidentProjection.wantAnswerForTargets` calls `wantAnswer graph` on filtered target ids (`src/Shared/ResidentProjection.fs`, function `wantAnswerForTargets`).

[ ] `ResidentProjection.captureLoadResponse` reads `(childMap, nodes)` from `wantAnswerForTargets` and sets `LoadResponse.childMap` and `LoadResponse.nodes` (`src/Shared/ResidentProjection.fs`, function `captureLoadResponse`).

[ ] `ResidentProjection.visibleClosureGraph` calls `visibleClosureWantAnswer` and passes `(edges, nodes)` into `Graph.fromNodes` (`src/Shared/ResidentProjection.fs`, function `visibleClosureGraph`).

[ ] `Api.wantAnswerFromHandle` calls `ResidentProjection.wantAnswer state.graph want` (`src/Server/Api.fs`, function `wantAnswerFromHandle`).

[ ] `Api.postPoll` calls `wantAnswerFromHandle` and maps `answer.childMap` and `answer.nodes` into `ChangeSuccessResponse` (`src/Server/Api.fs`, function `postPoll`).

[ ] `Api.postEvents` calls `wantAnswerFromHandle` and maps `answer.childMap` and `answer.nodes` through `acceptedChangeResult` (`src/Server/Api.fs`, function `postEvents`).

[ ] `Api.loadWantAnswer` maps `wantAnswerForTargets` into `SnapshotAnswer.childMap` and `SnapshotAnswer.nodes` (`src/Server/Api.fs`, function `loadWantAnswer`).

[ ] `Api.postLoad` uses `loadWantAnswer` and builds `LoadResponse` with `childMap` and `nodes` (`src/Server/Api.fs`, function `postLoad`).

[ ] `Api.getState` calls `ResidentProjection.visibleClosureGraph` for `BootstrapScope.RootClosure` boot graph (`src/Server/Api.fs`, function `getState`).

[ ] `Api.postLoadSaveCommand` calls `nodesForRequest state.graph request.start.graphIds` then `universalResponseFromHandle` (`src/Server/Api.fs`, function `postLoadSaveCommand`).

[ ] `Api.universalResponseFromHandle` constructs `{ nodes = nodes; events = events; latestId = latestEventId events persistId }` with no `childMap` (`src/Server/Api.fs`, function `universalResponseFromHandle`).

[ ] `encodeUniversalResponse` reads `response.nodes`, `response.events`, and `response.latestId` only (`src/Shared/ApiResponseSerialization.fs`, function `encodeUniversalResponse`).

[ ] `decodeUniversalResponseDecoder` builds `{ nodes; events; latestId }` only (`src/Shared/ApiResponseSerialization.fs`, function `decodeUniversalResponseDecoder`).

[ ] `encodeLoadSaveCommandResponse` nests `encodeUniversalResponse command` when `response.command` is `Some` (`src/Shared/ApiResponseSerialization.fs`, function `encodeLoadSaveCommandResponse`).

[ ] `decodeLoadSaveCommandResponseDecoder` nests `decodeUniversalResponseDecoder` for optional `command` (`src/Shared/ApiResponseSerialization.fs`, function `decodeLoadSaveCommandResponseDecoder`).

[ ] `LoadSaveCommandClient.applyResponse` matches `LoadSavePath.Git, Some command` and passes `command.events` to `commandDone` only (`src/Client/LoadSaveCommandClient.fs`, function `applyResponse`).

[ ] `SyncLogic.graphAfterWant` passes `response.childMap` and `response.nodes` to `ResidentProjection.installWantAnswer` (`src/Shared/SyncLogic.fs`, function `graphAfterWant`).

[ ] `SyncLogic.hasWantPayload` reads `response.childMap.IsEmpty` (`src/Shared/SyncLogic.fs`, function `hasWantPayload`).

[ ] `SyncLogic.loadResponseToSync` copies `response.childMap` from `LoadResponse` (`src/Shared/SyncLogic.fs`, function `loadResponseToSync`).

[ ] `SyncLogic.changeSuccessToSync` copies `response.childMap` from `ChangeSuccessResponse` (`src/Shared/SyncLogic.fs`, function `changeSuccessToSync`).

[ ] `UniversalResponse` record construction site: `Api.universalResponseFromHandle` (`src/Server/Api.fs`).

[ ] `UniversalResponse` record construction site: ``UniversalResponse round-trips nodes events and latestId`` in `tests/Shared.Tests/SerializationTests.fs`.

[ ] `UniversalResponse` record construction site: `gitResponse` helper in `tests/Server.Tests/LoadSaveCommandClientTests.fs` (file outside ticket Inputs).

[ ] `UniversalResponse` field use: `LoadSaveCommandClient.applyResponse` reads `command.events` only.

[ ] `UniversalResponse` field use: ``UniversalResponse round-trips nodes events and latestId`` asserts `latestId`, `nodes`, and `events` after decode (`tests/Shared.Tests/SerializationTests.fs`).

[ ] In `wantAnswer`, a child id whose Header is absent causes the whole parent edge to be omitted from the returned map (not merely dropped from `nodes`).

[ ] In `visibleClosureWantAnswer`, `edges` keeps every child from `GraphChildren.tryGet` without a Header check; `nodes` includes only ids with `Map.containsKey id graph.nodes`.

[ ] In `nodesForRequest`, a requested id with no Header is omitted from the returned `Node list` (no parallel child map today).

### Existing tests

[ ] ``wantAnswer returns wanted edges and pointed-at Nodes`` in `tests/Shared.Tests/WantTests.fs` checks `wantAnswer` returns the parent edge and resident child Nodes for a full edge, and empty maps for `want = []`.

[ ] ``wantAnswer omits an edge with an absent target Node`` in `tests/Shared.Tests/WantTests.fs` builds a parent edge to a missing child Header and asserts `Map.containsKey zoomId edges` is false and `nodes` is empty.

[ ] ``visibleClosureWantAnswer installs through installWantAnswer`` in `tests/Shared.Tests/WantTests.fs` checks `visibleClosureWantAnswer` plus `installWantAnswer` loads a directory parent and includes a file Node.

[ ] ``UniversalResponse round-trips nodes events and latestId`` in `tests/Shared.Tests/SerializationTests.fs` round-trips `{ nodes; events; latestId }` through `encodeUniversalResponse` and `decodeUniversalResponseDecoder` and asserts those three fields.

[ ] ``wantAnswerForTargets returns direct Children and pointed-at Nodes`` in `tests/Shared.Tests/LoadCaptureTests.fs` exercises `wantAnswer` indirectly via `wantAnswerForTargets` with `includeWorkspace = true`.

[ ] Tests that build Unloaded Nodes (for example ``visibleClosureGraph loads reserved Children and Zoom ancestors`` and ``visibleClosureGraph bad Zoom stays inside reserved set`` in `tests/Shared.Tests/WantTests.fs`) are outside this ticket’s required test edits unless behavior forces them to change; ticket Do not load closes editing those tests.

### Copy-from

[ ] `Serialization.encodeChildMap` and `Serialization.decodeChildMap` in `src/Shared/Serialization.fs` already round-trip `Map<NodeId, ChildNode list>` as the wire `"childMap"` list of `{ parent; children }` objects.

[ ] `encodeChangeSuccessResponse` and `decodeChangeSuccessResponseDecoder` in `src/Shared/ApiResponseSerialization.fs` attach `"nodes"` and required `"childMap"` via `Serialization.encodeChildMap` / `decodeChildMap` (Poll and post-Event envelope pattern).

[ ] `encodeLoadResponse` and `decodeLoadResponseDecoder` in `src/Shared/ApiResponseSerialization.fs` use the same `"childMap"` encode and decode pair on `LoadResponse`.

[ ] `Api.postPoll` and `Api.postEvents` already populate `ChangeSuccessResponse.childMap` from `ResidentProjection.wantAnswer` via `wantAnswerFromHandle` (child list on Poll and post, not on `UniversalResponse`).

### implement

[ ] Ticket One job: each state return carries Header plus child list of ids; code today omits whole `wantAnswer` edges when any child Header is missing (`completeEdge` in `src/Shared/ResidentProjection.fs`).

[ ] Ticket One job: `UniversalResponse` must carry the child list; code today has only `nodes`, `events`, and `latestId` (`src/Shared/ApiResponses.fs`).

[ ] Ticket One job: `nodesForRequest` must attach each Node’s child list; code today returns Headers only (`src/Server/Api.fs`, function `nodesForRequest`).

[ ] Ticket process step 2: after change, ``wantAnswer omits an edge with an absent target Node`` must assert the parent edge stays and the absent child id stays in the child list (inverse of current test expectations).

[ ] Ticket process step 3–4: add `childMap` (or equivalent field name matching Poll) to `UniversalResponse`, wire encode/decode, and build that map in `universalResponseFromHandle` from graph child data for requested ids.

[ ] ``UniversalResponse round-trips nodes events and latestId`` must construct a non-empty `childMap`, round-trip it, and assert equality (ticket Output names this test file).

[ ] Adding a field to `UniversalResponse` forces every record literal to gain `childMap`: `Api.universalResponseFromHandle` (in Inputs) and `gitResponse` in `tests/Server.Tests/LoadSaveCommandClientTests.fs` (outside ticket Outputs — parent stop unless that test file is added to Outputs).

[ ] `LoadSaveCommandClient.applyResponse` does not read `childMap` from `UniversalResponse`; this ticket’s named tests do not cover Git Load/Save install of child lists on the Client.

[ ] ``wantAnswer omits an edge with an absent target Node`` observes the change at public seam `ResidentProjection.wantAnswer` (direct call, asserts returned map and node list).

[ ] ``UniversalResponse round-trips nodes events and latestId`` observes the change at public seams `ApiResponseSerialization.encodeUniversalResponse` and `ApiResponseSerialization.decodeUniversalResponseDecoder`.

[ ] Architecture path 6.1 (`wantAnswer`, `visibleClosureWantAnswer`) matches ticket Inputs; path 6.2 (`nodesForRequest`, `UniversalResponse`, encode/decode) matches ticket Inputs; no Input path mismatch found.

[ ] Ticket Inputs claim ``wantAnswer omits an edge with an absent target Node`` “expects a child id with no Header to leave the edge”; current test expects the edge omitted (`Assert.False(Map.containsKey zoomId edges)`), which matches code today and differs from the ticket One job (child id stays in the list).
