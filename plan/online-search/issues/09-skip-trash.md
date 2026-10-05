# 09 — Skip trash

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

The person runs an ordinary Find. Deleted Nodes sit under trash. Story path **Skip trash** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

An ordinary search skips trash. The walk includes trash only when the search starts at the trash node. That start is [10 — Start at the trash node](10-start-at-the-trash-node.md).

### 1. Search Actor

Trash state stays on [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**.

1. [ ] Ordinary search skips trash — Search skips trash unless it starts at the trash node.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
