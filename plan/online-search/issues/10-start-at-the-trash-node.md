# 10 — Start at the trash node

**Status:** `defined`
**Type:** coding
**Blocked by:** [09 — Skip trash](09-skip-trash.md)

## Context

The person starts a search at the trash node. [09 — Skip trash](09-skip-trash.md) keeps ordinary search out of trash. Story path **Start at the trash node** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

When the start Node is TRASH, the search includes trash. The person can see trash hits from that start.

### 1. Search Actor

The trash start stays on [Online search architecture](plan/online-search/arch.md) §2 item 1 **Search Actor**, State. TRASH is [GraphBuild.trashId](src/Shared/GraphBuild.fs).

1. [ ] TRASH — The search starts in trash when its start Node is TRASH ([GraphBuild.trashId](src/Shared/GraphBuild.fs)).

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
