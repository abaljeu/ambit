# Stretch: desktop workspace upload without WPF

Date: 2026-09-17. Question: can Shared.dotnet Upload land a named Workspace and one File on a local server, then show both in a headed Browser, with no WPF host? Result: **pass**.

Related: [[doc/current/desktop-local-files.md]], [[doc/roadmap/workspace-file-sync.md]], [[src/Shared/dotnet/WorkspaceFileSync.fs]], [[src/Desktop/WorkspaceSyncEndpoints.fs]], [[src/Server/WorkspaceWebDav.fs]]. Harness: [[scripts/stretch-workspace-upload.sh]].

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
bash scripts/client.sh build
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5215 \
  DB_CONNECTION_STRING='Host=localhost;Database=gambol;Username=gambol;Password=gambol_dev' \
  dotnet run --project src/Server --no-launch-profile
printf 'stretch-upload-1789649487-4850\n' > /tmp/ambit-stretch-upload/hello.md
dotnet run --project scripts/stretch-workspace-upload -- \
  http://127.0.0.1:5215/ambit /tmp/ambit-stretch-upload stretch
```

Harness steps (no WPF, no folder picker): GET `/ambit` for `gambol_auth` (empty Development Auth + mailbox login); GET `/ambit/state?scope=full`; mint Change (`eventId` 0, authority `Browser`, command `Load`); POST stubs from `WorkspaceLocalInventory.listForUpload` + `WorkspaceUploadStructure.planStubOps`; `WorkspaceFileSync.post`; `planServerFilePresentOps` Unparsed.

Browser: open `/ambit?debug=1`; select `stretch`; Load; ArrowRight.

## Evidence

Harness stdout: `PASS workspace-created eventId=1`; `PASS stubs stub eventId=2`; `PASS upload uploaded=1 … paths=hello.md`; `PASS nodes … file:hello.md,workspace:stretch`. After mark, `GET /state?scope=full` `eventId=3`.

CDP row dump after unfold: Workspaces; Workspace `stretch`; File `hello.md` class `amb-row-sync-unparsed`.

## Blockers (none fatal)

- Development json reconnects to `postgres`/`postgres` after env load. Local override only.
- `Thoth.Json.JavaScript` is Fable dummy on .NET. Harness encodes with `Thoth.Json.Newtonsoft` (same as Shared.Tests).
- `git` not on PATH. Upload still ran; detail said `.gitignore filter skipped (git unavailable)`.
- RootClosure boot shows the named Workspace as a header. File children need Load, then unfold.
- Cookie is `Secure`. Chrome on `localhost` HTTP accepted it. The harness sends `Cookie` on each request (does not use a Secure cookie jar).
- Cursor `computerUse` did not start (model usage). Headed Chrome + CDP on `DISPLAY=:1` is the Browser proof.
