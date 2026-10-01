# Workspace File Persistence

**Type:** grilling
**Status:** defined
Blocked by: None

## Question

Reconcile [Workspace File Persistence](doc/roadmap/workspace-file-persistence.md) with existing plans and with [[doc/current/]]. Decide which layout and write rules are current behavior and which stay target design. Leave the file at that path. This filing does not merge the claims.

## Contradictions

- This doc Status is Draft. Its authority line calls it the target design for server-side workspace file storage.
- [Workspace stage plan](doc/current/workspace-stage-plan.md) marks Stage 7 core and Stage 8 done, and leaves Stage 7 follow-ups open (hard delete under TRASH removes on-disk artifacts). It cites this doc as the full spec.
- [Persistence model](doc/current/persistence-model.md) describes auto-persist of document artifacts under `DataDir` and incremental writes that skip unchanged documents as server behavior. The same doc lists “Full per-document snapshot layout and incremental file writes” under Not implemented and points at this doc. Those two statements in the current doc disagree.

## Comments

- 2026-10-01 — Filed from Alan. Map: [[../map.md]]. Source stays [[doc/roadmap/workspace-file-persistence.md]].
