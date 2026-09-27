# 11 — Migrate Bullet and Included readers

**Type:** coding
**Status:** coded
Actual: 30m
**Blocked by:** None — can start immediately

## Context

Bullet and Included must read `Graph.childMap` consistently so an Unloaded Node shows a hollow-circle Bullet until Children arrive and Fold bounds Included. Production bootstrap belongs to [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md), not this ticket.

## What to build

The hollow-circle Bullet follows absent `childMap` or Unparsed. Included walks `childMap` and honors Fold. Remove any remaining reader of the retired Node-children residency shape. No new per-Node loading Status. No new command that names Nodes.

### 1. Bullet

Module [Bullet](../arch.md). Story paths 10, 11, 12, 26, 34.

- [x] 11.2.2 Absent key is hollow Unloaded — indicator reads absent `childMap` as Unloaded
- [x] 11.2.1 Unparsed stays hollow — Unparsed keeps the same hollow-circle Bullet
- [x] 26.2 Children arrive — chevron or solid replaces hollow when Loaded and not Unparsed
- [x] 34.1 Unloaded is not empty — absent key never renders as a Loaded leaf
- [x] 12.2 No new loading Status — residency stays `childMap` plus Unparsed

### 2. Included

Module [Included](../arch.md). Story path 33.

- [x] 4.2.2 Walk childMap — `expand` walks `childMap`, not Node children
- [x] 33.1 Honor Fold — stop at folded children

### 3. Graph childMap constraints

Module [Graph childMap](../arch.md). Story path 32.

- [x] 1.2.1 One residency reader — Bullet and Included use `Graph.childMap`; no retired Node-children residency read remains in these paths
- [x] 32.1 No new named-Node command — silent wants are enough

### 4. Reader proof

Prove the UI distinction at the Shared view-model seams.

- [x] 10.2, 25.3, and 26.2 Indicator proof — absent, empty Loaded, populated Loaded, and Unparsed cases render the specified glyph
- [x] 33.1 Included proof — Fold bounds traversal while `childMap` supplies Children

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Hollow-circle Bullet**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Production bootstrap ownership moved to [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md); this ticket now owns only Bullet and Included readers.
- 2026-09-26: Republished via `/to-tickets`; completed expand 07 clears this ticket's live blockers.

## Time

- 2026-09-27 30m — migrated Bullet and Included residency readers and verified Shared seams (from chat)
