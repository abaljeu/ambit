# Decision log

How each accepted point was reached, and what was superseded on the way. This replaces the round-by-round grill files. The rules themselves live in the other detail documents.

The method was a design tree: ask the whole frontier each round, number the questions, give a recommended answer, then wait. A question that depends on an open answer belongs to a later round.

## Round 1 — what "event sourcing" means this time

The older map had already examined Event Modeling and Event Sourcing and rejected **replay from genesis**. The question was which proposal this project is.

**Answered:** keep the snapshot and the tail. Every mutation that matters — Parse, reparse, residency, background work — arrives as Changes and Ops that the Browser already applies. Success is a **semantic foundation**, not a replacement of the state endpoint by a Change stream. Genesis replay is not reopened.

## Round 2 — a wider goal is not a wider implementation

**Q2, answered A.** Incomplete use cases justify a wider **goal**. They do not reopen the log as the source of truth.

**Q3, answered C with a rider.** No new refusal at install time, plus a producer duty: do not propose an Op-invalid Graph. The standard should be written now; a fuller system comes later.

## Round 3 — the producer gate, and partial views

**Q4** asked whether the producer duty is an honour system or a closed door on transfer. **Q5** asked what a partial view may believe: a resident subset, a lagging prefix, or an in-flight mix.

**Not answered.** The user stopped the topic — "enough about the state endpoint". Both questions are **parked**, not dropped ([[open-questions.md]]).

## Round 4 — residency conveyance

**Q6, answered A.** Load stays mixed: the Change tail as Ops, the packages as a Graph transfer. Poll is already the Op path. Load exists because a partial view cannot replay what it never stored. Do not dress packages as Ops.

## Round 5 — three frontier questions

**Q1, leftover pending — answered, then reversed.** The first answer was that the Client amends leftover pending locally. Superseded in round 6.

**Q2, how a Server-side Actor enters apply — don't-care.** Objects into the inner apply, or the existing encode-and-send detour, are both acceptable. Do not lock either.

**Q3, whose Change Undo may invert — answered C.** Leave the desirability of unrestricted Undo open. Do not pin this increment's Undo to own-History-only.

## Round 6 — the Server is the only amender

The user reversed round 5's Q1: **send leftover pending unamended**. The Server amends it as the newest Actor when it arrives. The reason is simplicity, and the belief that the outcome does not change. The Server then amends both applied Changes and leftover pending.

**Narrow pin on revision:** posts and polls carry only the last revision the Client **received from the Server**, never a locally advanced number. A stale value against the Server's current one is exactly the amendment case. **Quiz follow-up:** one **global** Server arrival order / revision sequence is **accepted** (not per-Workspace). Exact token encoding may still be refined; the global sequence itself is not parked.

## Round 7 — pipelined posts

The clash: many posts in flight, each carrying the same last-received revision, would make every acknowledgement repeat the same tail; and the Server does not track Clients.

**Pinned by the user:** the acknowledgement **informs** that external Changes exist; the Client **notes the baseline**; when the posting queue is empty the Client **polls** from that baseline, rewinds to it, and applies the list. Not wait-for-one. Not Client tracking. Not one batched request. This is the origin of the two-path split ([[messaging.md]]).

## Round 8 — acknowledgement payload

**Answered A.** A flag is enough. The catch-up point is the last revision already received, so a second number on every pipelined acknowledgement is unnecessary until something needs it.

## The amendment-order correction

The most important correction in the project, and it did not come from a numbered round.

The documents said "common prior, sequence, adjust the second", but the language about amendment had drifted into **node-local corrections**: the Server rewriting Ops so they are true of the Nodes in question, and emitting only that rewrite. The `SetText C→B` example — a one-field transform for an optimistic Client — had been generalised into the strategy.

That reading drops every other-Actor Op that does not rewrite the same field or span. The corrected rule is the amendment order in [[merge-invariant.md]], and the corrected Client behavior is rewind and replay in [[client-consume.md]].

## Points accepted outside the numbered rounds

- **Same text.** First arrival stays on the Node; the loser becomes an `amb-conflict` first child. The optimistic Client rewinds and replays rather than rewriting its own field.
- **Name.** The same family, and merge success rather than a Reject.
- **Child lists.** Positional Replace by default; on conflict, an occurrence-bag Accept Both against the common prior; the algorithm is later. The bag holds occurrences, not Node ids.
- **Classes.** A set delta against the common prior.
- **Owner count.** A well-formed Change never raises it from one to two; the extra Owned edge becoming a Ref is a bug net, not a merge case.
- **DocumentState.** Removed from the standard; both states are inferred.
- **Fill-in timing.** The completing Ops ride in the same Change as the delete, which matches the practice of fill-in appearing on the Browser undo stack.
- **Soft lock.** The meaning is an advisory subtree reservation; edits there stay legal.
- **Recoverable kick-back.** Merge success with a Change list, which obsoleted the older Reject-and-replan slice for that case.

