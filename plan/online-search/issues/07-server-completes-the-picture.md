# 07 — Server completes the picture

**Status:** `defined`
**Type:** coding
**Blocked by:** [06 — Residence hits first](06-residence-hits-first.md), [14 — Cap of 200](14-cap-of-200.md)

## Context

The person has stopped changing the search text. The dialog already shows the residence hits from [06 — Residence hits first](06-residence-hits-first.md). After a short quiet gap, one Start runs the shared Find and Move Search Actor. The gate on [14 — Cap of 200](14-cap-of-200.md) is section 1 **Find and Move**. That section stops the client search at 200 hits. Story path **Server completes the picture** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

One Start after the quiet gap runs the shared Find and Move Search Actor. The Actor uses the same client search algorithm. The reply holds up to 200 hits and then stops. The dialog keeps a reply only when that reply matches the text on screen.

### 1. Search Actor

State, Interface, and Uses for **Search Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 1. This ticket introduces the locked file `src/Server/SearchActor.fs`. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change. Say event source for that log.

1. [ ] Quiet gap — One Start fires after the search text has been unchanged for a short quiet gap. The gap has no millisecond value. A text change before the gap ends sends nothing. When the client already has 200 hits, the Start is not sent.
2. [ ] Actor — That Start runs the shared Find and Move Search Actor in `src/Server/SearchActor.fs`. Move uses this Actor. Query does not use this gap.
3. [ ] Shared algorithm — The Actor uses [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs). The client call supplies the residence Graph and the zoom root. The Actor call supplies the full server Graph, the focus node, and the zoom root. The walk is that same algorithm.
4. [ ] Up to 200 hits — The reply holds at most 200 Node ids, then the walk stops. That stop is the client algorithm from [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. Client hits plus this reply stop at 200.
5. [ ] One reply — The Actor posts that one reply and stops. There is no continuation cursor.
6. [ ] Stale reply — The dialog applies a reply only when it matches the current search text, or an equivalent generation of that text.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 3 **Actor file**
