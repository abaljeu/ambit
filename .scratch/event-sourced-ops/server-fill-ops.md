# Server fill-in Ops (proposed)

As-implemented expectation for future Change merge, plus a framework pattern. Not locked. No algorithm.

## Pattern

An Actor with a Local Subgraph can delete an **Owned** edge and lack the Graph to name the promote-Ref-to-Owned Op. Semantics still want another edge to become Owned (which owner is not critical). The **Server** (full Local Graph / DB) is an Actor: it generates **additional Changes** and sends them (Poll).

Do not have the Browser send a reparent it cannot see.

## Facts

- When other occurrences are **Resident**, the Browser already plans promote-then-remove in **one** Change (`LocalDeleteWithPromotion` in [[src/Shared/ViewModelDeleteOps.fs]]). That Change is what `ClientHistory.record` stores — fill-in is on the stack because the Browser sent it.
- ACK may append **only** `SetUpdateTime` suffixes; those project onto the Graph and **do not** amend History.
- Poll/Load with a non-empty Change tail **clears** History (`applySyncResponse`).
- Startup [[.scratch/owner-edge-db-repair/]] promotes a Ref with **no** Change / no Poll.
- **As-practice (user):** Server fill-in is relayed onto the Browser undo stack. That fights Poll-clear and History-neutral ACK unless fill-in rides in the **same** recorded Change (or we change those paths).

## Open (timing)

After the delete merges, a no-owner but Ref-reachable Node is today's "defect." Legal until Server extras arrive, or Server must apply them in the **same** Change/transaction as the delete? Same transaction still looks like two steps on Poll unless one Change carries both Ops. Two partial Actors dropping different Owned edges: fill-in must not dual-Own.
