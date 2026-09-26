# 07 — Expand Want and edges/Nodes package

**Type:** coding
**Status:** coded
**Estimate:** 3h
**Actual:** 2.5h

## Context

A person opens the Browser on a Graph that is already large on the Server. Today Poll and post-Event carry Changes only, and Load Fetch still returns complete Workspace `packages`. Silent residency needs a Want list and an edges-plus-Nodes answer beside those old forms so nothing breaks. Locked grill (authoritative while [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md) is still on an open PR): request JSON field `want` as a `NodeId` list on Poll and post-Event; always send `want`; empty is `[]`. Answer on `ChangeSuccessResponse`: `nodes` plus `childMap`. `ApiVersion.current` is 13. Load keeps `packages` / `packageChildMap`. App and Browser use the same wants. There is no throttle.

## What to build

Add Want.compose, installWantAnswer, visible-closure bootstrap, and additive wire types beside the old Poll, post-Event, and Load Fetch `packages`. Old callers keep compiling. Do not switch production doors yet.

### 1. Want

Module [Want](../arch.md). Story paths 13, 14, 15, 33.

1. [x] Compose Included first — 13.1: list Included Nodes that miss Children
2. [x] Compose those Children second — 14.1: list those Children next
3. [x] No third tier — 15.1: reserved Nodes and Zoom ancestors stay off the ongoing Want
4. [x] Honor Fold — 33.2: use Included, not a deep unfold
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
- 2026-09-26: Built expand beside old Poll / post-Event / Load doors. Followed the locked grill for `want`, `nodes`, `childMap`, and ApiVersion 13. Production App and Server doors stay on the old path ([08 — Migrate Shared wire](08-migrate-shared-wire.md) through [11 — Migrate Bullet, Included, and bootstrap wants](11-migrate-bullet-included-and-bootstrap-wants.md)).

## Time

- 2026-09-26 2.5h — Want.compose, installWantAnswer, visibleClosureGraph, ApiVersion 13 wire, Shared.Tests (from chat)
