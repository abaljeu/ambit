# 04 — Use a placeholder
**Status**: 
One job: The outliner can select a placeholder row, expand it, and run every Command except edit, while Copy, Edit classes, and Exec stay unchanged because each needs the Node text. The story Use a placeholder in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md)

## Inputs

### Working

[ ] ``ViewModelRowState.isEntrySelected`` and ``ViewModelRowState.startEditInstanceAtPos`` in `src/Shared/ViewModelRowState.fs`.
[ ] ``UpdateOps.startEditOp``, ``UpdateOps.startEditAtPos``, and ``UpdateOps.toggleFoldOp`` in `src/Client/UpdateOps.fs`.
[ ] ``Want.compose`` filters with `not (GraphChildren.isLoaded graph id)` in `src/Shared/Want.fs`. An id with no child-list key passes that filter.
[ ] `Commands.commandRegistry` keeps `CommandId.EditNode`. The skip is in `startEditOp`, not in the registry.
[ ] `startEditInstanceAtPos` reads `model.graph.nodes.[entry.nodeId].text` with an index.
[ ] `startEditOp` and `startEditAtPos` read `model.graph.nodes.[targetId].text`.
[ ] `toggleFoldOp` calls `ViewModel.expandEntry`, which sets `expanded` true.
[ ] `Commands.jumpTargetOp` already returns the same model when the focused id has no Header.
[ ] ``compose lists Included that miss Children first`` in [[tests/Shared.Tests/WantTests.fs]] lists an id whose child-list key is absent.
[ ] ``startEditInstanceAtPos selects instance and enters Editing with node text`` in [[tests/Shared.Tests/ViewModelTests.fs]] requires a Header today.
[ ] ``startEditInstanceAtPos ignores graph root node`` in `tests/Shared.Tests/ViewModelTests.fs` returns `None` for the graph root.
[ ] `Commands.jumpTargetOp` in `src/Client/Commands.fs` returns `model, []` when `Map.tryFind` misses the Header.
[ ] `RunEditCommit.commitTextOps` in `src/Shared/RunEditCommit.fs` returns `[]` when the Header is absent.
[ ] [[src/Shared/ViewModelRowState.fs]] — `startEditInstanceAtPos` leaves the mode unchanged when the Header is absent.
[ ] [[src/Client/UpdateOps.fs]] — `startEditOp` and `startEditAtPos` do the same. Expand still calls `expandEntry`.
[ ] ``ViewModelDeleteOps.isBlockedOwnedDelete`` in `src/Shared/ViewModelDeleteOps.fs` reads `graph.nodes.[child.id].kind` with an index.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: Bullet color #444444. That color is 03 — See a moved Node.
[ ] Closed: `src/Shared/Want.fs` and `src/Client/Commands.fs`. The Want predicate and the Command registry stay.
[ ] Closed: Copy, Edit classes, and Exec. Each needs the Node text.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `startEditInstanceAtPos` so a missing Header leaves the mode unchanged.
3. [ ] Change `startEditOp` and `startEditAtPos` the same way. Leave expand on `expandEntry`.
4. [ ] Add the facts named in Outputs. The Want fact shows an id with no child list on `Want.compose` after expand.
5. [ ] Change `isBlockedOwnedDelete` so a missing Header is not a Workspace. Delete of that child follows the same MoveToTrash path.

## Outputs

[ ] `src/Shared/ViewModelRowState.fs`
[ ] `src/Client/UpdateOps.fs`
[ ] `tests/Shared.Tests/ViewModelRowStateTests.fs` — add the selection fact and the expand fact
[ ] `tests/Shared.Tests/WantTests.fs` — add ``compose wants a placeholder id with no child list after parent expand``
[ ] `tests/Shared.Tests/ViewModelTests.fs` — add ``startEditInstanceAtPos leaves Selecting when Header is absent``
[ ] `src/Shared/ViewModelDeleteOps.fs`
[ ] `tests/Shared.Tests/DeleteOpsTests.fs` — add ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/04-use-placeholder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``isEntrySelected is true for a placeholder SiteEntry with no Header``
  - `tests/Shared.Tests`: ``expandEntry leaves a placeholder row expanded``
  - `tests/Shared.Tests`: ``compose wants a placeholder id with no child list after parent expand``
  - `tests/Shared.Tests`: ``startEditInstanceAtPos leaves Selecting when Header is absent``
  - `tests/Shared.Tests`: ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/04-use-placeholder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
