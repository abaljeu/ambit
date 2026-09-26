# 12 — Contract old Load Fetch packages

**Type:** coding
**Status:** defined
**Blocked by:** [08 — Migrate Shared wire](08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md), [10 — Migrate Browser Poll, post-Event, and Boot](10-migrate-browser-poll-post-event-and-boot.md), [11 — Migrate Bullet and Included readers](11-migrate-bullet-included-and-bootstrap-wants.md)

## Context

After migration, Poll, post-Event, bootstrap, and explicit Load Fetch use edges plus Nodes. [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) affirms that no legacy package API remains. This ticket removes the old types and helpers after every caller has migrated.

## What to build

Remove the legacy Load Fetch package API after every migration ticket is done. Load still runs Upload and Parse, and Fetch uses the current edges-plus-Nodes answer. Auto wants stay on Poll and post-Event.

### 1. Load command

Module [Load command](../arch.md). Story path 31.

1. [ ] Death of packages — 31.4: Load Fetch uses the edges-plus-Nodes answer only
2. [ ] Load command remains — 30.1: the user-facing Load command stays
3. [ ] Hollow-click Load may remain — 18.1: do not require un-wiring the click; it must not call the deleted `packages` path

### 2. Sync wire and Server doors

Modules [Sync wire](../arch.md), [Server Sync doors](../arch.md).

1. [ ] Drop LoadResponse.packages — no caller remains
2. [ ] postLoad uses Want answer — same edges-plus-Nodes package as Poll
3. [ ] installPackages gone — ResidentProjection keeps installWantAnswer only

## See also

[06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md), [Browser residency architecture](../arch.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Contract. Decision home is [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md).
- 2026-09-26: Status changed to `defined`; linked Blocked-by tickets carry dependency order.
- 2026-09-26: Decision 06 affirmed removal; no dual-run or compatibility API remains.
