# 08 — Migrate Shared wire

**Type:** coding
**Status:** blocked
**Blocked by:** [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

Shared encode, decode, and Sync apply still treat Poll and post-Event as Changes only, then optional Load `packages`. After expand, the new fields exist beside the old. This batch moves Shared encode/decode and SyncLogic onto the Want answer while `packages` still work.

## What to build

Poll and post-Event Shared codecs carry Want plus edges and Nodes. After the Event tail, SyncLogic installs the Want answer. Load Fetch `packages` still apply. CI stays green because the old form remains.

### 1. Sync wire

Module [Sync wire](../arch.md).

1. [ ] Encode Want on Poll and post-Event — 5.2.1: encode and decode without dropping Changes
2. [ ] Empty Want allowed — encoding follows [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md)

### 2. SyncLogic

Module [SyncLogic](../arch.md). Story path 9.

1. [ ] Install after Events — 9.3: apply the Event tail, then installWantAnswer
2. [ ] Dual-run packages — 9.3.3: `loadResponseToSync` still installs `packages`
3. [ ] Outcome stamps unchanged — `getPollOutcome` still keys on `apiVersion` and event id

## See also

[Browser residency architecture](../arch.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
