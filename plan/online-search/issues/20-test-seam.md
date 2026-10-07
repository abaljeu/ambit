# 20 — Test seam

**Status:** `defined`
**Type:** coding
**Blocked by:** [19 — Shared segments](19-shared-segments.md)

## Context

The shared Find and Move result and the query Ref Replace are in place from [19 — Shared segments](19-shared-segments.md). A test needs one narrow place to prove them. Story path **Test seam** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Tests share one result of at most 200 Node ids. Find and Move tests install that result through the Want install door. Query tests also expect the Ref Replace under the query line, and a function stop under 200.

### 1. Want nodes for hits

The Find and Move install is a lock on [Online search architecture](plan/online-search/arch.md) §1 item 14 **Test seam** and §2 item 3 **Want nodes for hits**. [installWantAnswer](src/Shared/ResidentProjection.fs) is the install door. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

1. [ ] One result of Node ids — The narrowest shared point is one result of at most 200 Node ids. Find and Move tests install it with [installWantAnswer](src/Shared/ResidentProjection.fs). That install is a lock.

### 2. Query Actor

The Ref Replace stays the proposed design on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**. Identity of a hit is Node id (`NodeId`).

1. [ ] Query proof — Query tests also expect the Ref Replace under the query line, and a function stop under 200.

## See also

[Online search spec](plan/online-search/spec.md), [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**
