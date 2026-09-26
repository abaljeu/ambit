# 09 — Migrate Server Sync doors

**Type:** coding
**Status:** defined
**Blocked by:** None — can start immediately

## Context

The Server still answers Poll and post-Event with Changes only, and `/state` still scopes a complete ROOT Workspace. Shared defines the strict current-version requests and answers. This batch owns the production Server switch: small bootstrap `/state`, Poll POST, post-Event and Load Fetch answers, and the Server-side no-dangling builder. The Server Graph stays large.

## What to build

`postPoll` and `postEvents` require the Browser-computed Want and return Changes plus edges plus Nodes. `getState` uses saved Zoom only for the initial visible-closure. `postLoad` returns the same edges-plus-Nodes answer; no legacy package response remains.

### 1. Server Sync doors

Module [Server Sync doors](../arch.md). Story paths 1, 19, 20, 35.

- [ ] 6.2.1 Poll and post-Event answers — `postPoll` and `postEvents` require Browser-computed Want and return Changes plus edges plus Nodes
- [ ] 6.2.2 Small State — `getState` calls the saved-Zoom visible-closure projection; Core Graph stays complete
- [ ] 6.2.3 Load answer — `postLoad` returns `nodes` plus `childMap`, not `packages`
- [ ] 20.2 Current route — replace old Poll GET with the version 13 POST body; no compatibility door remains

### 2. Reserved bootstrap set

Module [Reserved ids](../arch.md). Story paths 4–7, 36.

- [ ] 14.2.2 Reserved Children — first paint includes Children of ROOT, TRASH, Workspaces Node, and SYSTEM
- [ ] 3.1 Zoom framing path — include the Loaded ancestor path to saved Zoom and Zoom Children
- [ ] 8.1 Production projection — stop using complete-ROOT `rootBootstrapGraph`; missing or stale Zoom falls back to ROOT

### 3. Want answer

Module [ResidentProjection](../arch.md). Story paths 21–23.

- [ ] 3.2.4 Server package builder — `wantAnswer` reads the large Server Graph for each Node id in Browser-supplied `want`
- [ ] 23.3 No dangling edges — include every target Node for every emitted edge; omit an edge rather than emit a missing target
- [ ] 20.3 Empty Want — `[]` returns empty `nodes` and `childMap`

### 4. Server proof

Prove each Server door against the current wire only.

- [ ] 19.3 and 20.3 Door proof — Poll and post-Event preserve Changes while adding the Want answer
- [ ] 8.1 Bootstrap proof — production State is the locked visible-closure, not a complete Workspace

## See also

[Browser residency architecture](../arch.md), [02 — Lock bootstrap visible-closure set](02-lock-bootstrap-visible-closure.md)

## Comments

- 2026-09-26: Filed via `/to-tickets`. Migrate batch. Blocked by expand.
- 2026-09-26: Redefined as the sole production Server-door and bootstrap owner.
- 2026-09-26: Republished via `/to-tickets`; completed expand 07 clears this ticket's live blockers.
