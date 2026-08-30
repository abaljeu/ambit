# Duplicate canonical ROOT children

## Verdict

The planner does not insert a second `node_children` row when ROOT already has that child_id as Owned or Ref.

[[src/Shared/ProjectionOwnershipRepair.fs]] `ensureOneCanonical` finds any ROOT edge for the well-known id. An Owned edge is a no-op. A Ref edge is promoted in place (same ordinal). Insert and ROOT ordinal shifts occur only when that pair is absent.

## Hypothesis checks

- Only `'owner'` under ROOT: false. `tryFind` matches `parentId = rootId && childId = id` with any ownership.
- Treating Ref as absent: false. Ref promotes. Tests lock this.
- Ignoring an existing node row: the node row is not a ROOT edge. A present `nodes` row with no ROOT child still inserts one child row (first edge, not a duplicate).
- GC then re-insert: protected ids (ROOT, Workspaces, SYSTEM, TRASH) are not deleted. If the ROOT edge existed, it survives. If it did not, the planner inserts the first edge.
- fromNodes insert-before-TRASH copied as INSERT+shift when a Ref already sits under ROOT: false. The planner does not move a present ROOT Ref.

## What plans an insert

Insert of Workspaces, SYSTEM, or TRASH under ROOT occurs when survivor `node_children` has no row with `parent_id = root_id` and `child_id` equal to that well-known id.

Typical shapes:

1. Canonical node row present, ROOT edge absent (protected orphan, or Owned only under another parent).
2. Canonical node row absent (planner also inserts the node). Same ROOT-edge insert as (1).
3. Workspaces and/or SYSTEM absent as ROOT children while TRASH is present — later siblings including TRASH shift. This is the [[pk-clash.md]] fixture.

A Ref under ROOT does not insert and does not shift ordinals.

## Why SYSTEM/TRASH looked present

[[src/Shared/GraphProjection.fs]] `graphFromPersistence` builds the Graph with `Graph.fromNodes`. `fromNodes` adds missing Owned-under-ROOT placements in memory. The frozen Graph can show Workspaces, SYSTEM, and TRASH while `node_children` still lacks those edges. The planner reads raw rows, not that Graph.

Likely database shape: `nodes` already had the well-known ids (or the UI showed them via `fromNodes`), but ROOT had no `node_children` row for Workspaces and/or SYSTEM. The planner then inserted the first ROOT edge and shifted later siblings. That is not a second row for the same `(parent_id, child_id)`.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ProjectionOwnershipRepair"
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~DatabaseProjection|FullyQualifiedName~DbAgentTests"
```

Shared: 15 passed. Server related: 31 passed.

New tests in [[tests/Shared.Tests/ProjectionOwnershipRepairTests.fs]]:

- canonicals already under root as ref promote in place with no insert
- canonicals already owned under root do not insert or shift
- canonical node without root edge is inserted once

No planner or apply change.

## Leftover risks

If a canonical is Owned by Workspaces (rank 0) and has no ROOT edge, the planner inserts Owned under ROOT, then `demoteExtraOwners` may demote that new ROOT row to Ref. The insert still occurs. Spec wants Owned-under-ROOT; ranking can undo the role. This is not a duplicate of an existing ROOT row.

`toPlan` keys `insertChildren` by `(parentId, childId)`. A working duplicate of an existing pair would not emit `insertChildren`; `ensureOneCanonical` does not create that duplicate when the edge exists.

No unique constraint on `(parent_id, child_id)` (out of scope).

## WORK.md mutations

None.
