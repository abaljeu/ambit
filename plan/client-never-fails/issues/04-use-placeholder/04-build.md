# 04 — Use a placeholder build

Ticket: [[04-use-placeholder-ticket.md]]

Spec: [[plan/client-never-fails/spec.md]]

## Stage

[x] Stage is build in [04 — Use a placeholder](CONTEXT.md).

## Edits

[x] Spec story 4 says Copy, Edit classes, and Exec are unavailable on a placeholder Bullet because each needs the Node text.
[x] `copyOp` in `src/Client/Commands.fs` returns the same model when `CommandRequest.anyNodeHeaderIsAbsent` is true.
[x] `onCopy` in `src/Client/Controller.fs` returns before it reads Node text when that gate is true.
[x] `serializeSubtree` in `src/Shared/Paste.fs` reads each Header with `Map.tryFind`. A missing Header adds no text.
[x] `openCssClassPromptOp` in `src/Client/UpdateOps.fs` returns the same model when `CssClassToggle.tryInitialUserClasses` is `None`.
[x] `submitCssClassPromptOp` returns the same model when a selected Header is absent.
[x] `execRunOp` in `src/Client/Commands.fs` returns the same model when `CommandRequest.nodeHeaderIsAbsent` is true.
[x] `AmbleRun.shouldExec` returns false when the Header is absent.
[x] `AmbleRun.runPlanOnNode` returns `Ok` with an empty plan when the Header is absent.

## Check

[x] Verdict is `pass` in [04 — Use a placeholder check](plan/client-never-fails/reports/04-use-placeholder-check.md).

## Status

[x] Ticket status is `coded`.
