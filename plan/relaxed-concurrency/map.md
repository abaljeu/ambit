# Relaxed concurrency

## Role

Build-upon layer on [[plan/event-sourced-ops/overview.md]]. This map records verified Graph/Ops apply-path facts, shared rejections, and frontier open questions D–F. The **active concurrency standard**, protocol, and implementation are event-sourced-ops — not this project.

## Verified knowns

Each item was checked against source while writing this map.

### 1. Every Op is per-node or per-parent-edge-list

`Op` has eight cases ([[src/Shared/History.fs]] 4-20). Seven carry a single `nodeId` and touch only that Node's own fields: `NewNode`, `SetText`, `SetClasses`, `NewSpecialNode`, `SetName`, `SetDocumentState`, `SetUpdateTime`. The eighth, `Op.Replace`, carries a `parentId` and touches only that parent's outgoing child list. There is no Op with graph-wide reach. Op granularity is therefore already fine enough for per-target conflict detection.

### 2. Attribute Ops are already compare-and-swap

`Graph.setText` compares the supplied `oldText` against the live Node and returns `Error "old text does not match"` ([[src/Shared/GraphMutate.fs]] 55-56). The same old-vs-new gate holds for `setClasses` (77-78) and `setDocumentState` (163-164), and for `setName`. The `old*` fields exist for undo, but they double as concurrency preconditions at no extra cost.

Two Ops are **not** compare-and-swap and should not be counted as such: `NewNode` / `NewSpecialNode` create rather than update, and `Op.SetUpdateTime` deliberately ignores a mismatch — the doc comment says "`oldTime` is for undo; apply ignores mismatch" ([[src/Shared/History.fs]] 19).

### 3. The global revision gate was removed

Previously `FileAgent.applyBatch` rejected a Change when `change.id <> s.revision.Value`, with `"Revision mismatch: server is at revision …, but this Change targets base revision …"` ([[src/Server/FileAgent.fs]]). One global counter blocked every unrelated concurrent Change. ESO issue 02 removed that gate; per-op preconditions now bound collisions. See [[plan/event-sourced-ops/details/as-implemented-facts.md]].

### 4. `Replace` index staleness is confined to one parent

`Replace`'s `index` is an offset into one parent's `children`. Index staleness can therefore only arise between two structural edits on the **same** parent's child list. Structural edits under different parents do not interact at all — they read and write disjoint Node records.

### 5. Parse already follows correct event discipline

The parse path is `Api.postParseFile` ([[src/Server/Api.fs]] 315) → `DocumentPersistence.planParseFile` ([[src/Server/DocumentPersistence.fs]] 230) → `ImportDocument.planParseFile` ([[src/Shared/dotnet/ImportDocument.fs]] 95) → `DocumentParseOps.planApplyArtifact` → `handle.postGraphOnlyChange` ([[src/Server/Api.fs]] 351-352).

The server runs the parser and logs the **resulting Op diffs**, not a "reparse this file" instruction. Historic parsers never need to be retained; replaying parses from git-versioned file contents is unnecessary. Non-determinism is resolved server-side at emit time — `Guid.NewGuid()` for `changeId`, `Op.SetUpdateTime` from `File.GetLastWriteTimeUtc`.

### 6. A `NodeId` can legally appear more than once under one parent

`ChildNode` is `{ ref: Ownership; id: NodeId }` with no occurrence discriminator ([[src/Shared/Model.fs]] 36-38). Owner+Ref and Ref+Ref under one parent are both legal and intentional; index in the list is the only thing that separates duplicate ids. Full evidence: [[child-occurrence-uniqueness.md]].

### 7. Replay from genesis is not required

`getState` is a full snapshot and `getChangesSince` a tail ([[src/Server/Api.fs]] 116, [[src/Server/FileAgent.fs]] 352). The system is **event streaming against a snapshot of record**, not Event Sourcing proper — only events inside a retention window need decoding.

### 8. `Graph.replace` performs full-value span compare-and-swap

`Graph.replace` compares the live slice at `index` against `oldChildren`, returning `Error "old span does not match"` on mismatch ([[src/Shared/GraphMutate.fs]] 241-247). Audit: [[replace-span-cas-feasibility.md]].

## Shared rejections

Nothing here is a Committed Decision. These are constraints on anything built on the ESO foundation.

### Full Event Sourcing with replay from genesis — rejected

Parse path already logs diffs (known 5); snapshot is the record (known 7). Genesis replay buys nothing and would mis-parse old files through new parsers.

### Id-anchored `Replace`

- **Strong form** (drop `index`, locate span by id run alone) — **rejected**; ambiguous when duplicate ids under one parent ([[child-occurrence-uniqueness.md]]).
- **Weak form — server-side silent relocation in `Graph.replace`** — **rejected**. Rationale: [[design.md#No server weak-form Replace (still valid)]].

### Rejection is a legitimate outcome

Not pursuing order-CRDTs, tombstones, or convergence without rejection — neither offline editing nor tiebreak convergence is a goal.

## Foundation: event-sourced-ops

Delivery for concurrency implementation lives in event-sourced-ops:

- **Gate removal:** done — [[plan/event-sourced-ops/issues/02-independent-concurrent-changes-succeed.md]].
- **Merge, amend, consume:** issues 01–05 done; protocol in [[plan/event-sourced-ops/architecture.md]].
- **Full-list Replace wire:** issues 13–14 done.
- **Remaining Reject:** auth, malformed requests, `CodeOutdated` — [[plan/event-sourced-ops/details/messaging.md]].

## Open questions

### A. Does `Graph.replace` compare the span? — RESOLVED

Yes (known 8). Audit: [[replace-span-cas-feasibility.md]].

### B. Are all `Op.Replace` producers faithful in constructing `oldChildren`? — RESOLVED

Yes in production; `ViewModelJoinOps.removeCurrentOp` now reads the live child at the removal index. Import/cold-parse batch-ordering invariants remain worth regression tests if same-parent concurrency becomes common. Audit: [[replace-span-cas-feasibility.md]].

### C. Same-parent structural rejection? — RESOLVED

Recoverable cases: ESO merge + rewind/replay ([[plan/event-sourced-ops/details/client-consume.md]]). Unrecoverable collisions still reject.

### D. Hybrid authority — log for the Graph, files for their own text

User-chosen direction; not worked out. Boundary is **document-derived subgraph versus graph-native overlay**. `DocumentState = Current | Unparsed | NoServerFile` ([[src/Shared/Model.fs]] 81-84) is an embryonic state machine.

### E. Identity stability of document-derived Nodes across reparse

Warm LCS path preserves ids when lines unchanged ([[src/Shared/dotnet/ImportDocument.fs]], [[src/Shared/dotnet/DocumentParseOps.fs]]). Open: does identity need an explicit anchor in artifact text rather than a diff heuristic?

### F. Undo under a command/event split

If pursued, events would not carry old values; undo becomes compensating commands server-side. Not urgent — current Ops carry old values and undo works today.

### G. Weak form of id-anchored `Replace`? — RESOLVED

Client replan after merge via ESO amend path — **not** server-side relocation. See [[design.md#No server weak-form Replace (still valid)]] and [[plan/event-sourced-ops/details/relation-to-relaxed-concurrency.md]].

## Out of scope

- Order-CRDTs, tombstones, convergence without rejection.
- Offline editing.
- `ChildNode` occurrence or edge identity changes.
- Retaining historic parsers; genesis replay; log-format long-term compatibility.
