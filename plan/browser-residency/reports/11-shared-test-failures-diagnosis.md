# Shared test failures after 11 — Migrate Bullet and Included readers

Write-once diagnosis. Runtime evidence from the two named Shared tests on `cursor/migrate-bullet-included-f003` versus `origin/staging` (`b41c5dea`).

## Verdict

| Test | Caused by 11 — Migrate Bullet and Included readers? | Evidence |
| --- | --- | --- |
| `AmbDocumentTests.read ambiguous owner-link candidates keeps map order` | No. Already red on `origin/staging`. | Same assert; Actual is a new Guid each run. |
| `ViewModelRowStateTests.document state change patches all visible owned File member rows` | Yes. Green on `origin/staging`, red after **11 — Migrate Bullet and Included readers**. | File row `RecreateRow` because Unparsed now wins over resident Children. |

Do not change AmbDocument or the owner-link test in this ticket. That failure is baseline.

## Hypotheses

### Failure 1 — AmbDocument map order

- **A — No owner-link candidate (confirmed):** `tryMatchPlainOwnerChild` finds no owner-link candidate, then `resolvePlainLine` calls `NodeId.New()`.
- **B — Map order selects `highId` (rejected):** Map order selects `highId` (`0000...0002`). Actual Guid is not `highId`.
- **C — Reserved-id collision (confirmed):** Test ids collide with `Graph.trashId` / `Graph.workspacesId`. `addDetachedNode` overwrites those system Nodes. `fromNodes` then sets `owner` back to root.

Prediction for **A — No owner-link candidate** plus **C — Reserved-id collision**: candidate list for the document parent is empty; created id equals the test Actual.

### Failure 2 — ViewModel Option.get None

- **D — Unparsed Bullet forces row recreation (confirmed):** **11 — Migrate Bullet and Included readers** changes Unparsed precedence so the File Bullet changes from `FoldChevron` to `HollowCircle`. `planPatchDOM` emits `RecreateRow`, not `PatchRow` `SetClassName`. Line 129 `Option.get` is None.
- **E — PatchRow without SetClassName (rejected for the File row):** A `PatchRow` exists without `SetClassName`. The File row is `recreate`. Owned descendants do get `hasClass: true` patches.
- **F — Unparsed indicator is HollowCircle (confirmed):** `rowChildrenIndicator` on Unparsed with `kids: "1"` returns `HollowCircle`.

Prediction for **D — Unparsed Bullet forces row recreation** plus **F — Unparsed indicator is HollowCircle**: old Current+children = chevron; new Unparsed+children = hollow; mutation reason `indicator`.

## Runtime evidence

Filter used:

```
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ViewModelRowStateTests.document state change patches all visible owned File member rows|FullyQualifiedName~AmbDocumentTests.read ambiguous owner-link candidates keeps map order"
```

`origin/staging` worktree: `/tmp/gambol-staging-diag`. AmbDocument failed. ViewModel passed.

Debug NDJSON: `/opt/cursor/logs/debug.log`.

**Failure 1 — AmbDocument map order:**

- `hypothesisId C` (**C — Reserved-id collision**): id `0000...0002` `isWorkspaces: true`, `exists: true`, `resOwner` root.
- `hypothesisId C` (**C — Reserved-id collision**): id `0000...0001` `isTrash: true`, `exists: true`, `resOwner` root.
- `hypothesisId A` (**A — No owner-link candidate**) match: `kidsLen: 0`, `cands: ""`, `matched: "none"`.
- `hypothesisId A` (**A — No owner-link candidate**) created-new: `newId` `95d674db-0668-4d33-add5-496c3b9cccdc` equals the test Actual.

**Failure 2 — ViewModel Option.get None:**

- `hypothesisId F` (**F — Unparsed indicator is HollowCircle**): File `27066123-...` Current `kids: "1"` `FoldChevron`, then Unparsed `kids: "1"` `HollowCircle`.
- `hypothesisId D` (**D — Unparsed Bullet forces row recreation**): same File, `reason: "indicator"`, `mut: "recreate"`.
- `hypothesisId E` (**E — PatchRow without SetClassName**): child and grandchild `mut: "patch"` with `hasClass: true`.

**11 — Migrate Bullet and Included readers** did not touch AmbDocument. It did change Unparsed Bullet precedence in [ViewModelChildrenIndicator.fs](../../../src/Shared/ViewModelChildrenIndicator.fs), which [ViewModelDomPlan.fs](../../../src/Shared/ViewModelDomPlan.fs) uses to choose `RecreateRow`.

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
