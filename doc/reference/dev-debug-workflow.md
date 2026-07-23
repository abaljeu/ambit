# Dev debug workflow

Category: Development
See also: [[doc/arch.md]], [[.vscode/launch.json]], [[.vscode/tasks.json]]

## Two Server starters

Gambol has two alternate ways to start the Server on `:5115`. They are mutually exclusive: only one can own the port.

| Starter | How | Debugger |
|---------|-----|----------|
| Watch task | Run [[.vscode/tasks.json|dev: Watch + Run]] or [[.vscode/tasks.json|server: Run]] (`dotnet watch run`) | No |
| F5 | Launch [[.vscode/launch.json|Local Server]] or [[.vscode/launch.json|Full Stack]] | Yes |

F5 does not replace a running watch-task Server. If the watch task already owns `:5115`, F5 tries to start a second Server and fails.

## Fable watch

[[.vscode/tasks.json|fable: Watch Client]] can stay running under either Server starter. Client-only edits then need only a browser refresh.

## Switching to F5

You do not stop a Server that F5 started in order to press F5 again. Stop the watch-task Server only when switching *to* F5 while `dev: Watch + Run` or `server: Run` is already running. If you never started the watch-task Server, just F5.

## Full Stack preLaunch

[[.vscode/launch.json|Full Stack]] `preLaunchTask` is [[.vscode/tasks.json|server: Build]] only (same as Local Server). It expects client JS already in `src/Server/wwwroot` from Fable watch or a manual [[.vscode/tasks.json|fable: Build Client]]. Cold Fable remains available via [[.vscode/tasks.json|fullstack: Build]] / `scripts/fullstack-build.sh`, not as Full Stack’s preLaunch.

## Server.Tests and DLL locks

Stop the debug session or the watch-task Server before rebuilding or running Server.Tests. Either process can lock DLLs under `src/Server/bin`.
