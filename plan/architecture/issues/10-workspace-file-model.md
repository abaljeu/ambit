# Workspace File Model

**Type:** grilling
**Status:** defined
Blocked by: None

## Question

Reconcile [Workspace File Model](doc/roadmap/workspace-file-model.md) with existing plans and with [[doc/current/]]. Decide which claims are current behavior, which stay the target, and which doc is authority for each. Leave the file at that path. This filing does not merge the claims.

## Contradictions

- This doc Status is working draft. It says implemented behavior is summarized in [Workspace graph](doc/current/graph.md) and stage scope is [Workspace stage plan](doc/current/workspace-stage-plan.md). It also says [Revising Workspace File Model](doc/roadmap/revising-workspace-file-model.md) is the authoritative behavioral target.
- The Documents section says today the whole graph is one document (monolithic snapshot), and the same section says per-document `DataDir` persistence is implemented. [Workspace stage plan](doc/current/workspace-stage-plan.md) marks stages 1–8 done, including Stage 7 live-save and Stage 8 incremental persist.
- [Workspace graph](doc/current/graph.md) still describes the Stage 6 TRASH name-token change as a target (“when Stage 6 lands”). [Workspace stage plan](doc/current/workspace-stage-plan.md) marks Stage 6 done. Those two current docs disagree. This file is the stage vocabulary they both cite.
- [Document formats](plan/document-formats/map.md) says this file stays in the roadmap. Format specs live on that Project.

## Comments

- 2026-10-01 — Filed from Alan. Map: [[../map.md]]. Source stays [[doc/roadmap/workspace-file-model.md]].
