# 142 — View shows a child that has no content yet

**Status:** `defined`

One job: When a section of the outline lists an item that this page has not received yet, the page shows a gray "…" row in that item's place instead of crashing. The row fills in by itself when the item arrives. The graph already allows a listed child with no content ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1 Model). This ticket only changes how the view draws it.

Type: bug-fixing

Blockers: none

## Inputs

### Working

- [[src/Shared/ViewModelSiteMap.fs]]: builds one site entry per id in `GraphChildren.get` (around 82, 139, 243) with no `graph.nodes` check
- [[src/Client/RowView.fs]]: `buildRowElement` (137) reads `model.graph.nodes.[nodeId]` and throws for an absent id
- [[src/Shared/ViewModelDomPlan.fs]] (170): reads `newModel.graph.nodes.[entry.nodeId]`
- [[src/Shared/ViewModelChildrenIndicator.fs]]: `rowChildrenIndicator` takes a `Node`
- [[src/Shared/ViewModelRowState.fs]] (168): start-edit reads the text
- Command and update sites that read `graph.nodes.[id]` for the focused row:
  - [[src/Client/UpdateHelpers.fs]] 335
  - [[src/Client/UpdateMove.fs]] 120
  - [[src/Client/UpdateOps.fs]] 54, 66, 519, 583
  - [[src/Client/Commands.fs]] 245
  - [[src/Shared/ViewModelSearch.fs]] 223
  - [[src/Shared/ViewModelJoinOps.fs]] 49, 120, 166
  - [[src/Shared/ViewModelDeleteOps.fs]] 30
- [[src/Server/wwwroot/style.css]]: defines `amb-row`
- Tests: a new `Shared` test file beside the site-map and row-state tests, added to the `tests/Shared.Tests` project file

### Reference

- [[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1 and §2.4
- [[src/Shared/Model.fs]] `Graph`, `GraphChildren`

## Do not load

- `Graph`, `GraphBuild`, `GraphMutate` (no graph model change)
- sync and apply code (`SyncLogic`, `ResidentProjection`)
- `Want` (ticket 143 changes what is loaded)

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand a view helper `ViewModel.rowNode : VM -> NodeId -> Node option`. Each Working read site uses it.
3. [ ] Change `buildRowElement` and the `ViewModelDomPlan` read. An absent node draws a placeholder row:
   - class `amb-row-pending`
   - a hollow bullet
   - the text "…"
   - no fold chevron and no name
4. [ ] Keep the site entry for the absent child, so `childIndex` and moves of its siblings stay exact.
5. [ ] Change start-edit, zoom, and each command site. On a placeholder row they return an error value: `CmdLastResult.Error (None, "not loaded yet")`. Select, and move as one of the parent's children, still work.
6. [ ] Expand [[src/Server/wwwroot/style.css]] with `amb-row-pending`: muted text.
7. [ ] Expand the tests in Outputs.

## Outputs

- Changed files: each file under Working.
- Added tests:
  - `Shared`: ``Site map keeps a listed child that has no content``
  - `Shared`: ``Row plan for a child with no content is a placeholder``
  - `Shared`: ``Edit on a placeholder row returns not loaded yet``
  - `Shared`: ``Placeholder becomes a normal row when content arrives``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/142-view-shows-a-child-that-has-no-content-stop.md`, then ask.

- the fix needs a graph model change
- a read site outside Inputs throws for an absent id (name it)
- Alan prefers hiding the row to a placeholder ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] Q6)

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Shared`: ``Site map keeps a listed child that has no content``
  - `Shared`: ``Row plan for a child with no content is a placeholder``
  - `Shared`: ``Edit on a placeholder row returns not loaded yet``
  - `Shared`: ``Placeholder becomes a normal row when content arrives``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/142-view-shows-a-child-that-has-no-content-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.

## Placeholder behavior (Alan, call 2026-10-28)
- The placeholder is the bullet row for a listed child with no node data. It exists like any row.
- Expand behaves the same as on an unloaded node.
- Select works the same as on any row. It responds to every command except edit.
- When the data arrives, the next render draws it as a normal row. Nothing special is needed; the renderer handles it.
