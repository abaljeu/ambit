# 11 — Server evaluates

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

A person runs a query line such as `= root descendants with name like "Bob"`. The Query Actor evaluates that line on the server. Story path **Server evaluates** is [Online search architecture](plan/online-search/arch.md) §1. [05 — Query fulfillment while eval stays local](05-query-fulfillment-while-eval-stays-local.md) stays in its current shape. This ticket waits on [07 — Server completes the picture](07-server-completes-the-picture.md) because both Actors live in the locked file `src/Server/SearchActor.fs`.

## What to build

The Query Actor evaluates the query once, when the line runs, then stops. A keypress does not start this Actor. Query keeps its own Actor. It does not use the Find globe.

### 1. Query Actor

State, Interface, and Uses for **Query Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 2. Eval is remote. The Ref post stays a proposed design for [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md).

1. [ ] Server Graph — The Query Actor evaluates the query on the server Graph once, when the line runs. The Actor then stops.
2. [ ] Not a keypress — A keypress does not start this Actor.
3. [ ] Own Actor — Query starts its own Actor in `src/Server/SearchActor.fs`. It does not use the Find and Move Actor or the Find globe.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 6 **Remote query eval**
