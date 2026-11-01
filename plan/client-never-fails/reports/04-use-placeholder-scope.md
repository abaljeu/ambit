# 04 — Use a placeholder scope report

Slice: On a placeholder row (child id with no Header, Absent case), Commands Copy, Edit classes, and Exec stay unavailable. They do nothing: no crash, no error, model unchanged, when the focused or selected row is a placeholder. Each Command needs the Node text. Today they read node text with an indexer such as `graph.nodes.[id]`, which throws when the Header is absent. Alan decision 2026-10-31.

## Signatures

### Client Command ops

[ ] S-01 — `copyOp`: `let copyOp (model: VM) : VM * Effect list` in `src/Client/Commands.fs`.
[ ] S-02 — `openCssClassPromptOp`: `let openCssClassPromptOp (model: VM) : VM * Effect list` in `src/Client/UpdateOps.fs`.
[ ] S-03 — `execRunOp`: `let private execRunOp (model: VM) : VM * Effect list` in `src/Client/Commands.fs`.
[ ] S-04 — `initialUserClassesForSelection`: `let private initialUserClassesForSelection (model: VM) (sel: Selection) : string` in `src/Client/UpdateOps.fs`.

### Shared Paste clipboard

[ ] S-05 — `serializeSubtree`: `let serializeSubtree (graph: Graph) (siteMap: SiteMap) (topLevelIds: NodeId list) : string` in `src/Shared/Paste.fs`.
[ ] S-06 — `collectSubtree`: `let collectSubtree (graph: Graph) (siteMap: SiteMap) (topLevelChildren: ChildNode list) : ClipboardContent` in `src/Shared/Paste.fs`.

### Shared Exec and ActorStart

[ ] S-07 — `AmbleRun.runPlanOnNode`: `let runPlanOnNode (focusNodeId: NodeId) (graph: Graph) : Result<ExprRun.Plan, string>` in `src/Shared/AmbleRun.fs`.
[ ] S-08 — `AmbleRun.shouldExec`: `let shouldExec (graph: Graph) (focusNodeId: NodeId) : bool` in `src/Shared/AmbleRun.fs`.
[ ] S-09 — `CommandRequest.tryStart`: `let tryStart (graph: Graph) (siteMap: SiteMap) (zoomId: NodeId) (focusId: NodeId) (eventId: EventId) : Result<ActorStart, string>` in `src/Shared/CommandRequest.fs`.
[ ] S-10 — `CommandRequest.tryQueryStart`: `let tryQueryStart (graph: Graph) (siteMap: SiteMap) (zoomId: NodeId) (focusId: NodeId) (eventId: EventId) : ActorStart option` in `src/Shared/CommandRequest.fs`.

### DOM structured Copy

[ ] S-11 — `onCopy`: `let onCopy (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit` in `src/Client/Controller.fs`.
[ ] S-12 — `onCopyOrCut`: `let private onCopyOrCut (model: VM) (ev: Event) (dispatch: Msg -> unit) (updater: Updater) (includeNodeIds: bool) (commandName: string option) : unit` in `src/Client/Controller.fs`.

### Related Client updaters Copy invokes

[ ] S-13 — `copySelectionOp`: `let copySelectionOp (model: VM) : VM * Effect list` in `src/Client/UpdateOps.fs` (calls `collectSubtree` after Copy serializes text).

## Match sites

### Copy — Command `copyOp`

[ ] M-01 — `copyOp` `match model.selectedNodes` in `src/Client/Commands.fs` function `copyOp`; caller `commandRegistry` entry `cmd CopyContent (keyAlways copyOp)`.
[ ] M-02 — `serializeSubtree` `let node = graph.nodes.[nodeId]` and `node.text` in `walk` inside `src/Shared/Paste.fs` function `serializeSubtree`; callers `copyOp` in `Commands.fs` and `onCopyOrCut` in `Controller.fs`.
[ ] M-03 — `collectSubtree` `let node = graph.nodes.[nodeId]` in `walk` and `graph.nodes.[topId]` in the `findEntry` `None` branch in `src/Shared/Paste.fs` function `collectSubtree`; caller `copySelectionOp` in `UpdateOps.fs` after `copyOp` writes the system clipboard.

