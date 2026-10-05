# 12 — Query skips trash

**Status:** `defined`
**Type:** coding
**Blocked by:** [11 — Server evaluates](11-server-evaluates.md)

## Context

A person runs an ordinary query. Deleted Nodes sit under trash. The Query Actor already evaluates on the server from [11 — Server evaluates](11-server-evaluates.md). Story path **Query skips trash** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

An ordinary query skips trash. The results stay out of deleted Nodes. Reaching trash is [13 — Trash function](13-trash-function.md).

### 1. Query Actor

Trash state stays on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**.

1. [ ] Ordinary query skips trash — An ordinary query skips trash.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
