# 04 — Use a placeholder scope

Slice: [4 — Use a placeholder](plan/client-never-fails/spec.md) — The placeholder row selects. Expand leaves the row expanded. An id with no child list stays on Want for the next Poll under today’s `GraphChildren.isLoaded` rule. Every Command runs except edit. `startEditOp` leaves the mode unchanged when the Header is absent. Assumes [3 — See a moved Node](plan/client-never-fails/spec.md) already places a child id with no Header on a row. This slice does not draw Bullet color `#444444`, does not remove Loaded or Unloaded, and does not change `Want.compose` beyond documenting current behavior for an id with no child list.

## Signatures

[ ] `ViewModelRowState.isEntrySelected : VM -> SiteEntry -> bool` in [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs)
[ ] `ViewModelRowState.isEntryFocused : VM -> SiteEntry -> bool` in [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs)
[ ] `ViewModelRowState.startEditInstanceAtPos : SiteId -> int -> VM -> VM option` in [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs)
[ ] `ViewModelSiteMap.expandEntry : SiteId -> Graph -> SiteMap -> SiteId -> SiteMap * SiteId` in [src/Shared/ViewModelSiteMap.fs](src/Shared/ViewModelSiteMap.fs)
[ ] `GraphChildren.isLoaded : Graph -> NodeId -> bool` in [src/Shared/Model.fs](src/Shared/Model.fs) — `true` when `childMap` contains the id; absent key is Unloaded
[ ] `Want.compose : Graph -> SiteMap -> NodeId -> NodeId list` in [src/Shared/Want.fs](src/Shared/Want.fs) — filters with `not (GraphChildren.isLoaded graph id)`
[ ] `UpdateOps.startEditOp : VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.startEditAtPos : int -> VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.startEditInstanceAtPos : SiteId -> int -> VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.toggleFoldOp : SiteId -> VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.toggleFoldSelectionOp : VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.arrowRightSelectionOp : VM -> VM * Effect list` in [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `Commands.commandRegistry : CommandEntry2 list` in [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] `Commands.tryFindCommand : CommandId -> CommandEntry2 option` in [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] `CommandEntry.CommandId` and `CommandEntry.commandFor : CommandId -> CommandEntry option` in [src/Shared/CommandEntry.fs](src/Shared/CommandEntry.fs)

## Match sites

[ ] `ViewModelRowState.isInstanceDirectlySelected` and `ancestorMatch` gate `isEntrySelected` on `SiteEntry` parent instance and index range, not on `graph.nodes` — [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs)
[ ] `ViewModelRowState.startEditInstanceAtPos` reads `model.graph.nodes.[entry.nodeId].text` with no `Map.tryFind` — [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs)
[ ] `UpdateOps.startEditOp` and `UpdateOps.startEditAtPos` read `model.graph.nodes.[targetId].text` after `focusedNodeId` — [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.startEditInstanceAtPos` delegates to `ViewModel.startEditInstanceAtPos` — [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `UpdateOps.doubleClickRowAtPos` and `UpdateOps.pointerActivateRowAtPos` call `startEditInstanceAtPos` or `selectInstance` — [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `Commands.commandRegistry` binds `CommandId.EditNode` to `keyAlways startEditOp` — [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] `Controller.selectionKeyBindings` and `Controller.editingKeyBindings` rebuild from `commandRegistry` via `CommandMeta.commandFor` — [src/Client/Controller.fs](src/Client/Controller.fs)
[ ] `Commands.filteredCommands` and `Controller.paletteRunOp` run palette entries through `cmd.run ()` — [src/Client/Commands.fs](src/Client/Commands.fs), [src/Client/Controller.fs](src/Client/Controller.fs)
[ ] `CommandDock.appendDockSlot` runs dock `DockCommand` ids through `tryFindCommand` — [src/Client/CommandDock.fs](src/Client/CommandDock.fs)
[ ] `ViewModelSiteMap.expandEntry` sets `expanded = true` and syncs child `SiteEntry` rows from `GraphChildren.get graph entry.nodeId` — [src/Shared/ViewModelSiteMap.fs](src/Shared/ViewModelSiteMap.fs)
[ ] `UpdateOps.toggleFoldOp` calls `ViewModel.expandEntry` when the entry is collapsed — [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `Want.compose` calls `IncludedDescendantIds.throughChildrenOfExpandedNodes`, then `plusChildrenOfEach` twice, then `List.filter (not << GraphChildren.isLoaded)` — [src/Shared/Want.fs](src/Shared/Want.fs)
[ ] `Commands.jumpTargetOp` already no-ops when `Map.tryFind focusId model.graph.nodes` is `None` — [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] `RunEditCommit.commitTextOps` returns `[]` when `Map.tryFind nodeId graph.nodes` is `None` — [src/Shared/RunEditCommit.fs](src/Shared/RunEditCommit.fs)

## Existing tests

[ ] ``compose lists Included that miss Children first`` in [tests/Shared.Tests/WantTests.fs](tests/Shared.Tests/WantTests.fs) — `unload` removes a `childMap` key; `Want.compose` lists that id
[ ] ``compose then installWantAnswer loads wanted parents and children`` in [tests/Shared.Tests/WantTests.fs](tests/Shared.Tests/WantTests.fs) — expand via `expandEntry` then Want lists an unloaded grandchild
[ ] ``startEditInstanceAtPos selects instance and enters Editing with node text`` in [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) — Resident Header required today
[ ] ``startEditInstanceAtPos ignores graph root node`` in [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) — `None` when instance is graph root
[ ] `expandNode` helper using `expandEntry` in [tests/Shared.Tests/ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) — expand leaves `entry.expanded` true
[ ] No test names `startEditOp` or `EditNode` in [tests/](tests/) — Client `UpdateOps.startEditOp` is untested in Shared.Tests

## Copy-from

[ ] `Commands.jumpTargetOp` returns `model, []` when the focused id has no Header in `graph.nodes` — [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] `UpdateOps.startEditOp` returns `model, []` when `targetId = model.graph.root` — same no-op shape for absent Header — [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs)
[ ] `RunEditCommit.commitTextOps` returns `[]` on absent Header instead of posting — [src/Shared/RunEditCommit.fs](src/Shared/RunEditCommit.fs)

## write-ticket

### Source files

[ ] [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs) — `startEditInstanceAtPos` must not enter `Editing` when `Map.tryFind entry.nodeId model.graph.nodes` is `None`
[ ] [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs) — `startEditOp` and `startEditAtPos` must leave `mode` unchanged when the target id has no Header; expand ops already call `expandEntry`
[ ] [src/Client/Commands.fs](src/Client/Commands.fs) — `EditNode` registry entry stays; edit skip is enforced in `startEditOp`, not by removing the Command
[ ] [src/Shared/Want.fs](src/Shared/Want.fs) — no predicate change; document-only in ticket text that an id with no `childMap` key is Unloaded and passes the existing `not (GraphChildren.isLoaded graph id)` filter when it appears on the Included walk

### Tests (max 4 add or change; extras under ## Over size)

[ ] Add ``isEntrySelected is true for a placeholder SiteEntry with no Header`` in [tests/Shared.Tests/ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) — build graph with child id on a Loaded parent list and no `nodes` entry; `singleSelectionForInstance` + `isEntrySelected`
[ ] Add ``expandEntry leaves a placeholder row expanded`` in [tests/Shared.Tests/ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs) — same fixture; assert `entry.expanded` after `expandEntry`
[ ] Add ``compose wants a placeholder id with no child list after parent expand`` in [tests/Shared.Tests/WantTests.fs](tests/Shared.Tests/WantTests.fs) — parent expanded on SiteMap; absent Header id has no `childMap` key; assert id in `Want.compose` output
[ ] Add ``startEditInstanceAtPos leaves Selecting when Header is absent`` in [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) — assert `None` or unchanged `Selecting` for placeholder instance

## Size

[ ] Source files: 3 — [src/Shared/ViewModelRowState.fs](src/Shared/ViewModelRowState.fs), [src/Client/UpdateOps.fs](src/Client/UpdateOps.fs), [src/Shared/Want.fs](src/Shared/Want.fs) comment-only if needed (under 8)
[ ] Tests: 4 add in [tests/Shared.Tests/ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs), [tests/Shared.Tests/WantTests.fs](tests/Shared.Tests/WantTests.fs), [tests/Shared.Tests/ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs)

## Over size

[ ] Fifth test: Shared wrapper or Fable test for `UpdateOps.startEditOp` when Header absent — deferred until a Client test project exists

## Command inventory (slice 04)

[ ] `commandRegistry` Command ids: `EditNode`, `Rename`, `Exec`, `Cancel`, `SplitAtCursor`, `Delete`, `JoinWithPrevious`, `JoinWithNext`, `CursorUp`, `CursorDown`, `CursorFoldLeft`, `CursorLeftToParent`, `CursorUnfoldRight`, `CursorRightToChild`, `MoveToPreviousNode`, `MoveToNextNode`, `SelectionUp`, `SelectionDown`, `EditCursorUp`, `EditCursorDown`, `MoveUp`, `MoveDown`, `CursorToStart`, `CursorToEnd`, `MoveSelectionToStart`, `MoveSelectionToEnd`, `SelectToStart`, `SelectToEnd`, `CursorToTopOfView`, `CursorToEndOfView`, `MoveSelectionToTopOfView`, `MoveSelectionToEndOfView`, `Indent`, `Outdent`, `Escape`, `FoldUnfold`, `ZoomIn`, `ZoomOut`, `ZoomOwner`, `Undo`, `Redo`, `CopyContent`, `CopyAsLinks`, `DupNodes`, `InsertFile`, `CommandPalette`, `MoveSelected`, `Find`, `EditClasses`, `ToggleBold`, `ToggleItalic`, `ToggleCheck`, `JumpToTarget`, `Load`, `Save`, `Download`, `CheckGraph`, plus prePick `Load` and `Save` git/desk variants — [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] Edit entry Commands bound to edit start or edit-only keys: `EditNode` → `startEditOp`; `SplitAtCursor`, `JoinWithPrevious`, `JoinWithNext`, `EditCursorUp`, `EditCursorDown`, `SelectionUp`, `SelectionDown` (via `editingKeyHandler`) — [src/Client/Commands.fs](src/Client/Commands.fs)
[ ] Functions that must skip edit for an absent Header: `UpdateOps.startEditOp`, `UpdateOps.startEditAtPos`, `ViewModelRowState.startEditInstanceAtPos` (and Client `UpdateOps.startEditInstanceAtPos` / double-click path that calls it)
[ ] `Want.compose` today: an id with no `childMap` key has `GraphChildren.isLoaded graph id = false`, so it remains in the Want list whenever `IncludedDescendantIds` places it on the walk (same as an Unloaded node today) — [src/Shared/Want.fs](src/Shared/Want.fs), [src/Shared/Model.fs](src/Shared/Model.fs)
