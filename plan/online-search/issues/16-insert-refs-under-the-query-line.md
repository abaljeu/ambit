# 16 — Insert Refs under the query line

**Status:** `defined`
**Type:** coding
**Blocked by:** [11 — Server evaluates](11-server-evaluates.md)

## Context

A person has run a query line. The Query Actor has evaluated it on the server in [11 — Server evaluates](11-server-evaluates.md). The person wants those results under the query line. Story path **Insert Refs under the query line** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Results under the query line are Refs. The line does not own them. The Query Actor posts that child list on the event source.

### 1. Query Actor

The Ref post stays a proposed design on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**, Interface and Uses. This ticket does not add a doc/current home for that post. Eval stays the remote evaluation from [11 — Server evaluates](11-server-evaluates.md).

1. [ ] Refs — Results under the query line are Refs, not Owned Children.
2. [ ] Existing Ref shape — [ExprRun](src/Shared/ExprRun.fs) `run` already materialises a Node answer with `ChildNode.reference`. The Query Actor uses that shape. The evaluation itself is the server evaluation.
3. [ ] Post the Replace — The Query Actor posts a Change on the event source whose child list uses that Ref shape under the query line.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 6 **Remote query eval**
