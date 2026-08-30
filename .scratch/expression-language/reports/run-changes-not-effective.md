# Run changes not effective in the app

Branch: `w/expr`. Did not change `//` cluster parse or eval. Did not rewrite the error-message feature: it was already hooked in Shared and on Ctrl+Enter.

## Why the UI did not change

The live client served stale JS from [[src/Server/wwwroot]]. Shared.Tests compiled the new [[src/Shared/ExprRun.fs]] / [[src/Shared/AmbleRun.fs]] with `dotnet`. The Fable copy did not.

| Artifact | Before this work | After rebuild |
| --- | --- | --- |
| [[src/Server/wwwroot/UpdateAmbleRun.js]] | 17:01, called `AmbleRun.run`, no unfold | 19:25, `runPlan` + `applyUnfold` |
| [[src/Server/wwwroot/Shared/AmbleRun.js]] | 27 Aug, echoed the input line on parse/eval fail | 19:25, writes the Error string / `No matches found` |
| [[src/Server/wwwroot/Shared/ExprRun.js]] | missing | emitted; `=` path present |
| [[src/Server/wwwroot/Program.bundle.js]] | 17:01 | 19:27 |

No `fable watch` / `esbuild` watch was running. [[.vscode/launch.json]] Full Stack `preLaunch` is `server: Build` only, so a Server restart does not compile Client JS.

Ctrl+Enter was already `CommandId.Exec` → `runAmbleOp` in [[src/Client/Commands.fs]]. The Exec path was not the gap. Unfold was already in [[src/Client/UpdateAmbleRun.fs]] and [[src/Shared/AmbleRun.fs]] `applyUnfold`; [[src/Shared/ViewModelSiteMap.fs]] was not mid-edit (`applyFoldSession` already existed). Those edits were invisible because the bundle still called old `run`.

## Fable compile blocker (why watch could not catch up)

`bash ./scripts/client.sh build` first failed in [[src/Client/UpdateHelpers.fs]]: Fable resolved `Gambol.Shared.Node` / `Node` to `ExprAnswer.Node` / the `Node` type, not `module Node`. That name clash comes from [[src/Shared/ExprAnswer.fs]] (`ExprAnswer.Node`, `ExprAnswerType.Node`) in the same namespace as `module Node`. Shared.Tests still compile because they do not compile Client.

Patched only the two Client call sites so Fable can emit: `child.ref` in `childrenForPaste`, and an inlined first-child lookup in [[src/Client/Update.fs]] `firstGraphChild`. Error-message and unfold logic in Shared were left as they were.

## What was rebuilt

```
bash ./scripts/client.sh build
```

Result: Fable + esbuild succeeded. `missing argument` is in [[src/Server/wwwroot/Shared/ExprParse.js]] and in `Program.bundle.js`. `legacyRun` now passes the parse/eval message, not `line`.

Server DLLs are not on this path: Run plans ops in the Client; the Server only applies posted ops.

## How to reload

1. Keep this rebuilt `wwwroot` (or start `fable: Watch Client` and `esbuild: Watch Client` so later Shared edits ship).
2. Open `/ambit` (bundle) or `/ambit?debug=1` (unbundled modules + source maps). Prefer `?debug=1` when you debug Client.
3. Hard-reload the tab (Ctrl+Shift+R). Ack on CodeOutdated does not unblock a new bundle. See [[doc/reference/dev-debug-workflow.md]].
4. Check: `= /` → blueletter `missing argument`; `> python` → redletter `Expression type not implemented` (not an echo of the input); a successful Run unfolds the parent. `= //Example` remains a spec miss (`No matches found`) when Example is inside a Workspace.

## WORK.md mutations (for the root)

- `add` [[.scratch/expression-language/reports/run-changes-not-effective.md]] — HITL hard-reload `/ambit` or `/ambit?debug=1`; confirm Run error strings and unfold (not old red-echo)
