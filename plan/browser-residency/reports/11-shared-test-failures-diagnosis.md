# Shared test failures after ticket 11

Write-once diagnosis. Runtime evidence from the two named Shared tests on `cursor/migrate-bullet-included-f003` versus `origin/staging` (`b41c5dea`).

## Verdict

| Test | Ticket 11? | Evidence |
| --- | --- | --- |
| `AmbDocumentTests.read ambiguous owner-link candidates keeps map order` | No. Already red on `origin/staging`. | Same assert; Actual is a new Guid each run. |
| `ViewModelRowStateTests.document state change patches all visible owned File member rows` | Yes. Green on `origin/staging`, red after ticket 11. | File row `RecreateRow` because Unparsed now wins over resident Children. |

Do not change AmbDocument or the owner-link test in this ticket. That failure is baseline.

## Hypotheses

### Failure 1 — AmbDocument map order

- **A (confirmed):** `tryMatchPlainOwnerChild` finds no owner-link candidate, then `resolvePlainLine` calls `NodeId.New()`.
- **B (rejected):** Map order selects `highId` (`0000...0002`). Actual Guid is not `highId`.
- **C (confirmed):** Test ids collide with `Graph.trashId` / `Graph.workspacesId`. `addDetachedNode` overwrites those system Nodes. `fromNodes` then sets `owner` back to root.

Prediction for A+C: candidate list for the document parent is empty; created id equals the test Actual.

### Failure 2 — ViewModel Option.get None

- **D (confirmed):** Ticket 11 Unparsed precedence changes the File Bullet from `FoldChevron` to `HollowCircle`. `planPatchDOM` emits `RecreateRow`, not `PatchRow` `SetClassName`. Line 129 `Option.get` is None.
- **E (rejected for the File row):** A `PatchRow` exists without `SetClassName`. The File row is `recreate`. Owned descendants do get `hasClass: true` patches.
- **F (confirmed):** `rowChildrenIndicator` on Unparsed with `kids: "1"` returns `HollowCircle`.

Prediction for D+F: old Current+children = chevron; new Unparsed+children = hollow; mutation reason `indicator`.

## Runtime evidence

Filter used:

```
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ViewModelRowStateTests.document state change patches all visible owned File member rows|FullyQualifiedName~AmbDocumentTests.read ambiguous owner-link candidates keeps map order"
```

`origin/staging` worktree: `/tmp/gambol-staging-diag`. AmbDocument failed. ViewModel passed.

Debug NDJSON: `/opt/cursor/logs/debug.log`.

Failure 1:

- `hypothesisId C`: id `0000...0002` `isWorkspaces: true`, `exists: true`, `resOwner` root.
- `hypothesisId C`: id `0000...0001` `isTrash: true`, `exists: true`, `resOwner` root.
- `hypothesisId A` match: `kidsLen: 0`, `cands: ""`, `matched: "none"`.
- `hypothesisId A` created-new: `newId` `95d674db-0668-4d33-add5-496c3b9cccdc` equals the test Actual.

Failure 2:

- `hypothesisId F`: File `27066123-...` Current `kids: "1"` `FoldChevron`, then Unparsed `kids: "1"` `HollowCircle`.
- `hypothesisId D`: same File, `reason: "indicator"`, `mut: "recreate"`.
- `hypothesisId E`: child and grandchild `mut: "patch"` with `hasClass: true`.

Ticket 11 did not touch AmbDocument. It did change Unparsed Bullet precedence in [ViewModelChildrenIndicator.fs](../../../src/Shared/ViewModelChildrenIndicator.fs), which [ViewModelDomPlan.fs](../../../src/Shared/ViewModelDomPlan.fs) uses to choose `RecreateRow`.

## Ticket-scoped follow-up

If this ticket must leave Shared tests green for the ViewModel case: update that test to accept `RecreateRow` on the File row (Bullet changed) and keep class-patch asserts on owned descendants. Do not revert Unparsed hollow precedence. That precedence is the ticket behavior, already proven by `rowChildrenIndicator stays HollowCircle when Unparsed has resident children`.

Do not retarget AmbDocument ids or `addDetachedNode` here.

## Instrumentation

Temporary `#region agent log` blocks remain in:

- [GraphBuild.fs](../../../src/Shared/GraphBuild.fs)
- [AmbDocument.fs](../../../src/Shared/documents/AmbDocument.fs)
- [ViewModelChildrenIndicator.fs](../../../src/Shared/ViewModelChildrenIndicator.fs)
- [ViewModelDomPlan.fs](../../../src/Shared/ViewModelDomPlan.fs)

Not committed. Remove after confirmation.
