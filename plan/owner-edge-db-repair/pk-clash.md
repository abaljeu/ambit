# node_children_pkey clash during startup maintenance

## Cause

The write that hit `23505` on `node_children_pkey` (`(parent_id, ordinal)`) was the second `UPDATE` in [[src/Server/DatabaseProjection.fs]] `applyRootOrdinals` (called from `execSql` / `ExecuteNonQuery`). That matches the debugger stack.

The planner in [[src/Shared/ProjectionOwnershipRepair.fs]] keyed ROOT ordinal diffs by `(parentId, childId)`. A parent can have two rows for the same `child_id` (Owned + Ref). `Map.ofList` keeps the last row. When missing Workspaces/SYSTEM forced a ROOT insert and later siblings shifted, the planner emitted a spurious update of that `child_id` to ordinal `0`. SQL then used `WHERE child_id = ANY(...)`, matched **both** rows, and set both to ordinal `0`.

A dense ROOT list with unique child ids (U1, U2, TRASH) does not hit `23505`. An Owner+Ref sibling pair under ROOT does. That shape is realistic: Duplicate (link) creates a sibling Ref.

## Fix

- Planner: zip original ROOT children with surviving working ROOT children in ordinal order. Do not map by `(parent, child)`.
- Apply: identify rows by `fromOrdinal` (the PK ordinal), stage with `ordinal + 1000000`, then write final ordinals. Do not match ROOT shifts by `child_id`. Do not use temporary negative ordinals.
- Catch: `execSql` and `executeCommandSafe` catch around `ExecuteNonQuery` and return `Error`. `executeMaintenance` also catches reader/scalar failures. The agent still fail-closes with `Startup projection sweep failed: …` and goes to `failedLoop`. No silent swallow. No invented ROOT.

## Tests

Red before the fix: Shared `duplicate root child keeps both rows when canonicals insert`; Server `ownership repair shifts root with owner-and-ref sibling without pkey clash` (`Npgsql.PostgresException` `23505` / `node_children_pkey` in `execSql`).

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ProjectionOwnershipRepair"
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~DatabaseProjection|FullyQualifiedName~DbAgentTests"
```

Shared: 12 passed. Server related: 31 passed.

## Leftover risks

Ownership diffs still key by `(parentId, childId)` last-wins. That path does not change ordinals. Staging `+ 1000000` collides only if a ROOT ordinal is already that high. `executeCommand` (persist) and `executeCommandSafe` (maintenance) duplicate the ExecuteNonQuery setup. [[src/Server/DatabaseProjection.fs]] is still over the 400-line guideline.

## WORK.md mutations

None. This is a bug fix on the Active owner-edge repair.
