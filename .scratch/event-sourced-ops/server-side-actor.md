# Long-running Server-side Actor — fit

Assessment. Not a lock. Stage still `charting`.

## Verdict

**Partial.** Once each Change **arrives** at Server apply, existing rules determine Merge and how a Browser consumes it. Soft-lock **meaning** is accepted (advisory subtree checkout; Merge still runs — [[soft-lock.md]]). How the job **enters** apply, how it is packaged, and residency are **not** decided.

Vocab already says an Actor may be async and may have little Local Graph (Parse, later agents) — same kind as a user-edit Actor ([[vocab.md]]). This job is that Actor. Not a new product. **Parse File is the first such Actor** — what already fits vs what must realign: [[parse-file-actor.md]]. A later instance of the same kind: [[shell-command-actor.md]] (needs job id / cancel; Parse does not today).

## What is already determined

When a concluding Change is applied, it is the **newest Actor**. **Other Actors** are whoever already landed: Browsers that edited during the long run, other jobs, Parse. Server arrival sequences them.

**Server produce (accepted):** common prior (the base that Change was planned on — typically the subgraph snapshot at plan time) → those other accepted Changes → **amend** this job's Change. Recoverable overlap is 200 Merge (same-text / Name → `amb-conflict` Normal child; children Accept Both). Auth / malformed stay Reject.

If the job's Local Subgraph cannot name a promote-Owned Op, **fill-in** completes **that** Change (timing accepted). Fill-in is not amendment.

**Browser consume (accepted):** not the POST client unless that Browser posted. It **Polls**. Poll's Change list is the tail since its revision — same kind as a success ACK. If that Browser is optimistic on the subgraph: **rewind**, then replay the list. A Client that already applied the others needs only the amended job Change(s).

Load packages stay Graph transfer (parked). They are how a Browser gets Nodes it never had, not how the job concludes.

## Server path (as far as the docs go)

1. Job works on a Local Subgraph (Server has the full Graph available for fill-in).
2. Job concludes with one Change or several. Each, on apply, is newest in turn.
3. Server Merges each against whatever has landed. Soft-lock (advisory checkout of this job's subtree) does **not** stop Merge ([[soft-lock.md]]).

In-process apply vs HTTP POST `/changes`: see [[in-process-apply.md]]. **Recommend** the same inner apply (mailbox `applyBatch` + persist + log), not POST-to-self, not a new HTTP API. Parse already skips HTTP but still encodes JSON into `postGraphOnlyChange` and ignores the ACK. Not locked.

## Client path (as far as the docs go)

Resident Browser: **Poll** (own path) when the POST queue is empty — undo to baseline + apply the Change list. POST ACK is an external-changes **signal**, not that list. Job results arrive as Poll. Today's Poll-with-tail clear is software debt. "Poll = empty POST" **superseded** ([[pipelined-post.md]]).

Unloaded children in the job's subgraph: Poll still sends Ops; today's apply can skip Unloaded Replace. What the job must emit vs what the Browser must Load is **not** decided (Server-partial / Q5 parked).

## Gaps (sharp)

1. **Entry:** don't-care — objects or Parse-style JSON ([[in-process-apply.md]]).
2. **Packaging:** one Change vs a set; if a set, one shared common prior from job start vs each Change planned after the previous apply.
3. **Soft-lock:** meaning accepted. Issuance / expiry / chrome still proposed.
4. **Residency:** Unloaded / Load packages vs job Ops — parked.

## WORK.md mutations

Add this file to the Active [[project.md]] related list. Do not lock. No `add` / `move` / `block` / `remove` of a work item.
