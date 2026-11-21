# Dev debug workflow

Category: Development
See also: , [[.vscode/launch.json]], [[.vscode/tasks.json]]

## Local app URL

The local app is `http://localhost:8081/ambit`. Open `http://localhost:8081/ambit?debug=1` for unbundled Fable modules and source maps. Other docs name this URL by linking here; they do not copy host, port, path, or query.

## Two Server starters

Gambol has two alternate ways to start the Server on that port. They are mutually exclusive: only one can own the port.

| Starter | How | Debugger |
|---------|-----|----------|
| Watch task | Run [[.vscode/tasks.json|dev: Watch + Run]] or [[.vscode/tasks.json|server: Run]] (`dotnet watch run`) | No |
| F5 | Launch [[.vscode/launch.json|Local Server]] or [[.vscode/launch.json|Full Stack]] | Yes |

F5 does not replace a running watch-task Server. If the watch task already owns `:8081`, F5 tries to start a second Server and fails.

## Windows Hyper-V port exclusion

On Windows, Hyper-V, WSL2, and Docker Desktop reserve **dynamic** TCP port ranges. Those blocks often cover much of **5000–5600**, so ports like `5115` and `5215` are **not** reliable dev choices — they may fail with *access forbidden by its access permissions* even when nothing is listening.

Gambol’s default HTTP dev port is **8081** (above that band, and not the commonly held **8080**). Before first run on a new machine, confirm **8081** is not in an exclusion list:

```powershell
netsh interface ipv4 show excludedportrange protocol=tcp
```

If **8081** appears inside any range, pick another port that does not, then set `ASPNETCORE_URLS=http://localhost:<port>` (and match Desktop / browser URLs). `applicationUrl` in [[src/Server/Properties/launchSettings.json]] is the repo default.

## Fable watch

[[.vscode/tasks.json|fable: Watch Client]] and `esbuild: Watch Client` can stay running under either Server starter. Fable refreshes the generated modules and esbuild refreshes `Program.bundle.js`; client-only edits then need only a browser refresh.

The app loads the bundle by default in development and production. Open `/ambit?debug=1` to load unbundled Fable modules and their source maps for occasional debugging.

## Switching to F5

You do not stop a Server that F5 started in order to press F5 again. Stop the watch-task Server only when switching *to* F5 while `dev: Watch + Run` or `server: Run` is already running. If you never started the watch-task Server, just F5.

## Full Stack preLaunch

[[.vscode/launch.json|Full Stack]] `preLaunchTask` is [[.vscode/tasks.json|fullstack: Build]] (Server + Fable + esbuild). [[.vscode/launch.json|Local Client Chrome]] and Edge use [[.vscode/tasks.json|fable: Build Client]]. A failed Client compile aborts the browser launch. Azure Client configs do not compile locally.

## Server.Tests and DLL locks

Stop the debug session or the watch-task Server before rebuilding or running Server.Tests. Either process can lock DLLs under `src/Server/bin`.