### Copy — DOM `onCopy`

[ ] M-04 — `onCopyOrCut` `match model.selectedNodes` in `src/Client/Controller.fs` function `onCopyOrCut`; caller `onCopy`.
[ ] M-05 — `onCopyOrCut` calls `serializeSubtree model.graph model.siteMap selectedIds` before `dispatch (ApplyOp … copySelectionOp)` in `Controller.fs` function `onCopyOrCut`.
[ ] M-06 — `App.fs` registers `hiddenInput.addEventListener("copy", … onCopy …)`; `RowView.fs` registers `onCopyWhileEditing` on the edit field, which delegates to `onCopy` when the field has no text range selection.

### Edit classes — Command `openCssClassPromptOp`

[ ] M-07 — `openCssClassPromptOp` `match model.selectedNodes` in `src/Client/UpdateOps.fs` function `openCssClassPromptOp`; caller `commandRegistry` entry `cmd EditClasses (keyAlways openCssClassPromptOp)` in `Commands.fs`.
[ ] M-08 — `initialUserClassesForSelection` reads `model.graph.nodes.[nid].cssClasses` per selected child id in `UpdateOps.fs` function `initialUserClassesForSelection`; caller `openCssClassPromptOp`.

### Exec — Command `execRunOp`

[ ] M-09 — `execRunOp` uses `afterEditCommit` then `match committed.selectedNodes` in `Commands.fs` function `execRunOp`; caller `commandRegistry` entry `cmd Exec (keyAlways execRunOp)`.
[ ] M-10 — `submitQuery` calls `CommandRequest.tryQueryStart` (uses `Map.tryFind focusId graph.nodes`, no indexer) in `Commands.fs` function `submitQuery`; caller `execRunOp`.
[ ] M-11 — `execRunOp` calls `CommandRequest.tryStart` on the Actor path in `Commands.fs` function `execRunOp`; `tryStart` uses `nodeText` / `Map.tryFind` on the owner path, not the focus Header indexer.
[ ] M-12 — `execAmbleRunOp` calls `AmbleRun.shouldExec committed.graph focusId` in `Commands.fs` function `execAmbleRunOp`; `shouldExec` uses `graph.nodes.[focusNodeId].text` in `src/Shared/AmbleRun.fs`.
[ ] M-13 — `execRunOp` on `tryStart` `Error` calls `AmbleRun.shouldExec` before `execAmbleRunOp` in `Commands.fs` function `execRunOp`.
[ ] M-14 — `runAmbleOp` calls `AmbleRun.runPlanOnNode focusId committed.graph` in `src/Client/UpdateAmbleRun.fs` function `runAmbleOp`; `runPlanOnNode` uses `graph.nodes.[focusNodeId].text` in `AmbleRun.fs`; caller `execAmbleRunOp` in `Commands.fs`.

## Existing tests

### Paste — `serializeSubtree` and `collectSubtree`

[ ] T-01 — ``serializeSubtree single node no children`` in `tests/Shared.Tests/PasteTests.fs` checks one loaded node serializes to its text.
[ ] T-02 — ``serializeSubtree two sibling nodes`` in `PasteTests.fs` checks two loaded siblings.
[ ] T-03 — ``serializeSubtree node with collapsed children omits children`` in `PasteTests.fs` checks collapsed expand omits child text.
[ ] T-04 — ``serializeSubtree node with expanded children includes children`` in `PasteTests.fs` checks expanded tree includes child lines.
[ ] T-05 — ``serializeSubtree multi-level expanded`` and ``serializeSubtree partial expand: only expanded level included`` in `PasteTests.fs` check deeper expand behavior.
[ ] T-06 — ``collectSubtree single node no children`` in `PasteTests.fs` checks clipboard map for one loaded node.
[ ] T-07 — ``collectSubtree collapsed node excludes children`` and ``collectSubtree expanded node includes children`` in `PasteTests.fs` check child inclusion rules.
[ ] T-08 — ``collectSubtree multiple top-level nodes`` in `PasteTests.fs` checks multiple top-level ids.
[ ] T-09 — No named test covers `serializeSubtree` or `collectSubtree` when the top-level id has no Header (Absent placeholder).

