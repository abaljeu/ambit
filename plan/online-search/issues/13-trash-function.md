# 13 — Trash function

**Status:** `defined`
**Type:** coding
**Blocked by:** [12 — Query skips trash](12-query-skips-trash.md)

## Context

A person wants a query to start from trash the way `root` starts from ROOT. Ordinary queries skip trash after [12 — Query skips trash](12-query-skips-trash.md). Story path **Trash function** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

The function is named `trash`. It reaches trash the way `root` reaches ROOT. The query can start there.

### 1. Query Actor

The `trash` reach stays on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**, State.

1. [ ] trash — The function is named `trash`. It works like `root` for reaching trash.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
