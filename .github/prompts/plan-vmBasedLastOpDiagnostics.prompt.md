## Plan: VM-Based Last Op Diagnostics

Move keyboard diagnostic state from Controller-local mutable ref into VM as a required string field. Diagnostic state is updated by wrapping successful ops with a helper that writes `lastSuccessfulOp` into the returned VM. `setLastKeyDisplay` becomes a pure DOM formatter that reads from the model — it never stores state. Unbound-key branches do not call `setLastKeyDisplay` at all; the display simply retains the previous value because the VM did not change.

**Core mechanism**

Introduce a wrapper in `Controller.fs`:

```fsharp
let withDiagnostic (key: string) (opName: string) (f: VM -> VM * Effect list) : VM -> VM * Effect list =
    fun model ->
        let newModel, effects = f model
        { newModel with lastSuccessfulOp = opName }, effects
```

`dispatchResolvedKey` wraps the op before dispatching:
```fsharp
dispatch (ApplyOp (withDiagnostic keyStr resolved.commandName op))
```

Palette/search run handlers wrap their ops the same way, using `""` or `"Palette"` as the key label and `cmd.name` as the operation name.

`setLastKeyDisplay` then becomes:
```fsharp
let setLastKeyDisplay (key: string) (operation: string) : unit =
    let el = document.getElementById "cmd-last-result"
    if not (isNull el) then
        el.textContent <- " | Last key: " + key + " → " + operation
```

It is called from render/patch with `model.lastSuccessfulOp` — never from key dispatch directly.

**Steps**
1. Phase 1: Model contract update
2. Add `lastSuccessfulOp: string` to `VM` in `d:/dev/amble/gambol/src/Shared/ViewModel.fs` (required field, no option).
3. Initialize `lastSuccessfulOp` in all VM constructors:
4. `StateLoaded` VM creation in `d:/dev/amble/gambol/src/Client/Update.fs` — use `editCommand.name` (`"Edit node"`) or a shared constant.
5. Update VM builders in tests:
6. `d:/dev/amble/gambol/tests/Shared.Tests/ViewModelTests.fs` helpers (`emptyModel`, `emptyModelAt`, `modelWithSel`).
7. `d:/dev/amble/gambol/tests/Shared.Tests/SyncLogicTests.fs` helper (`emptyModel`).
8. Phase 2: Controller — add `withDiagnostic`, simplify `setLastKeyDisplay` (*depends on 1*)
9. Remove Controller-local `lastSuccessfulOp` ref from `d:/dev/amble/gambol/src/Client/Controller.fs`.
10. Add `withDiagnostic` helper (see above).
11. Change `setLastKeyDisplay` to a pure DOM formatter `(key: string) -> (operation: string) -> unit`; no internal state.
12. Phase 3: Propagate diagnostics through update flow (*depends on 2*)
13. In `dispatchResolvedKey`, wrap dispatched op with `withDiagnostic keyStr resolved.commandName` in the `Some op` branch. Do nothing diagnostic in the `None` branch.
14. In unbound-key branches (`Error _` paths in `handleKey`, `handlePaletteKey`, `handleCssClassPromptKey`), do not call `setLastKeyDisplay` — the VM is unchanged so the display will retain the last successful op.
15. In `paletteRunOp` and the palette click handler in `View.fs`, wrap `op { model with mode = ret }` with `withDiagnostic "" cmd.name`. Remove the `setLastKeyDisplay None ...` calls.
16. In `onPaste`, `onCopyOrCut`, and `SearchDialogView.fs` handlers, replace direct `setLastKeyDisplay` calls with `withDiagnostic`-wrapped dispatch.
17. Phase 4: Wire display update from render (*depends on 3*)
18. Call `setLastKeyDisplay model.lastKey model.lastSuccessfulOp` (or equivalent) from `renderStatus` / a new `renderDiagnostics` function in `View.fs`, driven by the model — not by event handlers.
19. Phase 5: Consistency + verification (*parallel with 4 once compile succeeds*)
20. Run build/tests and fix exhaustive-record errors from VM field addition.
21. Sanity-check diagnostic UI (`#cmd-last-result`) in selection/editing/palette/search flows to confirm:
22. successful commands update display,
23. unbound keys do not overwrite display,
24. initial value is correct after state load.

**Relevant files**
- `d:/dev/amble/gambol/src/Shared/ViewModel.fs` - add `VM.lastSuccessfulOp` field.
- `d:/dev/amble/gambol/src/Client/Update.fs` - initialize field on `StateLoaded` VM creation.
- `d:/dev/amble/gambol/src/Client/Controller.fs` - remove ref, add `withDiagnostic`, simplify `setLastKeyDisplay`, update `dispatchResolvedKey` and palette op wrappers.
- `d:/dev/amble/gambol/src/Client/View.fs` - call `setLastKeyDisplay` from render/patch using model, remove direct calls from palette click handler.
- `d:/dev/amble/gambol/src/Client/SearchDialogView.fs` - replace direct `setLastKeyDisplay` calls with `withDiagnostic`-wrapped dispatch; drop unhandled-key display calls.
- `d:/dev/amble/gambol/tests/Shared.Tests/ViewModelTests.fs` - update VM helper constructors.
- `d:/dev/amble/gambol/tests/Shared.Tests/SyncLogicTests.fs` - update VM helper constructor.
- `d:/dev/amble/gambol/src/Server/wwwroot/gambol.template.html` - verify existing `#cmd-last-result` target remains valid (no structural changes expected).

**Verification**
1. Build client/server solution (`dotnet build src/Server -c Debug` and/or fullstack build task) and resolve compile breaks from new VM field and call signature changes.
2. Run test projects focusing on Shared tests (`tests/Shared.Tests`) to validate VM constructor updates.
3. Manual keyboard smoke test in browser:
4. execute known command in selection mode; verify `Last key` updates with key/op.
5. press unbound key; verify display remains unchanged.
6. run palette command; verify display updates only on success.
7. run search dialog keyboard actions; verify only handled actions update diagnostics.

**Decisions**
- `lastSuccessfulOp` lives in VM; updated only by `withDiagnostic` wrapper, never by direct mutation in event handlers.
- `setLastKeyDisplay` is a pure DOM formatter called from render, not from key handlers.
- Unbound/no-op branches do nothing; the display retains the last value because the VM did not change.
- Initialize with `editCommand.name` (`"Edit node"`) or a shared constant.

**Further Considerations**
1. If `editCommand` is not in scope where VM initializes, define a shared constant for the initial op string to avoid drift.
2. If future telemetry wants key misses, track them separately from the UI last-successful display.
3. `lastSuccessfulOp` currently stores only the operation name. If the key label is also wanted in the VM (for richer diagnostics), add a companion `lastSuccessfulKey: string` field in the same pass.