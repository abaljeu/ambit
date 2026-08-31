# Launch Client fail-closed

Stay on `w/expr`. Did not edit [[WORK.md]]. Did not rewrite agent instruction files.

## Root cause

[[.vscode/launch.json|(Local Client Chrome)]] and Edge already had `preLaunchTask` [[.vscode/tasks.json|fable: Build Client]], but that task used `"problemMatcher": []`. js-debug treats an empty matcher as nothing to wait on, so Chrome/Edge can open while `bash scripts/client.sh build` is still running, or after it printed F# errors. The browser then loads stale `src/Server/wwwroot`. `continueOnError` was unset (default false) and the watch tasks were not on this path.

[[.vscode/launch.json|(Client for Full Stack)]] had no Client `preLaunchTask`. Compound [[.vscode/launch.json|Local Server and Web Client]] used `preLaunchTask` [[.vscode/tasks.json|server: Build]] only, then started Chrome in parallel with Server. A Fable failure never blocked the browser.

Azure Client Chrome/Edge have no local compile; they were left unchanged.

## Files changed

- [[.vscode/tasks.json]] — [[.vscode/tasks.json|fable: Build Client]] and [[.vscode/tasks.json|fullstack: Build]] now use `$msCompile` and `"continueOnError": false` (same matcher as `server: Build`). `$msCompile` makes the debugger wait for process exit and fail the task on F# errors even if the shell exit code is lost. `client.sh` / `fullstack-build.sh` still use `set -e`, so esbuild failure is a non-zero exit.
- [[.vscode/launch.json]] — Full Stack compound `preLaunchTask` is [[.vscode/tasks.json|fullstack: Build]] (Server + Fable + esbuild). `(Client for Full Stack)` stays without its own `preLaunchTask` (same pattern as `(Desktop App for Local Server)`): the compound gate runs first; a failed compile does not start Chrome. Local Client Chrome/Edge still use `fable: Build Client`.
- [[doc/reference/dev-debug-workflow.md]] — Full Stack preLaunch paragraph matches the new chain and states that a failed Client compile aborts the browser launch.

## How to confirm

1. Break Fable (for example a syntax error in `src/Client`). Do not commit it.
2. F5 [[.vscode/launch.json|(Local Client Chrome)]] (or Edge) with the Server already on `:5215`. The Terminal shows `fable: Build Client` / `$msCompile` errors. Chrome must not open. `wwwroot` may still hold the last successful bundle; that is expected.
3. F5 [[.vscode/launch.json|Local Server and Web Client]]. `fullstack: Build` must fail and neither Server nor Chrome start.
4. Revert the syntax error. F5 again: compile succeeds, browser opens.
5. Azure Client configs still have no local Fable gate.