### AmbleRun — Exec gates

[ ] T-10 — ``runPlanOnNode after SetText uses committed node text`` in `tests/Shared.Tests/AmbleRunTests.fs` checks plan text tracks `SetText` on a loaded focus node.
[ ] T-11 — ``shouldExec is false for a non-statement line`` in `AmbleRunTests.fs` checks plain child text is not executable.
[ ] T-12 — Related Amble tests in `AmbleRunTests.fs` call `AmbleRun.runPlanOnNode` and `shouldExec` only on graphs with Headers present.
[ ] T-13 — No named test covers `shouldExec` or `runPlanOnNode` when the focus id has no Header.

### CommandRequest — ActorStart (Exec path, safe today)

[ ] T-14 — ``tryStart encodes distinct Focus Command and Zoom from owner-scan`` in `tests/Shared.Tests/CommandRequestTests.fs` checks `tryStart` request fields.
[ ] T-15 — ``tryStart errors when no runnable Command is on the path`` in `CommandRequestTests.fs` checks Error when no `?` command on the owner path.
[ ] T-16 — ``tryQueryStart uses the equals line as focus and command`` in `CommandRequestTests.fs` checks query lines via `Map.tryFind` on the focus node.
[ ] T-17 — ``Focus under count equals takes Amble path not ActorStart`` in `CommandRequestTests.fs` checks `tryQueryStart` returns `None` under an equals scan-stop.

### RunEditCommit — Exec helper mirror (not placeholder)

[ ] T-18 — ``stale Editing snapshot commits live text and queues ActorStart`` in `tests/Shared.Tests/RunEditCommitTests.fs` uses `afterEditCommitThenTryStart` mirroring Client `execRunOp` tryStart arm; all nodes have Headers.

### Placeholder graph helpers and sibling ticket tests

[ ] T-19 — `absentUnloaded` in `tests/Shared.Tests/GraphChildMapHelpers.fs` removes the Header and unloads the child-list key: `let absentUnloaded (nodeId: NodeId) (graph: Graph) : Graph`.
[ ] T-20 — ``isEntrySelected is true for a placeholder SiteEntry with no Header`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` uses `placeholderChildUnderRoot` and `absentUnloaded`.
[ ] T-21 — ``expandEntry leaves a placeholder row expanded`` in `ViewModelRowStateTests.fs` checks expand on a placeholder instance.
[ ] T-22 — ``compose wants a placeholder id with no child list after parent expand`` in `tests/Shared.Tests/WantTests.fs` uses `absentUnloaded` on a child id.
[ ] T-23 — ``startEditInstanceAtPos leaves Selecting when Header is absent`` in `tests/Shared.Tests/ViewModelTests.fs` checks edit does not enter Editing without a Header.
[ ] T-24 — ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH`` in `tests/Shared.Tests/DeleteOpsTests.fs` uses `absentUnloaded`.

### CssClassToggle — Edit-classes adjacent pattern

[ ] T-25 — Tests in `tests/Shared.Tests/CssClassToggleTests.fs` call `CssClassToggle.toggleClassOps`; missing ids produce no ops (no throw). No test covers prompt seed string for absent Headers.

## Copy-from

[ ] C-01 — `jumpTargetOp` in `src/Client/Commands.fs` uses `Map.tryFind focusId model.graph.nodes`; on `None` returns `model, []` without reading text.
[ ] C-02 — `startEditWithCaret` in `src/Client/UpdateOps.fs` uses `Map.tryFind targetId model.graph.nodes`; on `None` returns `model, []` (Edit Node Command, same placeholder rule as the slice).
[ ] C-03 — `CssClassToggle.toggleClassOps` in `src/Shared/CssClassToggle.fs` uses `Map.tryFind nid graph.nodes`; on `None` skips the id (no op, no throw).
[ ] C-04 — `RunEditCommit.commitTextOps` in `src/Shared/RunEditCommit.fs` uses `Map.tryFind nodeId graph.nodes`; on `None` returns `[]` (Shared gate Exec uses after edit commit).

