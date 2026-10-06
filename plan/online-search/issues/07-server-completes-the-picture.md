# 07 — Server completes the picture

**Status:** `coded`
**Type:** coding
**Actual:** 3h
**Blocked by:** [06 — Residence hits first](06-residence-hits-first.md), [14 — Cap of 200](14-cap-of-200.md)

## Context

The person has stopped changing the search text. The dialog already shows the residence hits from [06 — Residence hits first](06-residence-hits-first.md). After a short quiet gap, one Start runs the shared Find and Move Search Actor. The gate on [14 — Cap of 200](14-cap-of-200.md) is section 1 **Find and Move**. That section stops the client search at 200 hits. Story path **Server completes the picture** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

One Start after the quiet gap runs the shared Find and Move Search Actor. The Actor uses the same client search algorithm. The reply holds up to 200 hits and then stops. The dialog keeps a reply only when that reply matches the text on screen.

### 1. Search Actor

State, Interface, and Uses for **Search Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 1. This ticket introduces the locked file `src/Server/SearchActor.fs`. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change. Say event source for that log.

1. [x] Quiet gap — One Start fires after the search text has been unchanged for a short quiet gap. The gap has no millisecond value. A text change before the gap ends sends nothing. When the client already has 200 hits, the Start is not sent.
2. [x] Actor — That Start runs the shared Find and Move Search Actor in `src/Server/SearchActor.fs`. Move uses this Actor. Query does not use this gap.
3. [x] Shared algorithm — The Actor uses [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs). Today those functions take the search text, the zoom, and the Graph. The client call supplies the residence Graph. Actor Start supplies root, focus, and the full server Graph. The Search walk may ignore focus. The walk is that same algorithm.
4. [x] Up to 200 hits — The reply holds at most 200 Node ids, then the walk stops. That stop is the client algorithm from [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. Client hits plus this reply stop at 200.
5. [x] One reply — The Actor posts that one reply and stops. There is no continuation cursor.
6. [x] Stale reply — The dialog applies a reply only when it matches the current search text, or an equivalent generation of that text.

## Comments

- 2026-10-06: The Search Actor runs [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) on the full server graph. Find and Move send one Start after the search text settles, and they send none when the client already has 200 hits. The dialog keeps a reply only when the text or generation matches. Query cap is unchanged.
- 2026-10-06: Search Start records ActorStart on the event source, then ActorStop after the one reply. The record supplies root and focus. `graphIds` is the root. The walk reads the full server Graph from State. Query cap is unchanged.

## Time

- 2026-10-06 2h — Search Actor reuses the client walk (from chat)
- 2026-10-06 1h — ActorStart on the event source (from chat)

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 3 **Actor file**
