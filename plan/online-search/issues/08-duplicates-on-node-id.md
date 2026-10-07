# 08 — Duplicates on Node id

**Status:** `cancelled`
**Type:** coding
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)

## Context

Superseded by [21 — Globe requests the server](21-globe-requests-the-server.md). This ticket is not the live story. The server reply replaces the client list. The start does not carry shown Node ids.

The dialog already lists client hits. The old server phase asked only for the other hits. That story path is gone. Result identity is Node id (`NodeId`). That lock is [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**. [04 — Duplicate hit identity](04-duplicate-hit-identity.md) stays in its current shape.

## What to build

Do not build this ticket. [21 — Globe requests the server](21-globe-requests-the-server.md) is the live story.

The old ask was: a client hit and a server hit are the same hit when they share a Node id, and the one request asks only for hits the client does not already have. That ask is superseded. The server list lists each Node id once, and that list replaces the client list.

### 1. Search Actor

This section is not live. [21 — Globe requests the server](21-globe-requests-the-server.md) owns the list rule.

1. [ ] Node id — Superseded. Do not drop a client hit out of a merged list.
2. [ ] Shown ids — Superseded. The start does not carry shown Node ids.

## Comments

- 2026-10-07: Superseded by [21 — Globe requests the server](21-globe-requests-the-server.md). The quiet-gap merge is not the Find path. The server list lists each Node id once, and that list replaces the client list.

## See also

[Online search spec](plan/online-search/spec.md) §1 Search spec, [Online search map](plan/online-search/map.md) Decisions so far item 5 **Node id**
