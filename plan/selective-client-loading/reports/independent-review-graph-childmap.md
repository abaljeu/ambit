# Independent review — Graph.childMap

Reviewer did not write the implementation. This report is not approval. No ticket Status changes.

**Range:** `origin/staging...HEAD` at `c76009ee`. Base `origin/staging` `0170be5b`. Command: `git diff origin/staging...HEAD`. Diff is non-empty (158 files, +3453 / −3506). One commit: `Move Node children onto Graph.childMap`.

**Spec:** [Selective client loading spec](plan/selective-client-loading/spec.md) (residency model; User story 9; Testing Decisions) and [14 — Simplify selective client loading](plan/selective-client-loading/issues/14-simplify-selective-loading.md). The PR body is the representation contract: absent `childMap` key = Unloaded; present key including `[]` = Loaded. Tickets 21+ are out of range.

**Axis reports:** [Standards](code-review-standards-graph-childmap.md), [Spec](code-review-spec-graph-childmap.md).

Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH). No function over 40 lines. No file-limit hit.

## Standards

### 1. Line length in Api.fs (hard)

[.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) limits each source line to 100 characters. [Api.fs](src/Server/Api.fs) line 68 is 113 characters (`loadPackages` return type `Node list * Map<NodeId, ChildNode list>`).

### 2. Line length in SerializationTests.fs (hard)

The same line-length rule applies to test source. The 800-line file exemption does not apply. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) added line 66 (229 characters, legacy Graph JSON) and line 78 (169 characters, legacy package JSON).

### 3. Dual names for Children lookup (smell: Mysterious Name)

[GraphChildren](src/Shared/Model.fs) uses `get` / `tryGet` / `status`. [GraphOps](src/Shared/GraphOps.fs) adds `Graph.children` / `Graph.tryGetChildren` / `Graph.childrenStatus`. `get` returns `[]` for Unloaded, so Unloaded and Loaded-empty look the same at that call site. Client, Paste, and some Server files call `Graph.children`. Other Shared files call `GraphChildren.get`.

### 4. GraphBuild lookup wrappers (smell: Middle Man)

[GraphBuild.fs](src/Shared/GraphBuild.fs) `tryGetChildren`, `getChildren`, `isLoaded`, and `childrenStatus` only call [GraphChildren](src/Shared/Model.fs).

## Spec

The childMap move is a semantic-preserving refactor of the shipped residency model. [spec.md](plan/selective-client-loading/spec.md) Implementation Decisions and [14 — Simplify selective client loading](plan/selective-client-loading/issues/14-simplify-selective-loading.md) still say `Node.children` plus `childrenStatus`. That is **doc drift**, not a missing product requirement.

### 1. Document overlay walks the context childMap (wrong)

User story 9: a loaded empty child list stays distinguishable from an unloaded list. Testing Decisions: an unloaded child list is never treated as an authoritative empty list. PR contract: Node no longer stores children; `memberNodeIds` must walk the read’s Loaded lists.

Staging [mergeReadResult](src/Shared/documents/DocumentFormat.fs) did `{ context with nodes = readResult.nodes }` and `memberNodeIds` walked `node.children` on those replaced Nodes. After the move, `GraphChildren.get` reads `graph.childMap`, which is still the **context** map. Overlay ids therefore follow the old lists.

A cold read of a Loaded-empty File overlays only the File id. The File can receive new child edges while descendant Nodes are omitted. `GraphChildren.get` then returns `[]` for those missing ids. [DocumentColdParse.readArtifactCold](src/Shared/documents/DocumentColdParse.fs) and [DocumentWarm](src/Shared/documents/DocumentWarm.fs) both call this merge.

Confirmed: `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~readArtifact cold Amb ignores previous"` failed. [DocumentAssemblyTests](tests/Shared.Tests/DocumentAssemblyTests.fs) line 401: `after.nodes.[(Graph.children after docId).Head.id]` throws `KeyNotFoundException`. The File list has a child id; that Node is not in `nodes`.

### 2. Graph.replace Unloaded is untested

PR contract: `Graph.replace` on an Unloaded parent is Error. [GraphMutate.replace](src/Shared/GraphMutate.fs) returns `"parent children not loaded"`. No test asserts that message. A `Replace([], …)` against `get` = `[]` is the empty-leaf trap.

