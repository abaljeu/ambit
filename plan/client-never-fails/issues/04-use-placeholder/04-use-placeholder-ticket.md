# 04 — Use a placeholder
**Status**: coded
One job: The outliner can select a placeholder row, expand it, and run every Command except edit. Copy, Cut, Edit classes, and Exec do nothing when the focused or selected row is a placeholder, because each needs the Node text: no crash, no error, and the model stays the same. The story Use a placeholder in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [03 — See a moved Node](plan/client-never-fails/issues/03-see-moved-node/03-see-moved-node-ticket.md)

## Inputs

### Working

[x] ``ViewModelRowState.isEntrySelected`` and ``ViewModelRowState.startEditInstanceAtPos`` in `src/Shared/ViewModelRowState.fs`.
[x] ``UpdateOps.startEditOp``, ``UpdateOps.startEditAtPos``, and ``UpdateOps.toggleFoldOp`` in `src/Client/UpdateOps.fs`.
[x] ``Want.compose`` filters with `not (GraphChildren.isLoaded graph id)` in `src/Shared/Want.fs`. An id with no child-list key passes that filter.
[x] `Commands.commandRegistry` keeps `CommandId.EditNode`. The skip is in `startEditOp`, not in the registry.
[x] `startEditInstanceAtPos` reads `model.graph.nodes.[entry.nodeId].text` with an index.
[x] `startEditOp` and `startEditAtPos` read `model.graph.nodes.[targetId].text`.
[x] `toggleFoldOp` calls `ViewModel.expandEntry`, which sets `expanded` true.
[x] `Commands.jumpTargetOp` already returns the same model when the focused id has no Header.
[x] ``compose lists Included that miss Children first`` in [[tests/Shared.Tests/WantTests.fs]] lists an id whose child-list key is absent.
[x] ``startEditInstanceAtPos selects instance and enters Editing with node text`` in [[tests/Shared.Tests/ViewModelTests.fs]] requires a Header today.
[x] ``startEditInstanceAtPos ignores graph root node`` in `tests/Shared.Tests/ViewModelTests.fs` returns `None` for the graph root.
[x] `Commands.jumpTargetOp` in `src/Client/Commands.fs` returns `model, []` when `Map.tryFind` misses the Header.
[x] `RunEditCommit.commitTextOps` in `src/Shared/RunEditCommit.fs` returns `[]` when the Header is absent.
[x] [[src/Shared/ViewModelRowState.fs]] — `startEditInstanceAtPos` leaves the mode unchanged when the Header is absent.
[x] [[src/Client/UpdateOps.fs]] — `startEditOp` and `startEditAtPos` do the same. Expand still calls `expandEntry`.
[x] ``ViewModelDeleteOps.isBlockedOwnedDelete`` in `src/Shared/ViewModelDeleteOps.fs` reads `graph.nodes.[child.id].kind` with an index.
[x] `copyOp` in `src/Client/Commands.fs` calls `serializeSubtree`, which reads `graph.nodes.[nodeId].text`.
[x] `onCopy` in `src/Client/Controller.fs` calls that same read.
[x] `openCssClassPromptOp` in `src/Client/UpdateOps.fs` reads `model.graph.nodes.[nid].cssClasses`.
[x] `execRunOp` in `src/Client/Commands.fs` reaches `AmbleRun.shouldExec` and `AmbleRun.runPlanOnNode`, which read `graph.nodes.[focusNodeId].text`.
[x] `CssClassToggle.toggleClassOps` in `src/Shared/CssClassToggle.fs` already uses `Map.tryFind` and skips a missing Header.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[x] Closed: `GLOSSARY.md` and [[plan/client-never-fails/arch.md]]. This ticket leaves the glossary and the architecture unchanged.
[x] Closed: Bullet color #444444. That color is 03 — See a moved Node.
[x] Closed: `src/Shared/Want.fs`. The Want predicate stays. The Command registry in `src/Client/Commands.fs` stays.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `startEditInstanceAtPos` so a missing Header leaves the mode unchanged.
3. [x] Change `startEditOp` and `startEditAtPos` the same way. Leave expand on `expandEntry`.
4. [x] Add the facts named in Outputs. The Want fact shows an id with no child list on `Want.compose` after expand.
5. [x] Change `isBlockedOwnedDelete` so a missing Header is not a Workspace. Delete of that child follows the same MoveToTrash path.
6. [x] Change Copy so a missing Header on a selected row returns the same model and no effects. `serializeSubtree` reads each Header with `Map.tryFind`. A missing Header is a value.
7. [x] Change Edit classes so a missing Header on a selected row returns the same model and no effects. The class seed reads each Header with `Map.tryFind`.
8. [x] Change Exec so a missing Header on the focused row returns the same model and no effects. `shouldExec` and `runPlanOnNode` read the Header with `Map.tryFind`. A missing Header is a value, and the Command sets no error.
9. [x] Change Cut so a missing Header on a selected row returns the same model and no effects. `collectSubtree` reads each Header with `Map.tryFind`. `onCut` and `cutSelection` return before effects when a Header is absent.

