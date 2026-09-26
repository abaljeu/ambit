# 08 — Migrate Shared wire

**Type:** coding
**Status:** blocked
**Blocked by:** [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

Shared encode, decode, and Sync apply still treat Poll and post-Event as Changes only, then optional Load `packages`. After expand, the new fields exist beside the old. This batch moves Shared encode/decode and SyncLogic onto the Want answer. Old `packages` may remain as the sole Load path until [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md). Do not add a production dual-run ([06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) done).

## What to build

Poll and post-Event Shared codecs carry Want plus edges and Nodes. After the Event tail, SyncLogic installs the Want answer. Load Fetch `packages` may still apply as the sole Load path until [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md). CI stays green because the old form remains. Do not add a second production Load path.

### 1. Sync wire

Module [Sync wire](../arch.md).

1. [ ] Encode Want on Poll and post-Event — 5.2.1: encode and decode without dropping Changes
2. [ ] Empty Want allowed — encoding follows [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md)

### 2. SyncLogic

Module [SyncLogic](../arch.md). Story path 9.

1. [ ] Install after Events — 9.3: apply the Event tail, then installWantAnswer
2. [ ] Old Load packages remain until contract — 9.3.3: `loadResponseToSync` still installs `packages` as the sole Load path; not a production dual-run
3. [ ] Outcome stamps unchanged — `getPollOutcome` still keys on `apiVersion` and event id

## See also

[Browser residency architecture](../arch.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) locked no dual-run in product. This batch does not add a second production Load path.
