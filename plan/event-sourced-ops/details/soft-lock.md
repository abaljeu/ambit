# Soft lock

The advisory reservation a long-running Actor makes on its subtree.

Status: the **meaning** is **accepted**. **Lifecycle coupling to a job** is an **accepted direction** (quiz pin). Issuance, expiry, and screen chrome details stay **proposed**. Hard locking stays out of this standard.

## Meaning (accepted)

A long-running Server Actor — Parse File, a shell command, the same kind of thing — reserves the subtree it may Change.

That reservation is **advisory**. When the job completes there may be Changes to those Nodes, so it is **recommended to work elsewhere**. It is not a hard lock, and it is not illegal to work there.

Concurrent Browser edits on that subtree are **legal**. They are other accepted Changes. The job's concluding Change is amended as the newest one, and returns success. Do not block those edits and do not Reject them.

Merge runs exactly as it would without the lock. The soft lock changes no rule in [[merge-invariant.md]].

## Lifecycle with a job (accepted direction)

The reservation **belongs to a job**. Job completion clears the lock. The lock indicator is an **access point to the job** (inspect / cancel), not a second independent object. Product work should ship job identity and soft-lock as **one vertical**, not as two sibling features ([[actors-and-jobs.md]], [[../to-tickets-draft.md]]).

Parse realignment can prove the Actor produce path **without** inventing this surface first (request-scoped Parse needs no multi-job soft-lock chrome).

## Cancel is not Undo (accepted)

Cancel stops the job from generating further Changes. Changes that already merged stay merged. Undo names an already-merged Change and inverts it ([[undo.md]]).

## Still proposed

- Who issues a soft lock, and how it expires.
- The screen chrome beyond “work elsewhere” and “indicator opens the job”.
- Exact cancel control placement on that shared surface ([[actors-and-jobs.md]]).
