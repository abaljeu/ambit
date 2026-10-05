# 15 — Dialog shows server hits

**Status:** `defined`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md), [14 — Cap of 200](14-cap-of-200.md)

## Context

The server has finished one Find or Move reply. That reply is [07 — Server completes the picture](07-server-completes-the-picture.md). The cap of 200 is [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. The person is looking at the dialog. Story path **Dialog shows server hits** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

If the server finds N items, the Find dialog shows them. Move shows that same N. N is at most 200. The hit Headers use the Want answer the Browser already installs.

### 1. Want nodes for hits

**Want nodes for hits** stays a proposed design on [Online search architecture](plan/online-search/arch.md) §2 item 3. This ticket does not add a doc/current home for that design.

1. [ ] Show N — If the server finds N items, the Find dialog shows them. Move shows that same N. N is at most 200.
2. [ ] Want nodes — Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 7 **Standing locks**
