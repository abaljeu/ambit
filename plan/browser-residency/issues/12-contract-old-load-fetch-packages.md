# 12 — Contract old Load Fetch packages

**Type:** coding
**Status:** coded
**Actual:** 35m
**Blocked by:** [08 — Migrate Shared wire](08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](09-migrate-server-sync-doors.md), [10 — Migrate Browser Poll, post-Event, and Boot](10-migrate-browser-poll-post-event-and-boot.md), [11 — Migrate Bullet and Included readers](11-migrate-bullet-included-and-bootstrap-wants.md)

## Context

After migration, Poll, post-Event, bootstrap, and explicit Load Fetch use edges plus Nodes. [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) affirms that no legacy package API remains. This ticket removes the old types and helpers after every caller has migrated.

## What to build

Remove the legacy Load Fetch package API after every migration ticket is done. Load still runs Upload and Parse, and Fetch uses the current edges-plus-Nodes answer. Auto wants stay on Poll and post-Event.

### 1. Load command

Module [Load command](../arch.md). Story path 31.

- [x] 31.4 Delete package response — remove `LoadResponse.packages` and `packageChildMap`
- [x] 13.2.2 Keep current Fetch — `tryStartLoadFetch` uses the edges-plus-Nodes answer only
- [x] 30.1 Keep Load command — the user-facing Load command still runs Upload, Parse, and Fetch
- [x] 18.1 Keep optional click wiring — do not require un-wiring hollow-circle → Load

### 2. Sync wire and Server doors

Modules [Sync wire](../arch.md), [Server Sync doors](../arch.md).

- [x] 3.2.3 Delete legacy projection helpers — remove `packagesForTarget`, `packagesForTargets`, and `installPackages`
- [x] 6.2.3 Keep current Server answer — `postLoad` uses the same edges-plus-Nodes answer as Poll
- [x] 9.2.1 Keep one apply path — `applySyncResponse` applies Events, then `installWantAnswer`

### 3. Contract proof

Prove no legacy symbol or caller remains.

- [x] 31.4 Symbol scan — no `LoadResponse.packages`, `packageChildMap`, `packagesForTarget`, `packagesForTargets`, or `installPackages`
- [x] 9.2.2 Residency proof — Poll, post-Event, bootstrap, and Load Fetch all install through the current answer path

## See also

[06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md), [Browser residency architecture](../arch.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Contract. Decision home is [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md).
- 2026-09-26: Status changed to `defined`; linked Blocked-by tickets carry dependency order.
- 2026-09-26: Decision 06 affirmed removal; no dual-run or compatibility API remains.
- 1. **Independent review fix** — 2026-09-27: Removed the remaining singular package projection and retargeted its tests to the current Want answer.

## 4. Time

- 1. **Contract implementation** — 2026-09-27 25m — removed the old Load Fetch package API and verified the current answer path.
- 2. **Independent review fixes** — 2026-09-27 10m — removed the singular package helper, retargeted tests, and verified the current answer path.
