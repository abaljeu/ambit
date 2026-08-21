# Grill round 3

Worker report. Parent speaks the user-facing round unchanged. [[WORK.md]] stays Active; no board mutations.

## Settled this round

Recorded in [[goal.md]]. Q2A + standard-now / fuller-later. Q3C + producer "Op-valid before transfer" — not treated as Q3-B.

## Facts (not for the user unless one line is needed)

- CAS and `Replace` span-CAS are apply-time (`old* does not match`). They are not a property of a sitting Graph.
- A static Graph check that exists: `History.validateOwnership` (owner edges, File/Directory placement, artifact names). Unloaded children must be empty. Partial Graphs already skip "missing owner" when the claimed owner is Unloaded or Absent.
- Poll already applies a Change tail (lagging prefix → eventual consistency). Load packages already install a resident subset (`installPackages`). Local pending is in-flight Ops on a Graph that may not match any Server revision.

## User-facing round (speak unchanged)

❓ **Q4** - **Producer gate ≠ receiver reject**: You picked C (no new install reject; `/state` may install Nodes no Change produced) *and* "Op-valid before transfer is proposed." CAS/span-CAS are apply-time, not a sitting-Graph property. A static check can mean ownership (and like). That is not Q3-B unless transfer fails closed.

A) **Honor C.** Server must not *propose* an Op-invalid Graph (checklist). GET `/state` and Load do **not** fail closed. Receiver map-merges, including Nodes no Change produced.

B) You meant **Q3-B**. Transfer **fails closed** if the Graph is Op-invalid. Receiver still does not run `Op.apply` on the blob.

C) Producer and receiver both reject. Name the check.

Who checks? Does GET `/state` / Load return an error, or is "don't propose" only a Server duty?

➡️ You cannot keep C *and* a citable standard with teeth. **B** if relaxed-concurrency must implement against this. **A** only if you accept an honor system and no new test.

❓ **Q5** - **Partial log — what may a partial view believe?**: Poll already is a lagging prefix (eventual consistency). Load already installs a resident subset, not a prefix. "More complicated than log→state" describes today unless you name a new belief rule.

A client with a partial view may believe:

A) **Resident subset:** my Nodes are last-applied Server facts for those ids. Unloaded/Absent are unknown, not false.

B) **Lagging prefix:** I applied Changes through revision N and nothing after. Missing Nodes are "not yet."

C) **In-flight mix:** some Ops applied, some pending. The Graph need not match any Server revision.

D) **A+C** (today's Browser). Then name what I must *not* believe.

➡️ **D** is today's Browser. **B** is today's Poll. If the standard is A+B+D, say what is *new* besides writing it down. Do not call Poll's eventual consistency a new model.

## Next-next frontier (do not ask yet)

1. Process boundary (Server / Browser / both / `POST /changes`) — deferred; Q4 already names producer vs receiver.
2. Load freshness: must-see vs may-lag vs lie (depends on Q5).
3. Which holes are in scope (package install, silent Unloaded skip, Browser apply without ownership validate, startup Graph replace).
4. HITL vs agentic vs long-running — one apply/reject model?
5. What "fuller system later" is allowed to add without breaking this standard.

## WORK.md mutations

None. Keep Active on [[.scratch/event-sourced-ops/project.md]].
