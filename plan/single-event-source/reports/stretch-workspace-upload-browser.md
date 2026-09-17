# Stretch: desktop workspace upload without WPF

Date: 2026-09-17. Question: can Shared.dotnet Upload land a named Workspace and one File on a local server, then show both in a headed Browser, with no WPF host? Result: **pass**. Reshape: one Shared.dotnet implementation; stretch entry is thin FSI.

Related: [[doc/current/desktop-local-files.md]], [[doc/roadmap/workspace-file-sync.md]], [[src/Shared/dotnet/WorkspaceCloudUpload.fs]], [[src/Shared/dotnet/AmbitSession.fs]], [[src/Shared/dotnet/WorkspaceFileSync.fs]], [[src/Desktop/WorkspaceSyncEndpoints.fs]], [[src/Server/WorkspaceWebDav.fs]]. Harness: [[scripts/stretch-workspace-upload.sh]] + [[scripts/stretch-workspace-upload.fsx]].

## Shape

Desktop non-UI and the stretch script call the same modules. [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) owns HttpClient construction, `gambol_auth` from [AuthToken.fs](src/Server/AuthToken.fs) (compiled into Shared.dotnet), GET `/state?scope=full`, and POST `/changes`. [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) owns create (`FileNodeOps.planCreateWorkspace`), inventory (`WorkspaceLocalInventory.listForUpload`), stubs (`WorkspaceUploadStructure.planStubOps`), WebDAV push (`WorkspaceFileSync.post`, same as `/_desktop/workspace-push`), and Unparsed mark (`planServerFilePresentOps`). [WorkspaceSyncEndpoints.fs](src/Desktop/WorkspaceSyncEndpoints.fs) and [LocalProxy.fs](src/Desktop/LocalProxy.fs) call `AmbitSession.cookieHeader` / `createHttpClient`. [stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx) only parses argv and prints PASS lines. No WPF, folder picker, or WebView2.

## Verdict

| Check | Result |
| --- | --- |
| Workspace created on the server Graph | **Pass.** `POST /ambit/changes` EventId-zero Change: `FileNodeOps.planCreateWorkspace` → `NewSpecialNode` Workspace `stretch`. Ack `eventId=1`. |
| Folder uploaded on the desktop WebDAV path | **Pass.** Same `WorkspaceFileSync.post` as `/_desktop/workspace-push`: prepare-push, `hello.md` PUT via upload-capability, finish-commit. `uploaded=1` `paths=hello.md`. Server file `data/stretch/hello.md` holds marker `stretch-upload-1789649487-4850`. |
| Browser shows the Workspace | **Pass.** Headed Chrome `http://localhost:5215/ambit?debug=1` (auto cookie login). Outline: Workspaces → `stretch`. |
| Browser shows a File | **Pass.** After Load (`Ctrl+Shift+.`) and ArrowRight, outline shows File `hello.md` (`amb-row-sync-unparsed`, `…`). Graph id `4665335f-35f6-4c13-9ee1-fa2c0946e923`, `documentState=unparsed`. |

## Commands

Local-only: set [[src/Server/appsettings.Development.json]] `DB_CONNECTION_STRING` to `Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev`. Do not commit that file. `addAppSettings` loads Development json after environment variables, so `postgres`/`postgres` would win and writes become read-only.

```
scripts/client.sh build
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5215 \
  DB_CONNECTION_STRING='Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev' \
  dotnet run --project src/Server --no-launch-profile
printf 'stretch-upload-1789649487-4850\n' > /tmp/ambit-stretch-upload/hello.md
scripts/stretch-workspace-upload.sh
```

`stretch-workspace-upload.sh` writes the fixture, builds Shared.dotnet, then `dotnet fsi` on [stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx). The script calls `WorkspaceCloudUpload.run` (login GET `/ambit` for `gambol_auth`; GET `/state`; create; stubs; `WorkspaceFileSync.post`; mark Unparsed).

Browser (manual): open `/ambit?debug=1`; select `stretch`; Load; ArrowRight.

## Evidence

Harness stdout: `PASS workspace-created eventId=1`; `PASS stubs stub eventId=2`; `PASS upload uploaded=1 … paths=hello.md`; `PASS nodes … file:hello.md,workspace:stretch`. After mark, `GET /state?scope=full` `eventId=3`.

CDP row dump after unfold: Workspaces; Workspace `stretch`; File `hello.md` class `amb-row-sync-unparsed`.

## Blockers (none fatal)

- Development json reconnects to `postgres`/`postgres` after env load. Local override only.
- `Thoth.Json.JavaScript` is Fable dummy on .NET. Shared.dotnet encodes `/changes` and `/state` with `Thoth.Json.Newtonsoft` (same as Shared.Tests).
- `git` not on PATH. Upload still ran; detail said `.gitignore filter skipped (git unavailable)`.
- RootClosure boot shows the named Workspace as a header. File children need Load, then unfold.
- Cookie is `Secure`. Chrome on `localhost` HTTP accepted it. `AmbitSession` sends `Cookie` on each request (does not use a Secure cookie jar).
- Cursor `computerUse` did not start (model usage). Headed Chrome + CDP on `DISPLAY=:1` is the Browser proof.
