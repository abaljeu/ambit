# Relaxed concurrency

## Destination

Let two clients edit unrelated parts of the Graph at the same time without one Change rejecting the other. The candidate destination is small: remove the **global revision gate** in [[src/Server/FileAgent.fs]] and rely on the **per-op preconditions that the Ops already carry**. Rejection stays a legal outcome for a genuine collision.

Event Modeling / Event Sourcing and genesis replay were examined and rejected. This map records verified knowns, active decisions, and open questions.

## Verified knowns

Each item was checked against source while writing this map.

### 1. Every Op is per-node or per-parent-edge-list

`Op` has eight cases ([[src/Shared/History.fs]] 4-20). Seven carry a single `nodeId` and touch only that Node's own fields: `NewNode`, `SetText`, `SetClasses`, `NewSpecialNode`, `SetName`, `SetDocumentState`, `SetUpdateTime`. The eighth, `Op.Replace`, carries a `parentId` and touches only that parent's outgoing child list. There is no Op with graph-wide reach. Op granularity is therefore already fine enough for per-target conflict detection.

### 2. Attribute Ops are already compare-and-swap

`Graph.setText` compares the supplied `oldText` against the live Node and returns `Error "old text does not match"` ([[src/Shared/GraphMutate.fs]] 55-56). The same old-vs-new gate holds for `setClasses` (77-78) and `setDocumentState` (163-164), and for `setName`. The `old*` fields exist for undo, but they double as concurrency preconditions at no extra cost.

Two Ops are **not** compare-and-swap and should not be counted as such: `NewNode` / `NewSpecialNode` create rather than update, and `Op.SetUpdateTime` deliberately ignores a mismatch — the doc comment says "`oldTime` is for undo; apply ignores mismatch" ([[src/Shared/History.fs]] 19).

### 3. The current conflict model is a single global gate

`FileAgent.applyBatch` rejects a Change when `change.id <> s.revision.Value`, with `"Revision mismatch: server is at revision …, but this Change targets base revision …"` ([[src/Server/FileAgent.fs]] 150-152). One global counter, one comparison. The consequence is that **any** committed Change anywhere in the Graph invalidates **every** other client's in-flight Change, however unrelated the two edits are.

The reported "occasional conflict between clients" is this rejection. It is not data corruption. The safety property is intact; the liveness property is bad.

### 4. `Replace` index staleness is confined to one parent

`Replace`'s `index` is an offset into one parent's `children`. Index staleness can therefore only arise between two structural edits on the **same** parent's child list. Structural edits under different parents do not interact at all — they read and write disjoint Node records.

### 5. Parse already follows correct event discipline

The parse path is `Api.postParseFile` ([[src/Server/Api.fs]] 315) → `DocumentPersistence.planParseFile` ([[src/Server/DocumentPersistence.fs]] 230) → `ImportDocument.planParseFile` ([[src/Shared/dotnet/ImportDocument.fs]] 95) → `DocumentParseOps.planApplyArtifact` → `handle.postGraphOnlyChange` ([[src/Server/Api.fs]] 351-352).

The server runs the parser and logs the **resulting Op diffs**, not a "reparse this file" instruction. Historic parsers never need to be retained; replaying parses from git-versioned file contents is unnecessary. Non-determinism is resolved server-side at emit time — `Guid.NewGuid()` for `changeId`, `Op.SetUpdateTime` from `File.GetLastWriteTimeUtc`.

### 6. A `NodeId` can legally appear more than once under one parent

`ChildNode` is `{ ref: Ownership; id: NodeId }` with no occurrence discriminator ([[src/Shared/Model.fs]] 36-38). Owner+Ref and Ref+Ref under one parent are both legal and intentional; index in the list is the only thing that separates duplicate ids. Full evidence: [[child-occurrence-uniqueness.md]].

### 7. Replay from genesis is not required

`getState` is a full snapshot and `getChangesSince` a tail ([[src/Server/Api.fs]] 116, [[src/Server/FileAgent.fs]] 352). The system is **event streaming against a snapshot of record**, not Event Sourcing proper — only events inside a retention window need decoding.

### 8. `Graph.replace` already performs full-value span compare-and-swap

`Graph.replace` compares the live slice at `index` against `oldChildren`, returning `Error "old span does not match"` on mismatch ([[src/Shared/GraphMutate.fs]] 241-247). The cheap win is "delete one check in `FileAgent`", not "add comparison to `GraphMutate` first". Audit: [[replace-span-cas-feasibility.md]].

## Decisions so far

Nothing here is a Committed Decision; the stage is `spec`.

### Full Event Sourcing with replay from genesis — rejected

Parse path already logs diffs (known 5); snapshot is the record (known 7). Genesis replay buys nothing and would mis-parse old files through new parsers.

### Id-anchored `Replace`

