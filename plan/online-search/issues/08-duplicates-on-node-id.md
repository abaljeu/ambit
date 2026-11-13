# 08 — Duplicates on Node id

**Status:** `cancelled`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

This ticket is not the live story. The server results exclude Node ids the client search found, and the dialog appends those server hits. Alan, 2026-11-13.

The dialog already lists client hits. The old server phase asked only for the other hits. That story path is gone. Result identity is Node id (`NodeId`). That lock is [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**. [04 — Duplicate hit identity](04-duplicate-hit-identity.md) stays in its current shape.

## What to build

Do not build this ticket.

The old ask was a request that carries Node ids from the dialog rows. That ask stays superseded. The server list lists each Node id once. The dialog appends the server hits and excludes Node ids the client search found.

### 1. Search Actor

This section is not live.

1. [ ] Node id — Superseded. Do not drop a client hit out of a merged list.
2. [ ] Client-hit ids — Superseded. The request does not carry the Node ids the client search found. The dialog excludes those Node ids when it appends the server hits.

## Comments

- 2026-11-13: Alan. The server list lists each Node id once. The server results exclude Node ids the client search found, and the dialog appends those server hits. Displayed rows play no part, except that both searches start at the zoom root.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**
