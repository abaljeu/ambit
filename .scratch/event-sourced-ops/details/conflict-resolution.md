# Conflict kinds and their resolutions

The taxonomy of concurrent edits, and what merge does with each. The invariant behind these rules is in [[merge-invariant.md]].

Status: the **taxonomy** as a whole is **proposed**. The four resolutions inside it — text, name, classes, children — are **accepted**. Kind 4 is **proposed** throughout.

## Independence is the load-bearing idea

If two Changes do not conflict, Actors can work those areas and merge is simple. An Actor's area is **node fields** and **child-list spans**, not a subgraph blob. Two Actors working under one parent then need not look as if they overlap.

Independence does **not** skip conveyance. A Client still applies the other Actors' accepted Changes before, or has already applied them when, the newest Change is amended.

An earlier sketch said that edits are safe when they do not touch the same Nodes **or** the same outgoing edges. That fights the case of two Actors both adding a child under one parent, because adding a child **is** writing the parent's outgoing edges, and both Actors touch the parent id. Independence is therefore not a set of Node ids. Node fields and child lists are separate axes, and the child list is not one conflict class.

## Kind 1 — disjoint field, disjoint parent list

Different Nodes' fields, or the child lists of different parents. **No conflict.**

## Kind 2 — same Node, same field

**Text (accepted).** The prior value is `A`. Server arrival is first, so `A→B` stays on the Node. The second Change is rewritten: drop its `SetText A→C`, and instead **Add Node** as the first child, with class `amb-conflict` and text `C`, inside that same Change. Every Local Graph converges to that. This is not a dialog, not a Reject, and not last-write-wins.

**Name (accepted).** The same family. Server arrival is first, so that name stays on the Node. The second Change drops its `SetName` and adds a first child with class `amb-conflict` and the new name as its text. This is merge success, not an HTTP Reject.

**Classes are not this kind (accepted).** A Node's classes are a **set of names**, and merge applies a **set delta** against the common prior, not a whole-set replace. From a common prior, the adds and the removes are disjoint, so concurrent class edits are independent and nothing is lost that neither Actor intended. Compute the delta from the common prior and this Actor's new set — never from the already-merged set.

**Update-time stamps are not this kind.** They ignore a mismatch by design.

**DocumentState is not a field.** It is removed from this standard ([[vocabulary.md]]).

## Kind 3 — same parent, child list

**Default (accepted).** The happy path is a **positional Replace**: specified position, specified Nodes. That is the Op the Actor posts. Two inserts do not conflict, in either order.

**Conflict (accepted).** Do not treat a Reject as the whole story. Compute a best approximation. The **critical** invariant is an occurrence-bag **Accept Both** against the common prior: the adds are new slots, the removes are prior slots, and those sets are disjoint in the same way class deltas are. Order is important but not critical. The approximation algorithm is later work.

The bag holds **occurrences** — edges, or child slots — not Node ids. If the elements were Node ids, then adding another `X` and removing `X` could cancel each other, which is an outcome neither Actor intended and a confusion of identity. That reading is **rejected**.

The Server amends the newest Actor's posted Replace into a definitive ordered-list Replace for the combined Local Graph — computed **after** the other accepted Changes are in, never from isolated node fields, and never as a substitute for sending those other Changes.

## Kind 4 — delete against edit

Actor A changes N's content or adds children to N. Actor B deletes N.

**Not a conflict (proposed).** A delete is a **move to TRASH**, so B's delete writes the **old parent's** child list and nothing else. A's edit writes N's text, N's classes, or N's own child list. Different axes, so Kind 1 applies in either arrival order: N lands in TRASH carrying A's text and A's new children. The same holds at depth — deleting an ancestor M touches only M's parent's list, so an edit anywhere below M is independent of it.

This rests on the removal exemption being read against the **common prior** ([[merge-invariant.md]]). B consented to losing what B could see under N, not to losing what A added concurrently. It is the occurrence-bag reasoning of Kind 3: adds are new slots, removes are prior slots, and the two do not cancel.

**Hard Orphaning is not settled.** [[merge-invariant.md]] calls orphaning against a critical edit a conflict and names no outcome, unlike every other kind here. Whether a well-formed Change can even produce one is unsettled, since Server fill-in makes every delete Move-shaped ([[completing-ops.md]]); the clause may only describe a combination across Changes. A **less aggressive orphan collection** is a proposed safety belt — it stops the janitor from defeating the invariant — not a resolution. **Open**, see [[open-questions.md]].

**Recovery when a Change'd Node lands under nothing or TRASH (tentative, future).** Independence keeps A's critical information, but leaving N under TRASH (or Orphaned) is a bad end state: the user watches their typing vanish. The tentative shape is a **wrapper at the old parent**: a Normal conflict Node labeled `deleted`, which **Owns** N, so concurrent delete does not leave N in TRASH. That redraws the `amb-conflict` edge rule below for this kind alone.

Read it as: if the transitive owner of a Node touched by a Change is nothing, or TRASH, recover it somehow. The Change itself does not carry ownership, so the Server cannot see the old parent from the Ops alone. When the Change arrives **before** the delete, correcting afterward is also awkward. The sketched how — not designed — is that a Change carries a **baseline**, and merge checks **history since that baseline** for the conflicting delete. Leave the algorithm as a **future consideration**; see [[open-questions.md]].

## `amb-conflict`

`amb-conflict` is a **node indicator**: a class and a text on a new child. That child is a **Normal Node**. Being a conflict is a role, not a Kind, and there is no Conflict Kind to invent.

It is not an edge-editing device for ordinary child-list conflicts (Kind 3). Kind 4's tentative `deleted` wrapper is the one proposed exception — still future work.

## What is not a conflict at all

Auth failures and malformed requests are request failures, not concurrency. See [[messaging.md]].
