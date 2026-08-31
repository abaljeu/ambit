# Owner-edge database repair — implementation

Stage: active. Branch `w/owner-edge-db-repair` was already checked out. No commit.

## What landed

Shared planner [[src/Shared/ProjectionOwnershipRepair.fs]] (after [[src/Shared/GraphProjection.fs]]) takes `rootId`, protected ids, and persistence rows. It GCs unreachable non-protected nodes, inserts missing Workspaces/SYSTEM/TRASH Owned-under-ROOT placements, demotes incoming Owned on ROOT, promotes the best ingress Ref until every survivor is on an acyclic Owned walk from ROOT, then keeps one ranked `'owner'` per child and demotes the rest to `'ref'`. A valid tree is a no-op. Missing `root_id` in `nodes` is `Error`. Planned rows are validated before the Server writes.

[[src/Server/DatabaseProjection.fs]] `executeMaintenance` now opens one transaction, loads the singleton plus all `nodes` and `node_children`, skips the planner when the singleton is absent, fails closed when ROOT is missing, applies the plan (node deletes with FK cascade, in-place ownership updates, node/child inserts, ROOT ordinal shifts), and never touches `changes`, `graph.revision`, or DataDir. Return value is `{ deletedIds; requiresReload; logFacts }`.

[[src/Server/DbAgent.fs]] / [[src/Server/DbAgentStartup.fs]]: on success, a non-no-op reloads via `tryLoadGraphFromProjection` and replaces frozen `state`/`persistedGraph` while keeping the DB revision. Reload error after commit goes to `failedLoop`. No-op uses `trimDeletedNodes`. `createForTest` fakes keep `requiresReload = false`. Production logs correction counts and affected ids with `eprintfn`. `loadPersistedState` swallow-and-invent is unchanged.

Startup paragraph in [[doc/current/persistence-model.md]] now describes repair plus reload.

Project [[.scratch/owner-edge-db-repair/project.md]] Stage is `active`. [[.scratch/index.md]] was regenerated.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ProjectionOwnershipRepair"
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~DatabaseProjection|FullyQualifiedName~DbAgentTests|FullyQualifiedName~DbAgentFailure"
```

Shared: 10 passed (nine spec stories; story 7 has missing-TRASH and missing-Workspaces/SYSTEM cases). Server related: 30 passed (existing GC contract, no revision/`changes` bump, missing ROOT fail-closed, dual-Owned ready Graph equals reloaded projection, FIFO/failure fakes).

## Leftover risks

[[src/Shared/ProjectionOwnershipRepair.fs]] (~440 lines) and [[src/Server/DatabaseProjection.fs]] (~515 lines) exceed the 400-line F# guideline; `executeMaintenance` / `plan` / `toPlan` exceed 40 lines. Frozen GetState during maintenance can still show the unrepaired Graph (accepted in the spec). No SQL unique/CHECK constraints (out of scope). Two-phase ROOT ordinal updates use temporary negative ordinals. Fable compile of the new Shared module was not run (Shared.Tests is .NET only).

## WORK.md mutations

- `move` [[.scratch/owner-edge-db-repair/spec.md]] from Pending to Active
