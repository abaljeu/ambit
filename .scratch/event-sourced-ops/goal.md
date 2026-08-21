# Goal (stub)

Mode: **collaborative, incremental**. This is a small framework for async behaviors, not an architecture.

**Draft:** An Actor produces a Change (a list of Ops). To merge Changes from a common prior Local Graph: apply other Actors' accepted Changes, then amend the newest Actor's Change so critical information is not lost (unless an Actor removed it). Node-local corrections that omit the other Changes are invalid. Server produces this sequence. Optimistic Client rewind+replays it (accepted). Every Node has a single Owned parent except ROOT and Orphaned Nodes (Orphaned means not Owned-reachable; this documents as-implemented reachability, not a new delete model). Which Node is the owner is not critical. Do not use the term Deleted.

Terms: [[vocab.md]]. Kinds: [[conflict-kinds.md]]. Merge: [[merge.md]]. Server fill-in: [[server-fill-ops.md]]. Soft lock: [[soft-lock.md]]. Undo: [[undo.md]] (unrestricted desirability open: [[undo.md#Unrestricted Undo desirability]]).

Parent rejection to beat: [[.scratch/relaxed-concurrency/map.md]] — genesis replay was rejected; Parse already logs Op diffs; the snapshot is the record. Relationship: this framework is a more general relaxed concurrency than that map's per-path CAS-or-reject (and slice 2 Reject+replan) — [[more-general-relaxed-concurrency.md]]. The older project is not cancelled.

## Parked (need more thought)

- Changes vs Graph transfer (Load packages).
- Whether Revision stays one global number.
- Q4/Q5 on `/state`.

## Grill record

Q1: success is a semantic foundation, not a replacement of `/state` with a Change stream. Q2A: write the standard now; a fuller system comes later. Q3C plus a producer Op-valid rider; fail-closed versus honor remains deferred. Fill-in timing: same Change as the delete. Owner count: a well-formed Change does not 1→2; Extra-Owned → Ref is a bug net only. Classes: merge is set delta; distinct from implemented whole-set replace. Same-text accepted: Server arrival is first; `B` on the Node; Add Node first child `amb-conflict` text `C`; optimistic Client rewind+replays. Name accepted: first name stays; `amb-conflict` child with the new name (not Reject). DocumentState: field deleted; NoServerFile / Unparsed inferred. Children accepted: default positional Replace (posted Op); conflict is bag Accept Both (approximation later); Server amends newest Replace after other accepted Changes; implemented span-CAS Replace is behavior to beat; `amb-conflict` is a node indicator (text and name), not edges. Amendment order: common prior, then other accepted Changes, then newest amended; Server produces. Client correction: rewind+replay (accepted); `SetText C→B` is not the strategy. Unified POST-ACK / Poll success envelope accepted (same Change-list response; Poll = empty POST `/changes`); rewind then apply; neither clears History (today's Poll clear is debt). Recoverable kick-back is 200 Merge; slice 2 Reject+replan obsolete for that case. Today's ACK is still `SetUpdateTime` suffixes. Unrestricted Undo desirability retained open (possible via global order; see-and-understand not answered) — [[undo.md#Unrestricted Undo desirability]].
