# Soft lock

The advisory reservation a long-running Actor makes on its subtree.

Status: the **meaning** is **accepted**. Issuance, expiry, and screen chrome are **proposed**. Hard locking stays out of this standard.

## Meaning (accepted)

A long-running Server Actor — Parse File, a shell command, the same kind of thing — reserves the subtree it may Change.

That reservation is **advisory**. When the job completes there may be Changes to those Nodes, so it is **recommended to work elsewhere**. It is not a hard lock, and it is not illegal to work there.

Concurrent Browser edits on that subtree are **legal**. They are other accepted Changes. The job's concluding Change is amended as the newest one, and returns success. Do not block those edits and do not Reject them.

Merge runs exactly as it would without the lock. The soft lock changes no rule in [[merge-invariant.md]].

## Cancel is not Undo (accepted)

Cancel stops the job from generating further Changes. Changes that already merged stay merged. Undo names an already-merged Change and inverts it ([[undo.md]]).

## Still proposed

- Who issues a soft lock, and how it expires.
- The screen chrome. The only decided part of the interface is the recommendation to work elsewhere.
- Whether the soft lock is also the surface a user cancels the job from ([[actors-and-jobs.md]]).
