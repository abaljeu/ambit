# Dev debug workflow

Category: Development
See also: [[doc/arch.md]], [[.vscode/launch.json]], [[.vscode/tasks.json]]

## Two Server starters

Gambol has two alternate ways to start the Server on `:5215`. They are mutually exclusive: only one can own the port.

| Starter | How | Debugger |
|---------|-----|----------|
| Watch task | Run [[.vscode/tasks.json|dev: Watch + Run]] or [[.vscode/tasks.json|server: Run]] (`dotnet watch run`) | No |
| F5 | Launch [[.vscode/launch.json|Local Server]] or [[.vscode/launch.json|Full Stack]] | Yes |

F5 does not replace a running watch-task Server. If the watch task already owns `:5215`, F5 tries to start a second Server and fails.

## Windows Hyper-V port exclusion

On Windows, Hyper-V reserves dynamic TCP port ranges that can include the old dev port `5115`. If Kestrel fails with *access forbidden by its access permissions* (not *address already in use*), check exclusions in PowerShell:

```powershell
netsh interface ipv4 show excludedportrange protocol=tcp
```

Gambol dev now uses `:5215`, outside typical exclusion blocks. Pick another free port if `5215` is ever reserved locally.

## Fable watch

[[.vscode/tasks.json|fable: Watch Client]] and `esbuild: Watch Client` can stay running under either Server starter. Fable refreshes the generated modules and esbuild refreshes `Program.bundle.js`; client-only edits then need only a browser refresh.

The app loads the bundle by default in development and production. Open `/ambit?debug=1` to load unbundled Fable modules and their source maps for occasional debugging.

## Switching to F5

You do not stop a Server that F5 started in order to press F5 again. Stop the watch-task Server only when switching *to* F5 while `dev: Watch + Run` or `server: Run` is already running. If you never started the watch-task Server, just F5.

## Full Stack preLaunch

[[.vscode/launch.json|Full Stack]] `preLaunchTask` is [[.vscode/tasks.json|server: Build]] only (same as Local Server). It expects client JS already in `src/Server/wwwroot` from Fable watch or a manual [[.vscode/tasks.json|fable: Build Client]]. Cold Fable remains available via [[.vscode/tasks.json|fullstack: Build]] / `scripts/fullstack-build.sh`, not as Full Stack’s preLaunch.

## Server.Tests and DLL locks

Stop the debug session or the watch-task Server before rebuilding or running Server.Tests. Either process can lock DLLs under `src/Server/bin`.
