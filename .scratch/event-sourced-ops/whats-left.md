# What's left (increment-2 charting)

Inventory. Not a lock. Not new work. Stage still `charting`.

## Accepted

- Increment-1 vocab (Op, Change, Actor, Subgraph, Local Graph).
- Amendment order; Client rewind+replay; node-local omit-others invalid.
- Same-text / Name → `amb-conflict` Normal child; 200 Merge, not Reject.
- Children: positional Replace; conflict = bag Accept Both (algorithm later).
- Classes set delta; owner count; fill-in **timing** (same Change as delete); DocumentState deleted.
- **Two paths (accepted):** POST ACK = external-changes **signal** + note **baseline**; queue-empty **Poll** applies the Change list (undo to baseline + replay). **"Poll = empty POST" superseded.** Neither clears History (today's Poll clear is debt). [[pipelined-post.md]]
- Leftover pending: stay **unamended**; next POST sends them; Server amends. Queue-empty then Poll catch-up. POST/Poll carry last-received Server Revision only.
- Recoverable kick-back = 200 Merge; slice 2 Reject+replan obsolete for that; remaining Reject = auth / malformed.
- Soft-lock **meaning**: advisory subtree checkout; edits there legal.
- Cancel ≠ Undo. Sibling of relaxed-concurrency, not a replacement.

## Still proposed / not locked

- **[[merge.md]] as a whole** — still titled proposed. Order / correction / several kinds accepted; invariant + per-Op tables not locked.
- **[[conflict-kinds.md]]** as a taxonomy (text / name / children / classes already pinned inside).
- **Fill-in pattern** ([[server-fill-ops.md]]) — timing accepted; rest proposed.
- [[unified-messaging.md]] unification **superseded** (was "POST and Poll identical"). Exact type fields; ACK payload (flag vs baseline Revision).
- Soft-lock issuance / expiry / chrome / cancel-surface.
- Inner-apply seam: **don't-care** (objects or Parse-style JSON). Job id / launch / cancel API (does not exist).
- Parse File realignment (CAS / `{"ok":true}`) — observation, not a plan ([[parse-file-actor.md]]).
- Shell command — later Actor, no product ([[shell-command-actor.md]]).
- Actor packaging / residency ([[server-side-actor.md]]).

## Retained unanswered

- Unrestricted Undo desirability ([[undo.md#Unrestricted Undo desirability]]). Possible; see-and-understand not answered.

## Not this project

- [[.scratch/relaxed-concurrency/spec.md]] slice 1 still **Pending**. Map slices 2–3 **Blocked** (superseded for recoverable kick-back).
- Parked: Load packages / `/state`, Revision, Q4–Q5, Server-partial.

## WORK.md

Add this file to the Active [[project.md]] related list.
