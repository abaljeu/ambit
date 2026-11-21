# 16 — Insert Refs under the query line

**Status:** `defined`
**Type:** coding
**Blocked by:** [11 — Server evaluates](11-server-evaluates.md)

## Context

A person has run a query line. The Query Actor has evaluated it on the server in [11 — Server evaluates](11-server-evaluates.md). The person wants those results under the query line. Story path **Insert Refs under the query line** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Results under the query line are Refs. The line does not own them. [22 — Run sends = lines to Amble](22-run-sends-equals-lines-to-amble.md) chooses which line runs Amble. [25 — The server evaluates Run](25-server-evaluates-run.md) is where the plan runs. That plan returns existing Node ids and writes no children. This ticket inserts Refs under the query line.

### 1. Query Actor

The Ref post stays a proposed design on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**, Interface and Uses. This ticket does not add a doc/current home for that post. Eval stays the remote evaluation from [11 — Server evaluates](11-server-evaluates.md).

1. [ ] Refs — Results under the query line are Refs. The line does not own them.
2. [ ] Existing Ref shape — [ExprRun](src/Shared/ExprRun.fs) `run` materialises a Node answer with `ChildNode.reference`. This insert uses that shape. The server evaluation returns existing Node ids and writes no children.
3. [ ] Insert — Refs under the query line stay this ticket. The server evaluation writes no children and posts no ops.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 6 **Remote query eval**
