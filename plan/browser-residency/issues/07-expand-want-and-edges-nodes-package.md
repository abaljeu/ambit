# 07 — Expand Want and edges/Nodes package

**Type:** coding
**Status:** done
**Blocked by:** None
**Estimate:** 3h
**Actual:** 2.5h

## Context

A person opens the Browser on a Graph that is already large on the Server. Poll and post-Event carried Changes only, and Load Fetch still returns complete Workspace `packages`. This expand slice added the Shared Want and edges-plus-Nodes shapes before production doors migrated. [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md) now owns the accepted current-version wire: request JSON field `want` as a `NodeId` list on Poll and post-Event; always send `want`; empty is `[]`. Answer on `ChangeSuccessResponse`: `nodes` plus `childMap`. `ApiVersion.current` is 13. Load keeps `packages` / `packageChildMap`. App and Browser use the same wants. There is no throttle.

## What to build

Add Want.compose, installWantAnswer, visible-closure bootstrap, and additive wire types beside the old Poll, post-Event, and Load Fetch `packages`. Old callers keep compiling. Do not switch production doors yet.

### 1. Want

Module [Want](../arch.md). Story paths 13, 14, 15, 33.

1. [x] Compose Included first — 13.1: Unloaded Included Nodes, then two ranks of Unloaded Children
2. [x] Children and grandchildren — 14.1: one compose includes Unloaded Children and grandchildren, including under Fold
3. [x] No bootstrap tier — 15.1: reserved Nodes and the Zoom framing path stay off the ongoing Want
4. [x] Honor Fold — 33.2: use the Included walk, then two Children ranks, not a deep unfold
5. [x] No throttle — a few wants at a time; no batching or backpressure

### 2. ResidentProjection

Module [ResidentProjection](../arch.md). Story paths 8, 9, 21–25. Narrowest test seam.

1. [x] installWantAnswer — 21–23: install edges and pointed-at Nodes separately; refuse dangling edges
2. [x] Absent key stays Unloaded — 24.2: do not insert an empty `childMap` key unless the answer sent `[]`
3. [x] Present key is Loaded — 25.2: `[]` marks a true leaf Loaded
4. [x] bootstrapGraph beside old — 8.1: add visible-closure bootstrap beside complete-Workspace `rootBootstrapGraph`
5. [x] Same package shape — 9.1: bootstrap and later Wants use edges plus Nodes

### 3. Sync wire

Module [Sync wire](../arch.md). Map Decisions **ApiVersion bump**.

1. [x] Additive fields — 5.1.3: Want-answer `nodes` and `childMap` on `ChangeSuccessResponse`; request `want` on `PollRequest` / `ChangeRequest` / `SyncWant`
2. [x] ApiVersion minor bump — `ApiVersion.current` is 13 (wire 1.3)
3. [x] Old Load packages remain — 5.1.2: `LoadResponse.packages` still compile

### 4. Shared.Tests

Narrowest shared test seam on [Browser residency architecture](../arch.md).

1. [x] Compose plus install — given Graph, SiteMap, Zoom, and an answer package, wanted parents are Loaded, Children are Resident, and no edge dangles
2. [x] App and Browser same wants — one compose function; no second App Want
3. [x] Old packages still install — `installPackages` still works beside installWantAnswer

## See also

[Browser residency architecture](../arch.md), [map.md](../map.md) Decisions so far

## Comments

- 2026-09-26: Filed via `/to-tickets`. Sequence expand-contract. Expand only.
- 2026-09-26: Built expand beside old Poll / post-Event / Load doors. Followed the locked grill for `want`, `nodes`, `childMap`, and ApiVersion 13. Production App and Server doors stayed on the old path for this completed slice ([08 — Migrate Shared wire](08-migrate-shared-wire.md) through [11 — Migrate Bullet and Included readers](11-migrate-bullet-included-and-bootstrap-wants.md)).
- 2026-09-26: [06 — Dual-run vs migrate explicit Load Fetch](06-dual-run-vs-migrate-explicit-load.md) later affirmed one current answer. The checked legacy-package items record this completed expand slice; [12 — Contract old Load Fetch packages](12-contract-old-load-fetch-packages.md) removes them from the destination.
- 2026-09-27: Want depth is two Children ranks. One compose lists Unloaded Included Nodes, then their Unloaded Children, then the Unloaded grandchildren, including under Fold.

## Time

- 2026-09-26 2.5h — Want.compose, installWantAnswer, visibleClosureGraph, ApiVersion 13 wire, Shared.Tests (from chat)
