# 05 — Expand-contract first aspect

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

Which aspect of the system should be migrated first under expand-and-contract, one aspect at a time, so each slice is independently shippable and safely contracted before the next begins?

Keep the four provisional Destination aims and their gaps. This ticket only locks sequencing of aspects. It does not lock or rewrite the aims themselves.

Grill:

1. **Candidate aspects** — Name the distinct remake aspects (at least: Graph handoff / drop extract; Actor duplex / Core door; mailbox orchestration / FS+DB workers; start surface / curried function vs registry). May add others only if sharp.
2. **First slice** — Which one aspect migrates first, and why it is independently shippable.
3. **Expand then contract** — For that first aspect, what expands alongside the old path, and what gets contracted only after the new path is proven.
4. **Order after first** — Provisional order of the remaining aspects (still fog until later grills if needed).
5. **Pilot Actor** — Which existing Actor (for example TestActor) proves each slice without rewriting all Actors.

Do not implement.

## Comments

- 2026-09-27 — Filed as next-pass sequencing after the first grillset. Status `defined`.
