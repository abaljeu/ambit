# Abort when Children remain after Delete

Investigation only. Branch `w/sitemap-parent-index`. Source claim: [[delete-range-refactor.md]] line 46. SiteMap holes: [[run-paths-sitemap.md]].

## Verdict

**Yes.** After a Delete that is supposed to clear the query Node's Children, Exec aborts (skips Run) if any Children remain. There is no path where a partial leftover Child list still Runs.

## Abort condition

[[src/Client/Commands.fs]] `execRunOp`, after the optional `deleteRange`:

- Read the focus Node on `afterDelete.graph`.
- `kidsLeft` is true when that Node exists and `children.Length > 0`.
- If `kidsLeft`: return `afterDelete` and do **not** call `runAmbleOp`.

Run happens only when `kidsLeft` is false (no Children, or the focus Node is gone).

## How Delete is attempted

Same function, only when `AmbleRun.shouldExec` is true and the focus Node has Children:

- Missing `focusedInstanceId` or missing Node: skip `deleteRange` (`afterDelete` unchanged).
- Missing SiteEntry for that instance: skip `deleteRange`.
- Else: [[src/Client/UpdateOps.fs]] `deleteRange` on `{ parent = query SiteEntry; start = 0; endd = n }`.

## Blocked classify (intended no-op abort)

[[src/Shared/ViewModelDeleteOps.fs]] `classifyDeleteForSelection`: if any selected Child is a blocked owned Delete (system folder or owned Workspace), return `[]`.

[[src/Client/UpdateOps.fs]] `deleteRange`: empty classify or empty `planDeleteOps` → model unchanged, no `applyAndPost`. `applyAndPost` `Error` (including `ResidentProjection.applyChange` `Invalid`, which rolls back) → graph unchanged.

Then `kidsLeft` is still true → Exec aborts. Graph unchanged → remap not required.

`planDeleteOps` always includes a full-span `ChildListWire.removeRange` when classify is non-empty. Classify is all-or-nothing (one blocked Child empties the whole list). There is no "delete some of the span" classify.

## Missing focus / SiteEntry

Skip Delete, Children remain, `kidsLeft` → abort. Graph unchanged.

## Partial apply that leaves Children

`ResidentProjection.applyChange` can skip an Unloaded `Replace` and still return `Changed` if other ops applied. `deleteRange` then returns `Ok` with Children still on the query Node. `kidsLeft` is true → Exec still aborts. Run does not proceed. SiteMap can be stale (see [[run-paths-sitemap.md]]).

## Not on this path

[[src/Client/UpdateOps.fs]] `deleteSelectionOp` remaps after command Delete. Exec does not use it.

`kidsLeft` is false when the focus Node is missing (`None`). That is not "Children remain". Delete of the query's Children does not remove the query Node under the current planner.
