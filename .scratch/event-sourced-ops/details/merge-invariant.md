# Merge invariant and amendment order

What merge must protect, and the order in which the Server produces its result. Per-Op transform tables are **later work** and are not written here. Protocol summary: [[.scratch/event-sourced-ops/architecture.md]].

Status: the amendment order is **accepted**. The invariant and the process shape are **proposed** as a whole, although several of the rules inside them are accepted; see [[conflict-resolution.md]] and [[open-questions.md]].

## The invariant

Never lose **critical information**, except when an Actor removed it. Then merge may propagate that removal.

**Removed means removed against that Actor's common prior (proposed).** An Actor consents to losing what it could see, not to losing what another Actor added concurrently. Deleting a Node therefore does not carry away a child a second Actor added to it in the same window. This is the occurrence-bag reasoning of Kind 3 — adds are new slots, removes are prior slots, and they do not cancel — applied to the exemption itself. See Kind 4 in [[conflict-resolution.md]].

Critical:

- Changing a Node's text.
- Adding a cssClass.
- Adding a child edge.
- Orphaning a Node — leaving no Owned path from ROOT — conflicts with changing that Node's critical details. No resolution is named for that conflict; a **less aggressive orphan collection** is only a safety belt. **Open**, see Kind 4 in [[conflict-resolution.md]].

Not critical:

- The order of edges. It is important, but merge may approximate it.
- Which Node is the owner. It is important, but not critical.
- Moving to TRASH. Those children are still Owned by TRASH, so this is not an Orphaning conflict. Landing a concurrent **edit** under TRASH is a separate discoverability problem; tentative recovery is future work in Kind 4.

## Amendment order (accepted)

1. Take the **common prior** Local Graph as the base.
2. Apply the **other Actors' accepted Changes** — every Op in them, not only the Ops that touch the same Nodes.
3. Apply the **newest Actor's** Change, amending its Ops so they fit that combined state, without destroying critical information.

With more than two concurrent Changes, each next Change is newest in relation to those already accepted.

The **Server must** produce this sequence. The **Client must** receive it and rewind and replay it ([[client-consume.md]]).

**Invalid:** the Server looks at the touched Nodes, rewrites the newest Ops so they match those Nodes, and emits only that rewrite. That drops every other-Actor Op on other Nodes and other fields. A one-field apply transform for an optimistic Client — rewriting `SetText C→B` because the Client sits at `C` — is not the general strategy. This was the error the project corrected; see [[decision-log.md]].

## Process, for two Changes

Two Changes are defined on a common prior. Sequence them. Adjust the second so it is compatible with the first, without destroying critical information. In our words: merge the second Change into a Local Graph that already includes the first. The transform is per-Op inside the second Change, after the first Change is in the Local Graph — never against isolated node fields.

Who is first is decided by Server arrival, for text and for everything else.

A soft lock does not change this process ([[soft-lock.md]]).

## Single owner

Every Node has one **Owned** parent, except **ROOT** and except **Orphaned** Nodes. Orphaned means no Owned child reference reaches it; those Nodes stay in the Graph until garbage collection. This documents the existing Owned and Ref distinction. It is not a new delete model. Future merge must preserve this reachability.

**Owner count (accepted).** A well-formed Change does not raise a Node from one Owned parent to two. A Move adds ownership on one parent and removes it on another; one Op may do both. A Delete removes ownership and an Undo puts it back. Two concurrent Moves of the same Node still leave the owner count at one; adjust the second. Dual ownership is therefore **not** a merge case.

**Bug net.** If a Change would raise the owner count from one to two, demote the extra Owned edge to a Ref. Which owner remains is not critical. This is a net for defects, not a merge rule.

Repair of existing defects at startup stays a no-Change path — [[.scratch/owner-edge-db-repair/]].

## What this does not decide

- Child-list merge semantics — acceptBoth and order invariants (context and intent order preserved) are locked in [[replace-amendment.md]] §4; interleaving polish only in issue 10.
- Any per-Op transform table.
- Whether an in-place transform could ever be proved identical to rewind and replay. Until that is proved, it is not equal.
