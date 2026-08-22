# Open questions, parked topics, and what is not locked

An inventory. It is not a lock and it is not new work. The project stage stays `charting`.

## Accepted — do not re-ask

- The increment-1 vocabulary: Op, Change, Actor, Subgraph, Local Graph ([[vocabulary.md]]).
- Amendment order; Client rewind and replay; a node-local correction that omits the other Changes is invalid.
- Same text and same name become an `amb-conflict` Normal child. Success with a Change list, not a Reject.
- Child lists: full-list Replace by default ([[replace-amendment.md]]); a conflict is occurrence-bag Accept Both with minimal algorithm in [[replace-amendment.md]]; order polish in issue 10.
- Classes are a set delta. Owner count never rises from one to two. Fill-in **timing** is the same Change as the delete. DocumentState is removed.
- **Two paths.** A post acknowledgement is an external-changes signal and a baseline note; a queue-empty poll applies the Change list. "Poll is an empty post" is **superseded**. Neither clears History; today's poll clear is debt.
- Leftover pending stays unamended; the next post sends it and the Server amends. Posts and polls carry only the last revision received from the Server.
- A recoverable kick-back is merge success. The older slice-2 Reject and replan is obsolete for that case. The remaining Reject is auth and malformed requests.
- The soft-lock **meaning**: an advisory subtree reservation; edits there are legal.
- **Soft-lock lifecycle couples to a job** (quiz pin): the reservation belongs to the job; job completion clears it; the lock indicator is an access point to the job. Issuance, expiry, and chrome details stay proposed.
- Cancel is not Undo. This project is a sibling of relaxed concurrency, not a replacement.
- **Load packages are Graph / state transfer** for Nodes and children the Client does not yet hold — not Ops replay, not genesis replay ([[decision-log.md]] Round 4; [[architecture.md]]).
- **One global Server arrival order / revision sequence.** Posts and polls carry the last revision received from the Server. Not per-Workspace revisions.
- **Shared success envelope type** for Post and Poll is the pinned direction (fewer concepts); the two **channels** stay distinct (Post signals; Poll carries the list) ([[messaging.md]]).

## Still proposed — not locked

- **The merge document as a whole** ([[merge-invariant.md]]). The order, the correction strategy, and several kinds are accepted; the invariant and any per-Op tables are not.
- **The conflict taxonomy** as a taxonomy ([[conflict-resolution.md]]). Text, name, children, and classes are already pinned inside it.
- **Kind 4, delete against edit** ([[conflict-resolution.md]]) — independence for critical information; removal exemption against the common prior ([[merge-invariant.md]]); tentative `deleted` wrapper recovery is future, not locked. Decide early relative to log/Change extension; implement only after accept ([[../to-tickets-draft.md]]).
- **The fill-in pattern** ([[completing-ops.md]]). The timing is accepted; the rest is proposed.
- **Exact envelope field set** beyond the shared-type direction ([[messaging.md]]).
- **Soft-lock issuance, expiry, and chrome** ([[soft-lock.md]]) — not the job-coupling pin above.
- **Job identity, launch, and cancel mechanics** ([[actors-and-jobs.md]]). None of it exists; product surface should ship with soft-lock, not as a second orphan concept.
- **Actor packaging and residency detail** — one Change or a set, and what a job emits against what a Browser must Load (Load itself stays Graph transfer).
- **Parse File realignment.** An observation, not a plan.
- **Shell command.** A later Actor, with no product behind it.
- **Client replan before POST (optional future UX).** Replan leftover pending against the graph after learning of external Changes, instead of sending unamended Ops for Server amend. Deferred: duplicates amendment logic for smoother optimistic display only ([[client-consume.md]]).

## Open and retained — not parked

**Is unrestricted Undo desirable?** The global order makes it possible. Whether Actors can see and understand those edits well enough to choose Undo properly is unanswered, on purpose ([[undo.md]]).

**Delete-against-edit recovery (future consideration).** Tentative choice: if the transitive owner of a Change'd Node is nothing or TRASH, recover with a `deleted`-labeled conflict wrapper at the old parent that Owns the Node ([[conflict-resolution.md]] Kind 4). Issues left open: a Change does not carry ownership; correcting when the Change precedes the delete is awkward. Sketched how: the Change's baseline, plus a scan of history since that baseline. No algorithm yet.

**What resolves a hard Orphaning against a critical edit?** [[merge-invariant.md]] names the conflict and no outcome. Whether a well-formed Change can produce one at all is part of the question, since fill-in makes every delete Move-shaped ([[completing-ops.md]]). A less aggressive orphan collection is a proposed safety belt only.

## Parked — deliberately not discussed now

- **Two deferred questions on the state endpoint.** Whether a producer must refuse to propose an Op-invalid Graph, or whether transfer fails closed; and what a partial view may believe. The user paused this topic. (Load-as-Graph-transfer and global revision are **not** parked — see Accepted.)
- **A Server-partial Local Graph** as a designed Server mode.
- **Action against Change as a framing.** No stake either way ([[vocabulary.md]]).

## Not this project

- The first slice of [[.scratch/relaxed-concurrency/]] — drop the global revision gate — is still pending there.
- That project's later slices are blocked, superseded for recoverable kick-back ([[relation-to-relaxed-concurrency.md]]).
