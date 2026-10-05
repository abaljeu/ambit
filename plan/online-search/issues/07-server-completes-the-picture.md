# 07 — Server completes the picture

**Status:** `defined`
**Type:** coding
**Blocked by:** [06 — Residence hits first](06-residence-hits-first.md)

## Context

The person has stopped changing the search text. The dialog already shows the client hits from [06 — Residence hits first](06-residence-hits-first.md). Story path **Server completes the picture** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

After a short quiet gap, one request starts the shared Find and Move Actor. The Actor returns one result and stops. The dialog keeps a reply only when that reply matches the text on screen.

### 1. Search Actor

State, Interface, and Uses for **Search Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 1. This ticket introduces the locked file `src/Server/SearchActor.fs`. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change. Say event source for that log.

1. [ ] Quiet gap — One server request fires after the search text has been unchanged for a short quiet gap. The gap has no millisecond value. A text change before the gap ends sends nothing. When the client already has 200 hits, the request is not sent.
2. [ ] Actor — That request starts the shared Find and Move Actor. Move does not start a second Actor. Query does not use this gap.
3. [ ] One result — The Actor returns one result and stops. There is no continuation cursor.
4. [ ] Stale reply — The dialog applies a reply only when it matches the current search text, or an equivalent generation of that text.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 3 **Actor file**
