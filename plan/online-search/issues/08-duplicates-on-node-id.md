# 08 — Duplicates on Node id

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

The dialog already lists client hits. The server phase must list only the other hits. Story path **Duplicates on Node id** is [Online search architecture](plan/online-search/arch.md) §1. Result identity is Node id (`NodeId`). That lock is [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**. [04 — Duplicate hit identity](04-duplicate-hit-identity.md) stays in its current shape.

## What to build

A client hit and a server hit are the same hit when they share a Node id. The one request asks only for hits the client does not already have. The dialog lists each Node once.

### 1. Search Actor

Dedup state and the start message stay on [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**.

1. [ ] Node id — The server phase drops a hit whose Node id the client already showed.
2. [ ] Shown ids — The start message carries those Node ids. The Actor omits those ids.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**
