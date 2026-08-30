# Owner-edge database repair

Status: ready-for-agent

Persisted ownership lives in `node_children.ownership` (`'owner'` / `'ref'`). There is no `nodes.owner` column. Foreign keys make a dangling parent id impossible under ordinary constraints.

## Problem

`node_children` can store an Owned subgraph that is not a tree: multiple `'owner'` rows for one `child_id`, zero `'owner'` rows for a reachable node, ROOT appearing as an Owned child, or an Owned cycle. Startup today loads those rows, collapses dual-Owned silently in memory, and GC-deletes nodes unreachable from `graph.root_id` through any edge. The projection is not rewritten, so the next startup sees the same rows.

## Solution

Fold this repair into the existing `DbAgent` startup projection maintenance — the same window as today's unreachable-node sweep. Keep the current order: load the projection into a frozen Graph, serve reads while maintenance runs, commit GC plus ownership writes in one ACID transaction with no `changes` row, no revision bump, and no DataDir rewrite, then refresh the in-memory Graph to match the committed projection and publish ready.

Do not add a second startup pipeline before `tryLoadGraphFromProjection`. After commit, every surviving non-ROOT node has exactly one `'owner'` row, ROOT has none, and every Owned chain is acyclic and reaches ROOT.

Log correction counts and affected Node ids. An absent `graph` singleton stays today's empty no-op. A present singleton whose `root_id` is missing from `nodes` fails maintenance the same way a sweep failure already fails closed. Recurrence constraints are out of scope.

## User Stories

1. As an operator restarting the Server, I want a reachable node with several Owned parents to keep one Owned appearance and have the others become Ref, so that the outline still shows every former parent.
2. As an operator, I want a reachable node with no Owned parent to promote one existing Ref to Owned, so that it stays where it already appears rather than moving under TRASH.
3. As an operator, I want a node with no path from ROOT through any edge to be deleted, so that true lost rows stay garbage.
4. As an operator, I want an Owned cycle that has an ingress from the rooted tree to keep the rooted owner and demote the cycle-closing owner.
5. As an operator, I want an Owned cycle reachable only by Ref to promote that Ref, then apply the two-owner rule.
6. As an operator, I want ROOT never to be an Owned child.
7. As an operator, I want missing TRASH (row or Owned placement under ROOT) repaired so later Graph construction can rely on it.
8. As an operator, I want a missing ROOT node to abort startup rather than invent one.
9. As an operator, I want the correction durable in PostgreSQL without a synthetic History Change.

## Implementation Decisions

- Extend `runStartupSweep` / `ProjectionMaintenanceCommand`, not a pre-load repair. Frozen GetState during maintenance may still show the uncorrected Graph, as it may still show unreachable nodes today.
- After a successful maintenance commit, replace the frozen Graph from the projection (reload). `trimDeletedNodes` is not enough once Owned rows change. A no-op maintenance skips the extra load.
- Planner input is the full row set. Output is GC deletes plus ownership updates and canonical inserts. Keep today's protected ids. Liveness uses the **original** edges of both roles from `graph.root_id`.
- Persist the same canonical Owned-under-ROOT placements `Graph.fromNodes` already ensures in memory: Workspaces, SYSTEM, and TRASH. Do not attach ordinary survivors to TRASH. `fromNodes` remains an in-memory fallback for empty graphs and tests, not a second DB policy.
- On survivors, keep at most one `'owner'` per `child_id`. Rank competing Owned parents: parent already on an acyclic Owned path from ROOT; then Workspaces; then ordinary; then TRASH; then lowest parent id. Rank Ref promotion the same way, then lowest parent id, then lowest ordinal.
- Displaced `'owner'` rows become `'ref'` in place. Sibling ordinals stay put except when inserting a missing canonical Owned child under ROOT.
- Validate before commit: ROOT has no `'owner'` incoming row; every other surviving `nodes.id` has exactly one; following `'owner'` from each survivor reaches ROOT without a cycle.
- Idempotent: a valid tree is a no-op besides a zero-correction log.
- Tests drive the planner against persistence rows. One Server test covers missing-ROOT failing closed like sweep failure; one covers maintenance-then-reload so the ready Graph matches the repaired rows.

## Out of scope

- Unique or CHECK constraints to prevent recurrence.
- Rewriting `changes` or DataDir documents.
- In-memory Graph repair as a substitute for the projection write.
- Inventing ROOT.
- Attaching reachable ownerless nodes under TRASH.
