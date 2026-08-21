# More general relaxed concurrency

Worker report. Design-scope claim. No software. Do not archive or cancel [[.scratch/relaxed-concurrency/]]. Event-sourced-ops Stage stays `charting`. Relaxed-concurrency Stage stays `spec`.

## The claim

[[.scratch/event-sourced-ops/]] has effectively defined a **more general relaxed concurrency** than [[.scratch/relaxed-concurrency/]]. Sibling, not a replacement. The older map's knowns and rejections still stand.

## In what sense more general

The older destination is small: drop the **global revision gate**; accept a Change when per-Op `old*` CAS matches; reject a genuine collision. Slices 2–3 (deferred): Reject body carries remote changes; Client merges and **replans** failed items at `pendingChanges` tail. G: no server weak-form Replace. No model change, no wire change for slice 1.

This framework is a **Merge** of Changes from a common prior, for any Actors (not only two Browsers):

- **Global order:** Server arrival sequences Changes.
- **Server produces:** common prior → other Actors' accepted Changes → newest Actor's Change **amended** against that combined Local Graph ([[merge.md#Amendment order]]).
- **Client consumes:** rewind, then replay that sequence — not genesis replay ([[merge.md#Client correction]]).
- **Independence still conveyed:** disjoint field / disjoint parent-list is simple Merge, not "skip the other Change" ([[conflict-kinds.md]]).
- **Collision is not always Reject:** same-text and **Name** keep first on the Node + `amb-conflict` child; children Accept Both (span-CAS is behavior to beat); classes are set delta.
- **Unified success envelope (accepted):** POST-ACK and Poll share a Change list; POST sends Changes, Poll does not. Rewind then apply; POST does not clear History. Recoverable kick-back is 200, not Reject.

The older picture is **per-path**: apply if CAS, else Reject (later: Reject + replan that pending item). This picture is **one sequence** that also covers the cases CAS would reject, by amending the newest Change. Slice 1 (drop the gate) can still be a first implementation step. It is not the whole framework.

## What stays from the older map

- Full Event Sourcing / **genesis replay — rejected**. Snapshot is the record; Poll is a tail (`getChangesSince`). Parse logs Op diffs. Rewind+replay is a short tail from the common prior, not replay from empty.
- Ops are per-Node field or per-parent child list. No graph-wide Op.
- G: **no** server-side silent relocation in `Graph.replace`.
- Slice 1 spec (drop the revision gate; keep per-Op CAS on the apply path) is **not cancelled**. Rejection stays legal where this framework still says reject (Name; unrecoverable cases).
- Out of scope there (order-CRDTs, offline, genesis) stays out of scope here unless a later increment says otherwise.

## Tensions (do not paper over)

- **Slice 2 Reject vs unified success ACK — resolved.** Recoverable kick-back is 200 Merge. Slice 2 Reject+replan is **obsolete** for that case ([[reject-vs-success-ack.md]], [[slice-2-obsoleted.md]]). Remaining Reject is auth, malformed POST, and similar request failures. Name is `amb-conflict` ([[name-clash-amb-conflict.md]]).
- **Rewind+replay vs leftover `pendingChanges`.** Consume of the POSTed Change is rewind+replay of the 200 list. Leftover pending stays unamended; next POST sends it; Server amends (grill-round-6; Q1 B superseded). Slice 2 replan of the *failed posted item* is obsolete.
- **This is not genesis ES.** Same rejection as the map. Do not reopen log-as-truth.
- **Model and wire.** Older slice 1: no model change, no wire change. This framework **is** a Merge model and (when unified messaging lands) an ACK-kind change. Do not pretend they are the same increment.
- **Children / text.** Older slice 1 keeps span-CAS and attribute CAS as the reject boundary. This spec treats those apply paths as behavior to beat for merge cases that Accept Both or keep both texts.

## Files changed

- [[project.md]] — related line states the claim
- [[goal.md]] — parent-rejection line
- [[vocab.md]] — pointer
- [[collab-vocab.md]] — speak
- [[.scratch/relaxed-concurrency/project.md]] — pointer only; Stage unchanged
- [[.scratch/relaxed-concurrency/map.md]] — short related note; spec not rewritten
- This file

No software. No [[WORK.md]] edit here.

## WORK.md mutations

Update the Active [[project.md]] related list: add [[more-general-relaxed-concurrency.md]]. Keep [[.scratch/relaxed-concurrency/map.md]] related. Do not `remove` or `block` the Pending relaxed-concurrency spec/map items. No `add` of a new work item.
