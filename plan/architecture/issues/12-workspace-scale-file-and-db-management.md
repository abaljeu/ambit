# Workspace scale file and db management

**Type:** grilling
**Status:** defined
Blocked by: None

## Question

Reconcile [Workspace scale file and db management](doc/roadmap/workspace-scale-file-and-db-management.md) with existing plans and with [[doc/current/]]. Decide which rollout steps are current, which plan owns each leftover, and what this umbrella still adds. Leave the file at that path. This filing does not merge the claims.

## Contradictions

- The doc says it was written from discussions without reading Gambol sources, and that terms may need adaptation.
- Rollout step 1 (`DataDir` live-save and path moves) is marked done via [Workspace stage plan](doc/current/workspace-stage-plan.md) §7.
- Later steps already have other homes: WebDAV sync on [Transport layer](plan/transport-layer/project.md), expand-to-parse on [Workspace scale import](doc/roadmap/workspace-scale-import.md), on-demand residency on [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md). [[doc/index.md]] covers persistence, workspace graph, and workspace file sync, and has no page for this umbrella.

## Comments

- 2026-10-01 — Filed from Alan. Map: [[../map.md]]. Source stays [[doc/roadmap/workspace-scale-file-and-db-management.md]].
