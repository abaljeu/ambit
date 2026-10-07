# 15 — Dialog shows server hits

**Status:** `defined`
**Type:** coding
**Blocked by:** [21 — Globe requests the server](21-globe-requests-the-server.md), [07 — Server completes the picture](07-server-completes-the-picture.md), [14 — Cap of 200](14-cap-of-200.md)

## Context

The server has finished one Find or Move reply. That reply is [07 — Server completes the picture](07-server-completes-the-picture.md). The cap of 200 is [14 — Cap of 200](14-cap-of-200.md) section 1 **Find and Move**. The person is looking at the dialog. Story path **Dialog shows server hits** is [Online search architecture](plan/online-search/arch.md) §1.

## What to build

If the server finds N items, the Find dialog shows that server list in place of the client list. Move shows that same list. N is at most 200. A server reply with Find results includes those found Nodes as Want-fulfillment. The replace rule is [21 — Globe requests the server](21-globe-requests-the-server.md).

### 1. Want nodes for hits

**Want nodes for hits** is a lock on [Online search architecture](plan/online-search/arch.md) §2 item 3. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

1. [ ] Show N — If the server finds N items, the Find dialog shows that server list in place of the client list. Move shows that same list. N is at most 200.
2. [ ] Want nodes — Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 11 **Want nodes**
