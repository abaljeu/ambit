# 17 — Server items inserted

**Status:** `defined`
**Type:** coding
**Blocked by:** [14 — Cap of 200](14-cap-of-200.md), [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md)

## Context

The server has found N items for a query. Refs under the query line are [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md). The query stop is [14 — Cap of 200](14-cap-of-200.md) section 2 **Query cap**, after [11 — Server evaluates](11-server-evaluates.md). Story path **Server items inserted** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

The query expression inserts those N items as Refs under the query line. N is at most 200, and lower when the function stops under 200. A Ref points at a Node the Browser has.

### 1. Query Actor

The insert stays on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**. The Ref post stays a proposed design.

1. [ ] Insert N — The query expression inserts the N items the server finds, as Refs under the query line. N is at most 200, and lower when the function stops under 200.

### 2. Want nodes for hits

Find hit Headers on the Want answer `nodes` list are a lock on [15 — Dialog shows server hits](15-dialog-shows-server-hits.md). This section is the query coupling. The Query Ref post stays a proposed design on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**.

1. [ ] Nodes then Refs — The same Poll carries the Want `nodes` for those ids and the Change that inserts the Refs, so a Ref points at a Node the Browser has. That coupling stays a proposed design.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 2 **No limit syntax**
