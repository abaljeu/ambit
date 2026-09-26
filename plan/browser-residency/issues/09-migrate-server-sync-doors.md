# 09 — Migrate Server Sync doors

**Type:** coding
**Status:** defined
**Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](01-lock-sync-want-package-shape.md), [02 — Lock bootstrap visible-closure set](02-lock-bootstrap-visible-closure.md), [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md), [08 — Migrate Shared wire](08-migrate-shared-wire.md)

## Context

The Server still answers Poll and post-Event with Changes only, and `/state` still scopes a complete ROOT Workspace. Shared defines the strict current-version requests and answers. This batch owns the production Server switch: small bootstrap `/state`, Poll POST, post-Event and Load Fetch answers, and the Server-side no-dangling builder. The Server Graph stays large.

## What to build

`postPoll` and `postEvents` require the Browser-computed Want and return Changes plus edges plus Nodes. `getState` uses saved Zoom only for the initial visible-closure. `postLoad` returns the same edges-plus-Nodes answer; no legacy package response remains.

### 1. Server Sync doors

Module [Server Sync doors](../arch.md). Story paths 1, 19, 20, 35.

1. [ ] Poll answers Want — story **Wants on Poll**: replace the old GET with current-version `postPoll`; return Changes plus edges plus Nodes
2. [ ] post-Event answers Want — 19.3: `postEvents` returns Changes plus edges plus Nodes
3. [ ] State is visible-closure — stories **Open a large Server Graph** and **Zoom first paint**: `getState` calls the locked small visible-closure projection
4. [ ] Server stays large — 35.1: Core Graph stays complete; visible-closure is the Browser answer only
5. [ ] Load uses current answer — story **Load uses the same package**: `postLoad` returns `nodes` plus `childMap`, not `packages`
6. [ ] No compatibility doors — old Poll and post-Event wire forms are removed; only version 13 is accepted

### 2. Reserved bootstrap set

Module [Reserved ids](../arch.md). Story paths 4–7, 36.

1. [ ] ROOT, TRASH, Workspaces Node, SYSTEM Children — 4.2–7.2, 36.2: first paint includes those Children
2. [ ] SYSTEM spelling — 36.1: reserved Node SYSTEM stays SYSTEM
3. [ ] Zoom path and Children — [02 — Lock bootstrap visible-closure set](02-lock-bootstrap-visible-closure.md): include the Loaded ancestor path to saved Zoom, Zoom Children, and required headers
4. [ ] Zoom fallback — missing or stale Zoom uses ROOT without widening to a complete Workspace

### 3. Want answer

Module [ResidentProjection](../arch.md). Story paths 21–23.

1. [ ] Server package builder — `wantAnswer` reads the large Server Graph for each Node id in the Browser-supplied `want`
2. [ ] No dangling edges — include every target Node for every emitted edge; omit an edge rather than emit a missing target
3. [ ] Empty Want — `[]` returns empty `nodes` and `childMap`

## See also

[Browser residency architecture](../arch.md), [spec.md](../spec.md) Solution **Bootstrap set**

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined as the sole production Server-door and bootstrap owner.
