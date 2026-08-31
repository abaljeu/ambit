# Undo Slice 6 worker report

## Outcome

Command names are resolved at the command/event source and passed into `applyAndPost` / `applyAndPostSync` / `applyStructureLocally`. CommandEntry stays the registry source of truth via `displayName`. Audited non-registry sources use `Edit node`, `Paste`, `Cut`, `Load`, and explicit `Download`. Undo and Redo set `#cmd-last-result` on optimistic stack success, including `Undo: nothing to undo` and `Redo: nothing to redo`. Slice 7 was not started.

## Files changed

- [[src/Shared/ViewModel.fs]] — `CmdLastResult.undoResult` / `redoResult`
- [[src/Shared/ClientHistory.fs]] — `tryPeekUndoName` / `tryPeekRedoName`
- [[tests/Shared.Tests/ViewModelCmdLastResultTests.fs]] — Undo/Redo display text
- [[tests/Shared.Tests/ClientHistoryTests.fs]] — required names verbatim; peek helpers
- [[src/Client/UpdateHelpers.fs]] — `applyAndPost` takes `commandName`; text commit `Edit node`; split uses CommandEntry
- [[src/Client/UpdatePaste.fs]] — `Paste` / `Cut`
- [[src/Client/UpdateEdit.fs]] — Join with previous/next names
- [[src/Client/UpdateMove.fs]] — threaded names for Indent, Outdent, Move Up/Down
- [[src/Client/UpdateOps.fs]] — Delete, Duplicate, Edit classes, Move Selected family; Undo/Redo lastCmdResult
- [[src/Client/UpdateRename.fs]] — `Rename`
- [[src/Client/UpdateFileSearch.fs]] — `Insert…`
- [[src/Client/UpdateAmbleRun.fs]] — `Run`
- [[src/Client/UpdateWorkspaceSync.fs]] — Load phases named `Load`
- [[src/Client/UpdateWorkspaceDownload.fs]] — explicit Download stamp named `Download`
- [[src/Client/Controller.fs]] — paste `Paste`, cut `Cut`
- [[src/Client/CommandDock.fs]] — dock clicks wrap `withDiagnostic` with CommandEntry names

## Checkpoint

- ClientHistory tests prove required names (`Edit node`, `Paste`, `Cut`, `Load`, `Download`) are stored verbatim and survive Undo/Redo.
- CmdLastResult tests prove `Undo: Edit node`, `Undo: nothing to undo`, `Redo: Paste`, `Redo: nothing to redo`.
- Source search: no `applyLocalChange "Change"`; every `applyAndPost` / `applyAndPostSync` / `applyStructureLocally` caller passes a name.
- CommandDock, prompts (Rename / Edit classes), paste, cut, text commit, Load, and Download use the required names.
- Automatic path refresh remains an effect only; auto-download still applies no History-worthy Change.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ClientHistoryTests|FullyQualifiedName~ViewModelCmdLastResultTests|FullyQualifiedName~ClientHistoryRuntimeTests"
```

Passed: 35 of 35.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

No commit was created. Slice 7 was not started.

## Leftover risks for slice 7

- Prompt Enter stays `commandBarOnly`, so Rename / Edit classes success does not stamp `#cmd-last-result` via `withDiagnostic`. History names are still set at `applyAndPost`.
- Copy clipboard events still pass `withDiagnostic None` (copy is not History-worthy).
- Opening `CommandEntry` shadows `Mode.CommandPalette`; UpdateOps and UpdateMove now qualify `Mode.CommandPalette`.
- `State.history` remains an unused empty History field so Graph apply still has a State value (from Slice 5).
- DB startup sweep can still drop detached Undo headers; ChangeLog still has the inverse Change.
- Two `DbAgentTests` hang/failure cases from Slice 5 remain unrelated unless Slice 7 hits them again.
- Do not optimize SiteMap/encoding/network until Slice 7 measures.

## WORK.md mutations

- `remove` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 6 (wire command provenance and feedback)
- `add` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 7 (verify and measure) (parent: [[plan/selective-client-loading/undo-spec.md]])
- `move` none
- `block` none
