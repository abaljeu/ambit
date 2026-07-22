# Command last-result message format

Category: Client UX
Status: Implemented
See also: [[doc/arch]], [[src/Shared/ViewModel.fs]], [[src/Client/Controller.fs]], [[src/Client/Commands.fs]], [[src/Shared/CommandEntry.fs]]

## What it gives you

Every run of a named command updates `#cmd-last-result` as `Commandname: result description` (for example `Move down: OK` or `Upload: uploaded 3`).

Today the bar usually shows only `OK` (or a bare detail/error string) with no command name.

## What it avoids for now

- Richer result text for ops that still default to OK (no per-command success copy rewrite).
- Restyling `#cmd-last-result` (colors, icons) beyond the text format.
- Changing command registry / key tables beyond threading the existing display name.
- Anything tied to the in-flight Shared graph split ([[src/Shared/GraphOps.fs]] and related) unless a last-result call site is touched incidentally.

## Problem and current paths

| Path | Behavior |
|------|----------|
| [[src/Client/Controller.fs]] `withDiagnostic` | If the op did not change `lastCmdResult`, forces `CmdLastResult.Ok`. |
| [[src/Shared/ViewModel.fs]] `CmdLastResult.toDisplay` | `Ok` → `"OK"`; `Detail` / `Error` → message only. No command name. |
| [[src/Client/Controller.fs]] `dispatchResolvedKey` | Resolves `commandName` on `ResolvedKeyBinding` but does not pass it into `withDiagnostic` or display. |
| Palette run ([[src/Client/Controller.fs]] `paletteRunOp`, [[src/Client/View.fs]] palette click) | Has `cmd` / registry id → `CommandEntry` name, but wraps with nameless `withDiagnostic`. |
| Explicit setters | [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateImport.fs]], [[src/Client/UpdateRename.fs]], [[src/Shared/ViewModelMoveOps.fs]] set `Detail` / `Error` strings without a command-name prefix. |
| [[src/Client/View.fs]] | Calls `setCmdLastResultDisplay model.lastCmdResult` on render. |

## Assumptions

- Display name = existing `CommandEntry` / key-binding `commandName` (human name, not `CommandId` case name).
- Success default body stays `OK` (current `toDisplay` spelling).
- "Any command run" means palette + key-bound commands that already go through `withDiagnostic`, plus named async flows that already set `lastCmdResult` (notably git). Pointer-only `ApplyOp` without diagnostics stays out of scope unless it already sets a result.

## Minimal approach

Keep one `lastCmdResult` field. Carry an optional registry display name on each `CmdLastResult` case (`Ok` / `Detail` / `Error`). Format in `CmdLastResult.toDisplay` as `Name: body` when the name is `Some`.

Thread the name at the choke points that already know it:

1. `withDiagnostic commandName` — stamp name onto the result (default `Ok name` when the op did not set one).
2. Key dispatch and palette run: pass registry `commandName` / `CommandEntry` display name; overlay-only bindings skip `withDiagnostic`.

Ops that set `Detail`/`Error` with `None` name (git, import, rename, invalid move) get the name attached when wrapped by `withDiagnostic`.

## Implementation slices

### Slice 1 — Format + named diagnostic wrap

1. Add `lastCmdName: string option` on `VM` (or equivalent minimal field) and clear it where `lastCmdResult` is cleared.
2. Change `withDiagnostic` to take `commandName: string`; set `lastCmdName` and keep Ok-default behavior.
3. Change `setCmdLastResultDisplay` (or `CmdLastResult` display helper) to emit `Name: body` when a name is present.
4. Wire `dispatchResolvedKey` and both palette run paths to pass the known command name.
5. Verify: run a key command and a palette command; `#cmd-last-result` shows `…: OK` (or existing Detail/Error body) with the name prefix.

Success: named key/palette commands no longer show bare `OK`.

### Slice 2 — Async / explicit result setters

1. Ensure async command entry points set `lastCmdName` when they start, so later `Detail`/`Error` updates still format with the name.
2. Touch only call sites that already write `lastCmdResult` and lack a name (Upload/Download, import, rename, invalid move) if Slice 1 does not cover them via the shared field.
3. Verify: Upload/Download failure and an invalid-move refusal show `Commandname: …`.

Success: every intentional `lastCmdResult` write surfaces with a command name prefix when a command initiated it.

## Tests

- Prefer a pure Shared (or Client-logic) unit around the display formatter: given name + `Ok` / `Detail` / `Error`, assert `Name: OK` / `Name: msg`.
- Extend existing move refusal coverage in [[tests/Shared.Tests/ViewModelMoveOpsTests.fs]] only if the formatter or VM field lives in Shared and assertions need updating.
- No Server tests.

## Open questions

- ~~Should overlay-only bindings (`Close palette`, `Cancel`, …) also show in the bar, or only registry commands?~~ **Decided:** skip `#cmd-last-result` for command-bar chrome. `KeyBinding.commandName` is always a `string`; `commandBarOnly = true` gates the update in `dispatchResolvedKey` (does not call `withDiagnostic`). Registry / palette-run paths stamp the name onto `CmdLastResult`.
- Exact casing: keep `OK` (current `toDisplay`).