### 3. Load codec does not prove Unloaded package headers

Testing Decisions require codec coverage of Unloaded and Loaded. Graph Unloaded round-trips. [LoadResponse round-trip with packages](tests/Shared.Tests/SerializationTests.fs) only uses `packageChildMap = [node, []]` (Loaded empty). No LoadResponse case has a package Node whose id is missing from `packageChildMap`.

## Independent checks

These were asked for on this review. They are not a third axis; they confirm or extend the Spec findings.

**Unloaded ≠ empty leaf (helpers).** [GraphChildren](src/Shared/Model.fs) `tryGet` / `isLoaded` / `status` are correct. New detached Nodes get `childMap[id] = []` in [addDetachedNode](src/Shared/GraphBuild.fs) and [newNode](src/Shared/GraphBuild.fs). [ResidentProjection.applyOp](src/Shared/ResidentProjection.fs) skips Unloaded Replace. [installPackages](src/Shared/ResidentProjection.fs) removes package ids, then writes `packageChildMap`; a missing package id stays Unloaded. [SyncLogic.applySyncResponse](src/Shared/SyncLogic.fs) installs packages after the projected tail. History undo of NewNode removes the `childMap` key.

**`get` vs `tryGet`.** SiteMap, search, Zoom, and Expr walks that use `get` and treat Unloaded as a leaf match the spec (unloaded Nodes behave as leaves when completeness is not required). [ViewModelChildrenIndicator](src/Shared/ViewModelChildrenIndicator.fs) uses `isLoaded` for the hollow circle. The dangerous `get` is the document overlay walk in Spec finding 1.

**Serialization / ApiVersion / persistence.** Write is `nodes` + `childMap`. Decode uses `childMap` when present, else legacy per-node `children` / `childrenStatus`. Unloaded-with-children still fails decode. `ApiVersion.current` is 12. Load/Sync wire field `childMap` is `packageChildMap`. DB [graphFromPersistence](src/Shared/GraphProjection.fs) still marks every persisted Node Loaded. [Snapshot.read](src/Shared/Snapshot.fs) marks every present outline Node Loaded. Those persistence limits match the PR follow-up note.

**Performance.** [addDetachedNode](src/Shared/GraphBuild.fs) and [appendChildren](src/Shared/GraphBuild.fs) do incremental `Map.add`. SiteMap and search do one `get` per parent. `installPackages` merges maps once, then one `fromNodes`. Non-append `replace` still rebuilds via `fromNodes` (same as staging). No new full-`childMap` rebuild on every child touch.

**Test coverage.** Focused suites named in the PR (Model/Graph/Sync/Load/Bootstrap/Serialization/History) do not include document assembly. The failing cold Amb test is outside that set. Unloaded vs Loaded-empty is covered for Graph equality, Graph JSON, and some Expr/ViewModel cases; not for Load package headers or `Graph.replace`.

## Verdict

**Needs fixes.**

### Must-fix

1. **Document overlay walks the context childMap** — [mergeReadResult](src/Shared/documents/DocumentFormat.fs) must walk the **read** child lists (for example set `graphWithRead.childMap` from `readResult.childMap` before `memberNodeIds`). Cold and warm artifact read, assembly, and parse-plan are broken. Re-run document tests, including [readArtifact cold Amb ignores previous when None](tests/Shared.Tests/DocumentAssemblyTests.fs).

### Should-fix

2. Add a test that `Graph.replace` on an Unloaded parent returns `"parent children not loaded"`.
3. Add a LoadResponse / `installPackages` case where a package Node id is absent from `packageChildMap` and stays Unloaded.
4. Update [spec.md](plan/selective-client-loading/spec.md) and [14 — Simplify selective client loading](plan/selective-client-loading/issues/14-simplify-selective-loading.md) so the residency model names `Graph.childMap` instead of `Node.children` / `Node.childrenStatus`.
5. Wrap the 113-character `loadPackages` type line in [Api.fs](src/Server/Api.fs). Wrap the two long JSON fixtures in [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs).

Standards: 4 findings (worst: 113-character line in [Api.fs](src/Server/Api.fs)). Spec: 3 findings (worst: document overlay invents child edges without descendant Nodes).
