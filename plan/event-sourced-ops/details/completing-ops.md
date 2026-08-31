# Completing Ops — Server fill-in

What the Server adds when an Actor's view was too small to write the whole Change.

Status: the **timing** is **accepted**. The rest of the pattern is **proposed**. There is no algorithm here.

## The pattern

An Actor with a Local Subgraph can drop an **Owned** edge and still be unable to name the Op that promotes another edge to Owned, because it cannot see that edge. The semantics still want an owner, and which owner is not critical.

The **Server** has the full Graph. As an Actor itself, it completes **that Change** with the missing Ops.

Do not have the Browser send a reparent it cannot see.

## Timing (accepted)

The completing Ops land in the **same Change** as the delete. There is no legal window with no owner. One History entry covers both, so an Undo of that entry inverts the delete and the promotion together.

A later fill-in, arriving as a second Change on a poll, is **rejected**. Fill-in is not a second Change.

This matches practice: the user's expectation is that Server fill-in Ops arrive on the Browser undo stack. Same-Change fill-in does that. History-neutral stamp suffixes stay out of History.

## Fill-in is not amendment, and not rewind and replay

Three different jobs, often confused:

| Job | What it does | Whose Change |
| --- | --- | --- |
| **Completing Ops** | Adds missing Ops **inside one** Change | The Change being completed |
| **Amendment** | Rewrites the newest Actor's Ops after the other accepted Changes are in | The newest Change ([[merge-invariant.md]]) |
| **Rewind and replay** | How a Client consumes the produced sequence | Every Change in the list ([[client-consume.md]]) |

The completed Change rides in that sequence. A Client must receive both the other accepted Changes and this Change as completed — and, if it is the newest, as amended.

## Owner count

Delete plus promote in one Change is Move-shaped: the owner count stays at one. If a defective Change would raise it to two, the extra Owned edge becomes a Ref — the bug net in [[merge-invariant.md]].

Repair of pre-existing defects at startup stays a separate, no-Change path — [[plan/owner-edge-db-repair/]].
