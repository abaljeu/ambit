# Server fill-in Ops (proposed)

As-implemented expectation for future Change merge, plus a framework pattern. Timing **accepted**. Dual-Own is not a merge case (owner-count invariant). No algorithm.

## Pattern

An Actor with a Local Subgraph can delete an **Owned** edge and lack the Graph to name the promote-Ref-to-Owned Op. Semantics still want another edge to become Owned (which owner is not critical). The **Server** (full Local Graph / DB) is an Actor: it completes **that Change** with the missing Ops. One History entry. Poll may relay that Change; not a later fill-in Change.

Fill-in is **not** amendment and **not** rewind+replay. Completing Ops add missing Ops inside **one** Change. Amendment rewrites the **newest** Actor's Ops after other Actors' accepted Changes are in the Local Graph ([[merge.md#Amendment order]]). Rewind+replay is how the Client **consumes** the Server sequence ([[merge.md#Client correction]]). The completed Change rides in that sequence (unified POST-ACK / Poll **proposed** — [[unified-messaging.md]]). The Client must receive both: other accepted Changes, and this Change as completed (and, if it is newest, as amended).

Do not have the Browser send a reparent it cannot see.

## Locked (timing)

Completing Ops land in the **same Change** as the delete. No legal no-owner window. Undo of that entry inverts delete+promote. Later Poll fill-in is rejected (fill-in is not a second Change). Poll History-clear is **resolved** against — neither POST nor Poll clears ([[unified-messaging.md]]); today's Poll clear is debt.

Startup [[.scratch/owner-edge-db-repair/]] remains a no-Change path for existing defects.

## Facts

- When other occurrences are **Resident**, the Browser already plans promote-then-remove in **one** Change (`LocalDeleteWithPromotion` in [[src/Shared/ViewModelDeleteOps.fs]]). That Change is what `ClientHistory.record` stores — fill-in is on the stack because the Browser sent it.
- ACK may append **only** `SetUpdateTime` suffixes; those project onto the Graph and **do not** amend History.
- Poll/Load with a non-empty Change tail **clears** History (`applySyncResponse`).
- Startup [[.scratch/owner-edge-db-repair/]] promotes a Ref with **no** Change / no Poll.
- **As-practice (user):** Server fill-in is relayed onto the Browser undo stack. Same-Change fill-in matches that; History-neutral ACK suffixes (`SetUpdateTime` only) stay out of History.

Delete+promote in one Change is Move-shaped: owner count stays 1. If a buggy Change would 1→2, extra Owned → Ref ([[merge.md]]).