- **Strong form** (drop `index`, locate span by id run alone) — **rejected**; ambiguous when duplicate ids under one parent ([[child-occurrence-uniqueness.md]]).
- **Weak form — server-side silent relocation in `Graph.replace`** — **rejected**. Contiguous-run matching may run during **client replan** after merge (slice 3). Rationale: [[design.md#Client vs server replan]].

Recovers non-overlapping same-parent structural edits that fail span CAS at the planned offset. Under slice 1 those still reject; under slices 2–3, recoverable cases merge and replan instead of wiping the queue.

### The candidate cheap win

Replace the global revision gate with the per-op preconditions that already exist. Expected result:

- Every attribute edit becomes concurrent (known 2).
- Every structural edit under a **distinct** parent becomes concurrent (known 4).
- Only same-parent structural collisions reject via span comparison (known 8).

No model change. No wire-format change. Recoverable same-parent collisions deferred to client merge-sync (slices 2–3).

### Rejection is a legitimate outcome

Not pursuing order-CRDTs, tombstones, or convergence without rejection — neither offline editing nor tiebreak convergence is a goal.

### Same-parent structural rejection — keep today's Reject path (slice 1)

Slice 1: server rejects through existing apply path; client uses today's Reject flow. No silent server-side relocation.

Slices 2–3 narrow this: recoverable failures merge + replan at `pendingChanges` tail; only unrecoverable collisions keep today's terminal `ServerRejected` wipe.

### Client merge-sync — RESOLVED (G)

G answered **YES** via client merge + replan, not server weak form. Full rationale: [[design.md#Client vs server replan]].

**Later (event-sourced-ops, design only):** slice 2's Reject + remote + replan + POST-again is **obsolete** for recoverable concurrent kick-back. That case is HTTP 200 Merge (A accepted, then B amended; Client rewind+replay). G's rejection of server weak-form Replace **stands**. Slice 1 (drop the gate) **stands**. Software does not do 200-Merge yet. [[.scratch/event-sourced-ops/details/relation-to-relaxed-concurrency.md]]. Remaining Reject is auth, malformed POST, and similar request failures. Name is `amb-conflict`, not Reject.

**Protocol:**

1. Client POSTs pending batch.
2. Server can apply → OK ack (as today).
3. Intervening commits prevent apply → **Reject** body **includes remote/extra changes**.
4. Client merges, replans failed items (Replace uses contiguous-run fallback when recoverable), appends to **tail** of `pendingChanges`, resubmits.
5. Unrecoverable collisions → terminal reject (`ServerRejected` wipe).

After slice 1: stale base revision alone does **not** reject — kick-back only on apply failure with remote payload.

**Slice layering:**

| Slice | Scope | Notes |
| --- | --- | --- |
| 1 | Gate removal | Current [[spec.md]] — today's Reject client path |
| 2 | Merge reject payload | **Obsolete** for recoverable kick-back — see event-sourced-ops 200 Merge. Still listed as the old protocol below. |
| 3 | Replace replan | Contiguous-run fallback during replan |

**Slice 2 blockers:** `reconcileAck` exact-match requirement; server batch fail-fast; `rejectPending` queue wipe; `ChangeBatchAck` has no remote-changes field. **Existing primitive:** `SyncPlanner.restorePending` / `mergePendingAfterLoad` in [[src/Client/App.fs]].

**Open forks (slices 2–3):** reject ack wire shape; partial batch vs full reject-on-first-failure; optimistic graph rebuild before replan.

## Open questions

### A. Does `Graph.replace` compare the span? — RESOLVED

Yes (known 8). Audit caveat: `SetUpdateTime` ignores mismatch by design; attribute Ops already carry old-value preconditions (known 2).

### B. Are all `Op.Replace` producers faithful in constructing `oldChildren`?

Mostly yes. Production gap: `ViewModelJoinOps.removeCurrentOp` fabricates Owner edge on Ref rows ([[src/Shared/ViewModelJoinOps.fs]] 31-32). Keep full-value comparison; fix the join planner. Also: import/cold-parse batch-ordering invariants worth regression tests if same-parent concurrency becomes common.

### C. Same-parent structural rejection? — RESOLVED (narrowed)

Slice 1: today's Reject path. Slices 2–3: merge + replan for recoverable cases — see **Client merge-sync** above.

### D. Hybrid authority — log for the Graph, files for their own text

User-chosen direction; not worked out. Boundary is **document-derived subgraph versus graph-native overlay**. `DocumentState = Current | Unparsed | NoServerFile` ([[src/Shared/Model.fs]] 81-84) is an embryonic state machine.

### E. Identity stability of document-derived Nodes across reparse

Warm LCS path preserves ids when lines unchanged ([[src/Shared/dotnet/ImportDocument.fs]], [[src/Shared/dotnet/DocumentParseOps.fs]]). Open: does identity need an explicit anchor in artifact text rather than a diff heuristic?

### F. Undo under a command/event split

If pursued, events would not carry old values; undo becomes compensating commands server-side. Not urgent — current Ops carry old values and undo works today.

### G. Weak form of id-anchored `Replace`? — RESOLVED

**YES** via client merge-sync (slices 2–3), **not** server-side relocation. See **Client merge-sync** and [[design.md#Client vs server replan]].

## Related later work (pointer only)

[[.scratch/event-sourced-ops/]] charts a **more general** Merge (global Change order, Server amends newest, Client rewind+replay). It does not rewrite this map. Genesis replay stays rejected. Slice 1 and G (no server weak-form Replace) stand. Slice 2 Reject+replan is **obsolete** for recoverable kick-back ([[.scratch/event-sourced-ops/details/relation-to-relaxed-concurrency.md]]). Remaining pending after a 200 POST is still open.

## Out of scope

- Order-CRDTs, tombstones, convergence without rejection.
- Offline editing.
- `ChildNode` occurrence or edge identity changes.
- Retaining historic parsers; genesis replay; log-format long-term compatibility.