## implement

### Ticket claims that differ from the code

[ ] I-01 — Ticket Input claims `startEditOp` and `startEditAtPos` read `model.graph.nodes.[targetId].text`; code in `UpdateOps.fs` `startEditWithCaret` already uses `Map.tryFind` and returns unchanged model when the Header is absent.
[ ] I-02 — Ticket Input claims `startEditInstanceAtPos` reads an indexer; `ViewModelRowState.startEditInstanceAtPos` already uses `Map.tryFind entry.nodeId model.graph.nodes` and keeps Selecting when absent.
[ ] I-03 — Ticket Input claims `ViewModelDeleteOps.isBlockedOwnedDelete` reads `graph.nodes.[child.id].kind` with an indexer; code already uses `Map.tryFind child.id graph.nodes` and treats absent as not a Workspace.
[ ] I-04 — Ticket Do not load lists Copy, Edit classes, and Exec as closed; the slice and Alan decision 2026-10-31 require those three Commands to no-op on a placeholder without throw.
[ ] I-05 — Ticket Outputs list tests for selection, expand, Want, edit, and delete placeholders; those named tests already exist in `ViewModelRowStateTests.fs`, `WantTests.fs`, `ViewModelTests.fs`, and `DeleteOpsTests.fs`.
[ ] I-06 — Copy Command still throws today: `serializeSubtree` and `collectSubtree` index `graph.nodes.[nodeId]` for selected placeholder child ids (`M-02`, `M-03`).
[ ] I-07 — Edit classes Command still throws today: `initialUserClassesForSelection` indexes `graph.nodes.[nid].cssClasses` (`M-08`).
[ ] I-08 — Exec Command still throws today on Amble paths: `AmbleRun.shouldExec` and `AmbleRun.runPlanOnNode` index focus text (`M-12`, `M-14`); ActorStart arms already use `Map.tryFind`.

### How new tests observe the change at a public Shared seam

[ ] I-09 — Proposed test ``serializeSubtree returns empty when top-level id has no Header`` calls `Paste.serializeSubtree` with a graph built via `absentUnloaded` and asserts `""` with no exception (Copy Command and DOM Copy both call this function before updating the model).
[ ] I-10 — Proposed test ``userClassesPromptSeed skips absent Headers in selection`` calls a new Shared function extracted from Client `initialUserClassesForSelection` (same `Map.tryFind` pattern as `CssClassToggle.toggleClassOps`) and asserts `""` when every selected id lacks a Header (Edit classes Command will call that Shared seed).
[ ] I-11 — Proposed test ``shouldExec is false when focus id has no Header`` calls `AmbleRun.shouldExec` with `absentUnloaded focusId graph` and asserts `false` without exception (Exec Command `execRunOp` and `execAmbleRunOp` gate on `shouldExec` before indexing text).
[ ] I-12 — Optional follow-on test ``runPlanOnNode errors when focus id has no Header`` calls `AmbleRun.runPlanOnNode` on an absent focus id and asserts `Error _` without exception (covers `runAmbleOp` in `UpdateAmbleRun.fs`).

### Three Command test names and public Shared entry points

[ ] I-13 — Copy — test name ``serializeSubtree returns empty when top-level id has no Header`` — public seam `serializeSubtree` in `src/Shared/Paste.fs`.
[ ] I-14 — Edit classes — test name ``userClassesPromptSeed skips absent Headers in selection`` — public seam new Shared helper (extract from `initialUserClassesForSelection`; mirror `CssClassToggle.toggleClassOps` lookup style).
[ ] I-15 — Exec — test name ``shouldExec is false when focus id has no Header`` — public seam `shouldExec` in `src/Shared/AmbleRun.fs`.
