# 09 — Migrate Server Sync doors

**Type:** coding
**Status:** blocked
**Blocked by:** [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md)

## Context

The Server still answers Poll and post-Event with Changes only, and `/state` still scopes a complete ROOT Workspace. After expand, Shared can build a Want answer and a visible-closure Graph. This batch switches the Server doors. The Server Graph stays large. Load Fetch `packages` stay as the sole Load path until [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md). Do not add a production dual-run ([06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) done).

## What to build

`getPoll` and `postEvents` accept Want and return Changes plus edges plus Nodes. `getState` returns visible-closure. `postLoad` still returns `packages` as the sole Load path until [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md). App and Browser hit the same doors with the same Want.

### 1. Server Sync doors

Module [Server Sync doors](../arch.md). Story paths 1, 19, 20, 35.

1. [ ] Poll answers Want — 20.3: `getPoll` returns Changes plus edges plus Nodes
2. [ ] post-Event answers Want — 19.3: `postEvents` returns Changes plus edges plus Nodes
3. [ ] State is visible-closure — 1.3: `getState` uses bootstrapGraph, not the whole Server Graph
4. [ ] Server stays large — 35.1: Core Graph stays complete; visible-closure is the Browser answer only
5. [ ] Load packages remain until contract — 6.2.3: `postLoad` still returns `packages` as the sole Load path; not a production dual-run

### 2. Reserved bootstrap set

Module [Reserved ids](../arch.md). Story paths 4–7, 36.

1. [ ] ROOT, TRASH, Workspaces Node, SYSTEM Children — 4.2–7.2, 36.2: first paint includes those Children
2. [ ] SYSTEM spelling — 36.1: reserved Node SYSTEM stays SYSTEM

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Bootstrap set**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) locked no dual-run in product. This batch does not add a second production Load path.