## Outputs

[x] `src/Shared/ViewModelRowState.fs`
[x] `src/Client/UpdateOps.fs`
[x] `tests/Shared.Tests/ViewModelRowStateTests.fs` — add the selection fact and the expand fact
[x] `tests/Shared.Tests/WantTests.fs` — add ``compose wants a placeholder id with no child list after parent expand``
[x] `tests/Shared.Tests/ViewModelTests.fs` — add ``startEditInstanceAtPos leaves Selecting when Header is absent``
[x] `src/Shared/ViewModelDeleteOps.fs`
[x] `tests/Shared.Tests/DeleteOpsTests.fs` — add ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH``
[x] `src/Client/Commands.fs` — Copy and Exec return the same model when the Header is absent
[x] `src/Client/Controller.fs` — DOM Copy and `onCut` return before effects when the Header is absent
[x] `src/Client/UpdateOps.fs` — Edit classes returns the same model when the Header is absent
[x] `src/Shared/Paste.fs` — `serializeSubtree` and `collectSubtree` read the Header with `Map.tryFind`
[x] `src/Shared/AmbleRun.fs` — `shouldExec` and `runPlanOnNode` read the Header with `Map.tryFind`
[x] `src/Shared/CssClassToggle.fs` — `tryInitialUserClasses` returns `None` when a Header is absent
[x] `src/Shared/CommandRequest.fs` — `anyNodeHeaderIsAbsent` is the shared gate
[x] `tests/Shared.Tests/PasteTests.fs` — add ``Copy does nothing when the selected row Header is absent`` and ``Cut does nothing when the selected row Header is absent``
[x] `tests/Shared.Tests/CssClassToggleTests.fs` — add ``Edit classes does nothing when the selected row Header is absent``
[x] `tests/Shared.Tests/AmbleRunTests.fs` — add ``Exec does nothing when the focused row Header is absent``
[x] `src/Client/UpdatePaste.fs` — `cutSelection` returns the same model and no effects when a Header is absent

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/04-use-placeholder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``isEntrySelected is true for a placeholder SiteEntry with no Header``
  - `tests/Shared.Tests`: ``expandEntry leaves a placeholder row expanded``
  - `tests/Shared.Tests`: ``compose wants a placeholder id with no child list after parent expand``
  - `tests/Shared.Tests`: ``startEditInstanceAtPos leaves Selecting when Header is absent``
  - `tests/Shared.Tests`: ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH``
  - `tests/Shared.Tests`: ``Copy does nothing when the selected row Header is absent``
  - `tests/Shared.Tests`: ``Edit classes does nothing when the selected row Header is absent``
  - `tests/Shared.Tests`: ``Exec does nothing when the focused row Header is absent``
  - `tests/Shared.Tests`: ``Cut does nothing when the selected row Header is absent``
[x] Run `scripts/agent-test.sh --full`. Failed 0.
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/04-use-placeholder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
