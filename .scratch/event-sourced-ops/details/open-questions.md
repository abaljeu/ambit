# Open questions, parked topics, and what is not locked

An inventory. It is not a lock and it is not new work. The project stage stays `charting`.

## Accepted — do not re-ask

- The increment-1 vocabulary: Op, Change, Actor, Subgraph, Local Graph ([[vocabulary.md]]).
- Amendment order; Client rewind and replay; a node-local correction that omits the other Changes is invalid.
- Same text and same name become an `amb-conflict` Normal child. Success with a Change list, not a Reject.
- Child lists: positional Replace by default; a conflict is an occurrence-bag Accept Both. The algorithm is later.
- Classes are a set delta. Owner count never rises from one to two. Fill-in **timing** is the same Change as the delete. DocumentState is removed.
- **Two paths.** A post acknowledgement is an external-changes signal and a baseline note; a queue-empty poll applies the Change list. "Poll is an empty post" is **superseded**. Neither clears History; today's poll clear is debt.
- Leftover pending stays unamended; the next post sends it and the Server amends. Posts and polls carry only the last revision received from the Server.
- A recoverable kick-back is merge success. The older slice-2 Reject and replan is obsolete for that case. The remaining Reject is auth and malformed requests.
- The soft-lock **meaning**: an advisory subtree reservation; edits there are legal.
- Cancel is not Undo. This project is a sibling of relaxed concurrency, not a replacement.

## Still proposed — not locked

- **The merge document as a whole** ([[merge-invariant.md]]). The order, the correction strategy, and several kinds are accepted; the invariant and any per-Op tables are not.
- **The conflict taxonomy** as a taxonomy ([[conflict-resolution.md]]). Text, name, children, and classes are already pinned inside it.
- **Kind 4, delete against edit** ([[conflict-resolution.md]]) — independence for critical information; removal exemption against the common prior ([[merge-invariant.md]]); tentative `deleted` wrapper recovery is future, not locked.
- **The fill-in pattern** ([[completing-ops.md]]). The timing is accepted; the rest is proposed.
- **Exact message types and fields** ([[messaging.md]]).
- **Soft-lock issuance, expiry, chrome, and the cancel surface** ([[soft-lock.md]]).
- **Job identity, launch, and cancel** ([[actors-and-jobs.md]]). None of it exists.
- **Actor packaging and residency** — one Change or a set, and what a job emits against what a Browser must Load.
- **Parse File realignment.** An observation, not a plan.
- **Shell command.** A later Actor, with no product behind it.

## Open and retained — not parked

**Is unrestricted Undo desirable?** The global order makes it possible. Whether Actors can see and understand those edits well enough to choose Undo properly is unanswered, on purpose ([[undo.md]]).

**Delete-against-edit recovery (future consideration).** Tentative choice: if the transitive owner of a Change'd Node is nothing or TRASH, recover with a `deleted`-labeled conflict wrapper at the old parent that Owns the Node ([[conflict-resolution.md]] Kind 4). Issues left open: a Change does not carry ownership; correcting when the Change precedes the delete is awkward. Sketched how: the Change's baseline, plus a scan of history since that baseline. No algorithm yet.

**What resolves a hard Orphaning against a critical edit?** [[merge-invariant.md]] names the conflict and no outcome. Whether a well-formed Change can produce one at all is part of the question, since fill-in makes every delete Move-shaped ([[completing-ops.md]]). A less aggressive orphan collection is a proposed safety belt only.

## Parked — deliberately not discussed now

- **Changes against Graph transfer.** Load packages and the state endpoint.
- **Whether revision stays one global number.** One narrow pin only: posts and polls carry the last revision **received from the Server**.
- **Two deferred questions on the state endpoint.** Whether a producer must refuse to propose an Op-invalid Graph, or whether transfer fails closed; and what a partial view may believe. The user paused this topic.
- **A Server-partial Local Graph.**
- **Action against Change as a framing.** No stake either way ([[vocabulary.md]]).

## Not this project

- The first slice of [[.scratch/relaxed-concurrency/]] — drop the global revision gate — is still pending there.
- That project's later slices are blocked, superseded for recoverable kick-back ([[relation-to-relaxed-concurrency.md]]).