## Superseded, and why

| Superseded claim | Replaced by |
| --- | --- |
| Poll is a post with an empty Change list; one handler, one envelope | Two separate paths: the acknowledgement signals, the poll applies ([[messaging.md]]) |
| The Client amends leftover pending before the next post | Send it unamended; the Server amends ([[client-consume.md]]) |
| The Server rewrites the newest Ops to match the touched Nodes, and emits only that | Amendment order: common prior, other accepted Changes, then the amended newest ([[merge-invariant.md]]) |
| `SetText C→B` as the general Client correction | Rewind to the baseline and replay the sequence ([[client-consume.md]]) |
| A recoverable collision is a Reject that the Client replans | Merge success with a Change list ([[relation-to-relaxed-concurrency.md]]) |
| A name clash is a Reject | An `amb-conflict` child, and merge success ([[conflict-resolution.md]]) |
| Implemented span compare-and-swap Replace stands in for the child-list rule | Behavior to beat ([[as-implemented-facts.md]]); Actor contract is full-list Replace ([[replace-amendment.md]]) |

## Quiz pins — later sequence (to-tickets)

User answers while drafting the program ticket sequence ([[../to-tickets-draft.md]]):

- **Soft-lock and job are one surface.** The lock belongs to the job; completion clears it; the indicator opens the job. Prefer one vertical project, not soft-lock-before-job or two parallel products. Parse remains a tracer for the Actor produce path without that surface.
- **Shared Post/Poll success envelope type** is preferred for a smaller footprint / easier verification. Channels stay distinct (Post signals; Poll lists). Fold into Ticket 0 expand; do not leave type unify as late optional cleanup.
- **Decision-first is OK** for delete-against-edit / orphan / Undo, provided early tickets leave extension room (optional Change baseline, adjustable short-tail retention, History not frozen as own-posts-only) so late accepts do not force wire rework.
- **Load packages as Graph / state transfer** reaffirmed **accepted** (Round 4); remove stale “parked” wording that blurred transfer kind with unfinished residency packaging.
- **One global Server revision sequence** reaffirmed **accepted**; not per-Workspace.
- **No Client replan for pending.** Server-only amendment stays the integration point; leftover pending posts unamended. Client replan before POST is a possible future UX improvement only — extra complexity, not an equal alternative ([[client-consume.md]]).
- **Sync status during external-changes catch-up.** After a Post external-changes signal and before the queue-empty Poll replay completes, the sync status control shows that remote Changes are forthcoming ([[../issues/04-client-consumes-merge-success-without-reload.md]]).

## Round 9 — permanent global history (proposed)

**Problem (fact):** Recovery does not reliably load **DB projection + Change log** on restart. Open Browsers can see stale-client rejection (`DataOutdated`, `ServerRejected`) instead of merge catch-up. File-mode bootstrap may truncate the log; that path is separate from the proposed DB+log model.

**Proposed:** Persist the global Change log permanently. Startup loads current state from the DB projection as today. Genesis — state at the first log entry — is derivable by inverting the full retained sequence; not routine, not historic-parser replay. Post-protocol change still forces Browser reload (`CodeOutdated`).

**Store (proposed, recommended):** PostgreSQL table `changes` already implemented in [[src/Server/Database.fs]] (append-only; `getChangesAfterCheckpointRevision`). Keep or evolve that table — do not add a file log, Redis, or parallel store. User direction ("I think the log is best stored as a PG table") aligns with the as-implemented store; not yet an accepted pin beyond this proposal.

**Goal outcome:** New server process/version does not demand Client restart when protocol is unchanged; state is consistent with pre-reset. Old Clients accepted unless we explicitly code a fail point. Server-generated Browser code means only short-term transition states matter; transition coding = keep state and protocols consistent so the prior Client does not break.

**Not reopened:** log-as-truth Event Sourcing, routine genesis replay, Load as Ops replay.

Detail: [[permanent-history-and-genesis.md]]. Implementation: [[../issues/15-permanent-global-change-log.md]].


Worker slang about a "wipe" of the pending queue, and a rejected-pending concept as a merge topic, were removed from the project. They were not a design case. The remaining Reject is authentication, malformed requests, and similar request failures.
