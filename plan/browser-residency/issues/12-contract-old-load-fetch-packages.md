# 12 — Contract old Load Fetch packages

**Type:** coding
**Status:** blocked
**Blocked by:** [08 — Migrate Shared wire](08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md), [10 — Migrate Browser Poll, post-Event, and Boot](10-migrate-browser-poll-post-event-and-boot.md), [11 — Migrate Bullet, Included, and bootstrap wants](11-migrate-bullet-included-and-bootstrap-wants.md)

## Context

After expand and migrate, Poll, post-Event, and bootstrap use edges plus Nodes. [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) locked the cut: no dual-run in product; old `packages` / `packageChildMap` exist only for development if needed; this ticket swaps explicit Load to edges-plus-Nodes (`nodes` / `childMap`). Do not maintain both production paths.

## What to build

Swap the old Load Fetch `packages` path for the new edges-plus-Nodes package. After this cut, explicit Load uses `nodes` / `childMap`. Old `packages` / `packageChildMap` exist only for development if needed. Load may still run Upload and Parse. Auto wants stay on Poll and post-Event. Hollow-click Load may remain as the Load command; it must not need the old production `packages` type.

### 1. Load command

Module [Load command](../arch.md). Story path 31.

1. [ ] Swap packages — 31.4: Load Fetch uses the edges-plus-Nodes answer only; no dual-run in product
2. [ ] Load command remains — 30.1: the user-facing Load command stays
3. [ ] Hollow-click Load may remain — 18.1: do not require un-wiring the click; it must not call the deleted production `packages` path

### 2. Sync wire and Server doors

Modules [Sync wire](../arch.md), [Server Sync doors](../arch.md).

1. [ ] Drop production LoadResponse.packages — no production caller remains; old form only for development if needed
2. [ ] postLoad uses Want answer — same edges-plus-Nodes package as Poll
3. [ ] Production installPackages gone — ResidentProjection keeps installWantAnswer; old install may remain for development if needed

## See also

[06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md), [Browser residency architecture](../arch.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Contract. Decision home is [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md).
- 2026-09-26: [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) is done. This ticket is the swap: explicit Load uses `nodes` / `childMap` after the cut; old form is development-only if needed.
