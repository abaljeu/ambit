# Spec review — 07 Expand Want

Range: `origin/staging...HEAD` (`90aa79b1`). Ticket [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).

## (a) Missing or partial

1. **Visible-closure omits Included.** [map.md](plan/browser-residency/map.md) Destination: "Bootstrap is that same Zoom-scoped visible-closure, not a complete Workspace: Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM, plus ancestors of the Zoom root, plus Included." `visibleClosureGraph` loads reserved parents, owner ancestors, Zoom, and those Nodes' direct Children only. It does not walk Included. Ticket 07 asked to add that bootstrap set beside `rootBootstrapGraph`.

## (b) Scope creep

None. Production [App.fs](src/Client/App.fs) and Server doors still omit Want; [Api.fs](src/Server/Api.fs) only fills empty `nodes` / `childMap` so the new record compiles. `LoadResponse.packages` remain. `ApiVersion.current` is 13.

## (c) Implemented but wrong

1. **Dishonest Shared segment 6.** [arch.md](plan/browser-residency/arch.md): "6. [x] Visible-closure bootstrap (reserved Children + Zoom ancestors + Included)". Included is not in the function. Module map ResidentProjection 2.1 is also `[x]` after the sentence dropped Included.

2. **Dishonest story 21.2.** [arch.md](plan/browser-residency/arch.md): "2. [x] SyncLogic / ResidentProjection installs those edges". `applySyncResponse` still calls `installPackages` only. [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) owns SyncLogic install.

Want.compose two-tier, Fold, no throttle, `installWantAnswer` dangling / absent / `[]`, and one Shared compose (no second App Want) match the ticket.
