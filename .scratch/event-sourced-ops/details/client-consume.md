# Client correction — rewind and replay

How an optimistic Client converges on the Server's Local Graph. This is **consume**. Producing the sequence is a different job ([[merge-invariant.md]]).

Status: **accepted**.

## The rule

The optimistic Client must match the Server. The strategy is **rewind and replay**.

1. **Rewind to the baseline.** Undo the optimistic Local Graph back to the catch-up point — the last revision received from the Server, when that is the same thing. This is consume rewind, not the unrestricted Undo question in [[undo.md]].
2. **Replay the Change list.** Apply the Server-produced sequence in order: the other Actors' accepted Changes, then the newest amended Change, one Op at a time.

Do **not** apply the list on top of the optimistic, unamended Local Graph.

## Why not an in-place transform

The Client sits at text `C`. The Server sequence is `A→B` on the Node, then a `amb-conflict` child holding `C`. Rewinding to the common prior (`A`) and replaying gives that. Rewriting the local field in place — matching the local value as if it were first — puts `B` in the child instead and diverges. `SetText C→B` is not the strategy.

A later increment may prove that some in-place transform is identical to rewind and replay. Until that proof exists, it is not an equal alternative.

## This is not genesis replay

The base is the common prior this merge already shares, plus a short Server tail. It is not a replay from empty. The rejection of replay from genesis stands ([[relation-to-relaxed-concurrency.md]]).

A shorter Server list — only the amended newest Change plus completing Ops — is still this strategy, as long as that list is the whole sequence from the rewind base. It is not a second path.

## Where the list arrives

The list arrives on **Poll**, when the posting queue is empty. A post acknowledgement does not carry a tail; it signals that external Changes exist and lets the Client note the baseline. See [[messaging.md]].

## Leftover pending (accepted)

After rewind and replay, the Client may still hold pending Changes that were not in this post.

Those stay **as planned and unamended**. The next post sends them, and the Server amends them as the newest Actor at that time. The Client does not amend its own pending work, and does not drop it.

The reasoning: the outcome is the same, and it is cheaper. The Server is then the only amender — both of applied Changes and of leftover pending when it arrives.

Posts and polls carry only the **last revision received from the Server**, never a locally advanced number. A stale last-received revision against the Server's current one **is** the amendment case; it is not an error.

## What this does not decide

- Whether the Client replans remaining pending work onto the new base. It does not; see above.
- History behavior beyond "neither channel clears it" ([[undo.md]]).
