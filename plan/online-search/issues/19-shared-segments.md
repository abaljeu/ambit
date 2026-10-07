# 19 — Shared segments

**Status:** `defined`
**Type:** coding
**Blocked by:** [14 — Cap of 200](14-cap-of-200.md), [15 — Dialog shows server hits](15-dialog-shows-server-hits.md), [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md), [18 — Move uses the same search](18-move-uses-the-same-search.md)

## Context

Find, Move, and Query each have their own story path. The person still sees one cap, one Find and Move Actor, and one query Actor. Story path **Shared segments** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Find and Move share one walk off the mailbox. The globe starts that walk when N is under 200. An edit starts a server search when the globe is selected. That walk posts one reply and stops. The walk is the client search algorithm. Query starts its own Actor when the line runs, posts one reply, and posts the Ref Replace on the event source. Each reply holds at most 200 Node ids. A query function may stop under 200.

### 1. Search Actor

**Search Actor** state stays on [Online search architecture](plan/online-search/arch.md) §2 item 1. The clear-fast rule is [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

1. [ ] Actor thread — The walk runs on the shared Find and Move Actor, off the mailbox. The globe starts that Actor when N is under 200. An edit starts a server search when the globe is selected. A keypress does not start the Actor when the globe is not selected. The walk is the client search algorithm. The Actor posts one reply and stops.
2. [ ] Cap — The reply holds at most 200 Node ids.
3. [ ] Want nodes — Those hit Headers ride the existing Want answer `nodes` list. This ride is a lock. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

### 2. Query Actor

**Query Actor** stays on [Online search architecture](plan/online-search/arch.md) §2 item 2. The Ref post stays a proposed design. Say event source.

1. [ ] Own start — Query starts its own Actor when the line runs. It posts one reply and stops.
2. [ ] Ref Change — Query also posts the Ref Replace on the event source.
3. [ ] Lower limit — When the function stops under 200, the result uses that stop. Otherwise the result stops at 200.

## See also

[Online search spec](plan/online-search/spec.md), [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
