# Spec review: Graph.childMap

Range: `origin/staging...HEAD` (`0170be5b`…`c76009ee`). Findings only.

## Representation vs spec

[spec.md](plan/selective-client-loading/spec.md) Implementation Decisions still says: “Node retains its ordinary ordered child list and adds a separate `childrenStatus` with exactly `Unloaded` and `Loaded`.” [14 — Simplify selective client loading](plan/selective-client-loading/issues/14-simplify-selective-loading.md) Residency and graph model says the same: “`Node` keeps ordinary `children: ChildNode list` and separate `childrenStatus`.”

The PR moves children to `Graph.childMap` (absent key = Unloaded; present key including `[]` = Loaded). That is a **faithful semantic-preserving refactor** of the shipped residency model, plus **missing spec update (doc drift)**. Do not treat the old fields as a missing product requirement. Tickets 21+ are out of range.

## Findings

### 1. Document overlay walks the context childMap (wrong)

Spec [User story 9](plan/selective-client-loading/spec.md): “a loaded empty child list to remain distinguishable from an unloaded child list.” Testing Decisions: “an unloaded child list is never treated as an authoritative empty list.” PR contract: “Node no longer stores children”; callers that need Unloaded ≠ empty must use `tryGet` / `isLoaded`.

[DocumentFormat.mergeReadResult](src/Shared/documents/DocumentFormat.fs) still does `{ context with nodes = readResult.nodes }` and then `DocumentPartition.memberNodeIds graphWithRead`. `memberNodeIds` uses `GraphChildren.get`, which reads **context** `childMap`, not `readResult.childMap`. Staging used `node.children` on the replaced Node map, so overlay followed the **read**. Now overlay follows the **old** lists.

A cold read of a Loaded-empty File therefore overlays only the File id. The File can receive new child edges while descendant Nodes are omitted. `GraphChildren.get` then returns `[]` for those missing ids (silent empty leaf). [DocumentColdParse.readArtifactCold](src/Shared/documents/DocumentColdParse.fs) and assembly go through this path.

### 2. Graph.replace Unloaded is untested

PR contract: “Graph.replace on Unloaded parent is Error.” [GraphMutate.replace](src/Shared/GraphMutate.fs) returns `"parent children not loaded"`. No test asserts that message. Testing Decisions ask that an unloaded list is never treated as an authoritative empty list; a `Replace([], …)` against `get`=`[]` is the exact trap.

### 3. Load codec does not prove Unloaded package headers

Testing Decisions: “Response codec coverage … proves `Unloaded` and `Loaded`.” [SerializationTests](tests/Shared.Tests/SerializationTests.fs) round-trips Graph Unloaded (absent `childMap` key) and LoadResponse with `packageChildMap = [node, []]` (Loaded empty). No LoadResponse case has a package Node whose id is missing from `packageChildMap`. [SyncLogicTests](tests/Shared.Tests/SyncLogicTests.fs) installs `external` in `packages` without that key and does not assert Unloaded. PR contract: “a package node id missing from that map stays Unloaded.”

## Not findings

`Graph.children` / `GraphChildren.get` → `[]` for Unloaded is the stated leaf contract for navigation, SiteMap, search, and Expr walk (`tryGet` is used where Unloaded is a miss). `ResidentProjection.applyOp` skips Unloaded Replace. `installPackages` drops package ids then writes `packageChildMap`. New detached nodes get `childMap[id]=[]`. ApiVersion 12; write nodes+childMap; read legacy per-node children. Persistence / Snapshot.read mark present Nodes Loaded. Incremental `Map.add` for detached Nodes; SiteMap/search do one `get` per parent. No demand for tickets 21+.
