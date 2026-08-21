# Goal (stub)

Mode: **collaborative, incremental**. This is a small framework for async behaviors, not an architecture.

**Draft:** An Actor produces a Change (a list of Ops). To merge two Changes from a common prior Local Graph, sequence them and adjust the second so that critical information is not lost (unless an Actor removed it). Every Node has a single Owned parent except ROOT and Orphaned Nodes (Orphaned means not Owned-reachable; this documents as-implemented reachability, not a new delete model). Which Node is the owner is not critical. Do not use the term Deleted.

Terms: [[vocab.md]]. Kinds: [[conflict-kinds.md]]. Merge: [[merge.md]]. Server fill-in: [[server-fill-ops.md]]. Soft lock: [[soft-lock.md]]. Undo: [[undo.md]].

Parent rejection to beat: [[.scratch/relaxed-concurrency/map.md]] — genesis replay was rejected; Parse already logs Op diffs; the snapshot is the record.

## Parked (need more thought)

- Changes vs Graph transfer (Load packages).
- Whether Revision stays one global number.
- Q4/Q5 on `/state`.

## Grill record

Q1: success is a semantic foundation, not a replacement of `/state` with a Change stream. Q2A: write the standard now; a fuller system comes later. Q3C plus a producer Op-valid rider; fail-closed versus honor remains deferred.
