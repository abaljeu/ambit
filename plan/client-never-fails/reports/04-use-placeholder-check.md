pass

## 04 — Use a placeholder

Branch `cursor/placeholder-commands-unavailable-95b1`. Copy, Edit classes, and Exec return the same model when the focused or selected row has no Header. A missing Header is a value. The Command sets no error.

## Before this change

- Copy read `graph.nodes.[nodeId].text` in `serializeSubtree`. A missing Header threw.
- Edit classes read `graph.nodes.[nid].cssClasses` in `initialUserClassesForSelection`. A missing Header threw.
- Exec read `graph.nodes.[focusNodeId].text` in `AmbleRun.shouldExec` and `AmbleRun.runPlanOnNode`. A missing Header threw. `CommandRequest.tryStart` already used `Map.tryFind`.

## Files

- `plan/client-never-fails/spec.md`
- `src/Client/Commands.fs`
- `src/Client/Controller.fs`
- `src/Client/UpdateOps.fs`
- `src/Shared/Paste.fs`
- `src/Shared/AmbleRun.fs`
- `src/Shared/CssClassToggle.fs`
- `src/Shared/CommandRequest.fs`
- `tests/Shared.Tests/PasteTests.fs`
- `tests/Shared.Tests/CssClassToggleTests.fs`
- `tests/Shared.Tests/AmbleRunTests.fs`

## Facts

- ``Copy does nothing when the selected row Header is absent`` asserts `anyNodeHeaderIsAbsent` and `serializeSubtree` returns `""`.
- ``Edit classes does nothing when the selected row Header is absent`` asserts `tryInitialUserClasses` is `None`, and a present Header with class `note` returns `Some "note"`.
- ``Exec does nothing when the focused row Header is absent`` asserts `nodeHeaderIsAbsent`, `shouldExec` is false, and `runPlanOnNode` is `Ok` with an empty plan.

## Commands

- `scripts/agent-test.sh` — Passed 67, Failed 0, in `tests/Shared.Tests`.
- `scripts/agent-test.sh --filter` for the eight facts this ticket names — Passed 8, Failed 0, in `tests/Shared.Tests`.
- `scripts/agent-test.sh --full` — `tests/Server.Tests` Passed 731, Skipped 1, Failed 0. `tests/Shared.Tests` Passed 1922, Skipped 3, Failed 0. Log `/tmp/ambit-full-suite-20261031-224535.log`.
