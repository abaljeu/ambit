# 19 — Shared segments

**Status:** `defined`
**Type:** coding
**Blocked by:** [14 — Cap of 200](14-cap-of-200.md), [15 — Dialog shows server hits](15-dialog-shows-server-hits.md), [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md), [18 — Move uses the same search](18-move-uses-the-same-search.md)

## Context

Find, Move, and Query each have their own story path. Find and Move share the cap of 200. The query cap is `ExprRun.maxMaterialisedAnswers`. That number is 50. The person still sees one Find and Move Actor and one query Actor. Story path **Shared segments** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

Find and Move share one walk off the mailbox. That walk posts one reply and stops. The walk is the client search algorithm. That reply holds at most 200 Node ids. Query starts its own Actor when the line runs. The reply is `ActorQuery` of existing Node ids. That actor posts no ops. The query cap is `ExprRun.maxMaterialisedAnswers`. That number is 50. A query function may stop under 50. [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md) inserts Refs under the query line.

### 1. Search Actor

**Search Actor** state stays on [Online search architecture](plan/online-search/arch.md) §2 item 1. The clear-fast rule is [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

1. [ ] Actor thread — The walk runs on the shared Find and Move Actor, off the mailbox. The walk is the client search algorithm. The Actor posts one reply and stops.
2. [ ] Cap — The reply holds at most 200 Node ids.
3. [ ] Want nodes — Those hit Headers ride the existing Want answer `nodes` list. This ride is a lock. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

### 2. Query Actor

**Query Actor** stays on [Online search architecture](plan/online-search/arch.md) §2 item 2. The Ref post stays a proposed design. Say event source.

1. [ ] Own start — Query starts its own Actor when the line runs. The reply is `ActorQuery` of existing Node ids. The actor posts no ops. The Focus node's children stay as they are.
2. [ ] Insert — Refs under the query line stay 16 — Insert Refs under the query line. This actor writes no children.
3. [ ] Lower limit — When the function stops under `ExprRun.maxMaterialisedAnswers`, the result uses that stop. Otherwise the result stops at that cap. That number is 50. The Find and Move cap stays 200.

## See also

[Online search spec](plan/online-search/spec.md), [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md)
