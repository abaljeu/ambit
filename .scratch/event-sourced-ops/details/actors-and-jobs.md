# Actors and long-running jobs

How a long-running Server-side Actor fits the framework, what already fits, and what does not exist yet.

Status: **assessment**, not a lock. The merge rules that apply once a Change arrives are accepted. How a job enters apply, how it is packaged, and residency are **not** decided.

## The fit, in one line

Once a Change **arrives** at Server apply, every existing rule decides the merge and the way a Browser consumes it. Everything before arrival — launch, identity, cancel, packaging — is undecided.

## What is already determined

A concluding Change is the **newest** Actor. The **other Actors** are whoever landed first: Browsers that edited during the long run, other jobs, Parse. Server arrival sequences them.

**Produce.** Common prior — typically the subgraph snapshot at plan time — then the other accepted Changes, then this job's Change amended. Recoverable overlap is merge success. Auth and malformed requests stay Reject.

**Complete.** If the job's Local Subgraph cannot name a promote-to-Owned Op, the Server completes **that** Change ([[completing-ops.md]]).

**Consume.** A Browser is not the posting Client unless it posted. It **polls**. The poll list is the tail since its revision. If that Browser is optimistic on the subgraph, it rewinds and replays. A Browser that already applied the others needs only the amended job Change.

Load packages stay a Graph transfer. They are how a Browser gets Nodes it never had, not how a job concludes.

## The three stages of a job

**1. Launch.** A job runs as its own task, off the apply queue. The launch request returns after the spawn; it does not sit on the queue. The Client keeps a job identity, and the Server maps that identity to a cancellation source and the task.

**2. Finish and apply.** The job builds Change objects and then sends a **message** into the apply queue. The queue picks it up and applies it, one at a time. The finishing task does not return to the original request, which is long since complete. The requesting Browser **polls**; there is no completion push.

**3. Cancel.** Cancelling a job cancels its token. The job must not send an apply message once cancelled. If the apply message is already in the queue, it runs — there is no cancel-after-enqueue.

None of this exists as a product. There is no multi-job launcher, no job identity, and no cancel interface.

## How a job should enter apply

**Recommended, not locked:** the same inner apply the request path uses — take already-built Changes, run apply, persist, and log as the newest Actor, and return the produced sequence. The request path decodes its body and then calls the same thing.

Do **not** post a request to the Server's own interface. That re-enters authentication, encoding, and acknowledgement handling for a caller that already holds Change objects. Do not add a new public interface for it.

The user's answer on this was **don't-care**: passing objects, or encoding to text the way Parse does today, are both acceptable. The clean seam is the recommendation above; today's encoding detour is a workable temporary fact.

## Parse File — the first Actor of this kind

**Already fits:** it produces a Change, not a graph dump; it plans **off** the apply queue and then sends a message for apply; it emits one Change of Ops; and the requester consumes by polling, not by a completion push.

**Must realign:** its apply compares and swaps on a revision, so Browser Changes that land between planning and apply cause a mismatch — the framework says amend and answer with success instead. Its acknowledgement is a bare success flag, not the produced sequence. Other optimistic Browsers must rewind and replay rather than rewrite their own fields.

**Not required now:** job identity, returning before apply, and cancel. Parse is still one request-scoped task.

## Shell command — a later Actor of the same kind

Prospective, and no such interface exists. Same shape as Parse: its own task off the apply queue, conclude with Changes, send them to inner apply, Browsers poll and replay.

**New in relation to Parse:** several concurrent jobs, Client-held job identities, and cancel. Whether output becomes node text or parsed Ops is unspecified.

## The sharp gaps

1. **Packaging.** One Change or a set. If a set: one shared common prior from job start, or each Change planned after the previous apply landed.
2. **Soft lock.** The meaning is accepted; issuance, expiry, and chrome are not ([[soft-lock.md]]).
3. **Residency.** What a job must emit against what a Browser must Load is parked. Poll still sends Ops for Unloaded parts, and today's apply can silently skip them.
4. **Entry.** Don't-care, as above.
