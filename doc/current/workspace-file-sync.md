# Workspace file sync

Category: Capability

See Also

[Workspace local mapping](workspace-local-mapping.md)
[Desktop local files](desktop-local-files.md)
[Workspace graph](workspace-graph.md)
[Multi-client sync](sync-mvp.md)
[Gambol.Desktop](gambol-desktop.md)
[Gambol.Server](gambol-server.md)
[Gambol.Shared.DotNet](gambol-shared-dotnet.md)
[Workspace WebDAV](doc/roadmap/workspace-webdav.md)
Server HTTP surface and the required `getlastmodified` listings.

[Workspace upload client structure](doc/roadmap/workspace-upload-client-structure.md)

Client-first stubs and bulk caps.

[Lazy load](doc/roadmap/lazy-load.md)

Reconcile remains for web and for repair.

Workspace file sync transfers a tree between an App folder and Server `DataDir/{label}/` over WebDAV Class 1.

## Job

[x] Upload (`Ctrl+Shift+>`) ensures a map, then creates client stubs, then pushes bodies over WebDAV with prepare-push and finish-commit. Ensure-map: pick-folder plus Put when the label is unmapped. Workspaces focus creates a named Workspace from the folder basename. File focus then Parses.
[x] Download (`Ctrl+Shift+<`) ensures a map, then enqueues `workspace-download`. Target: a named Workspace, Directory, or File. Target: not ROOT and not Workspaces.
[x] ROOT and Workspaces cannot acquire a mapping.
[x] Focus sets the scope. Workspace: the whole tree. Directory: a relative prefix. File: one path.

## Transport

[x] Client Upload and Download do not use a remote and do not use smart-HTTP pack transport.
[x] Server mount: `/ambit/dav/{label}/…` on `DataDir/{label}/`.
[x] Class 1 methods in use: `PROPFIND`, `GET`, `PUT`, and `MKCOL`.
[x] After a Push batch, the desktop calls `_prepare-push`, then `_finish-commit`, on that mount.
[x] `_prepare-push` runs `WorkspaceGit.jitCommitBeforeWorkspacePush` when DataDir is dirty.
[x] `_finish-commit` adds and commits through [WorkspaceGit.fs](src/Server/WorkspaceGit.fs) and [GitSave.fs](src/Server/GitSave.fs), so Server `HEAD` advances.
[x] Upload and Download use the same `/ambit` session auth as the other app routes. They do not use a separate PAT.

## Inventory

[x] Inventory: the candidate path list for a scoped transfer, before bytes move.
[x] Ignore filtering runs on that list. The inventory source depends on the direction.
[x] Upload inventory: a local walk under the mapped scope: Workspace, Directory prefix, or File. Then `git check-ignore --no-index` runs against the mapped root. The remaining paths may upload.
[x] Download inventory: a Server `PROPFIND` under `/ambit/dav/{label}/…` for the same scope. DataDir check-ignore reduces the listing. The remaining paths may download.
[x] Transfer always skips `.git/`, with no dependence on ignore rules.
[x] A `.gitignore` file itself stays transferable. The same exception is in [IgnoredDestination.fs](src/Server/IgnoredDestination.fs).
[x] Upload ignore source of truth: the local mapped tree. `GIT_WORK_TREE`: the mapped root.
[x] Download ignore source of truth: the rules in Server `DataDir/{label}/`, applied when the server builds `PROPFIND`.
[x] The server rejects `PUT` to an ignored destination.
[x] Inventory code: [WorkspaceLocalInventory.fs](src/Shared/dotnet/WorkspaceLocalInventory.fs), [GitCheckIgnore.fs](src/Shared/dotnet/GitCheckIgnore.fs), and [IgnoredDestination.fs](src/Server/IgnoredDestination.fs).
[x] When the desktop ignore-filter binary is not on PATH, Upload still walks the scope and still skips `.git/`. The command does not fail. The result detail says the filter was skipped.
[x] WebDAV transfers an exact `.amb` file, so Upload and Download keep the Directory File body.

## Upload

[x] Desktop `post` runs JIT `_prepare-push`, then local inventory, then classify and plan, then client Directory and File stubs, then eligible WebDAV `PUT` bodies smallest-first, then `_finish-commit`.
[x] The `PUT` sends the local mtime on `X-Gambol-Source-Mtime`.
[x] An eligible bulk file is at most 1 MiB. Only eligible files and eligible bytes count toward the caps.
[x] At most 1,500 eligible files and 16 MiB of eligible bytes keep the full structure, and the client uploads every eligible body.
[x] Over either cap, the client keeps immediate-child structure and uploads the eligible top-level bodies.
[x] An oversized selected file keeps a `NoServerFile` stub. The client transfers no body for that file.
[x] A direct single-file Upload keeps a 4 MiB body limit and does not skip on mtime.
[x] A new File stub shows `∅` until PUT or an mtime skip confirms a server body, then shows `…` until Parse.
[x] Desktop Upload does not run a post-upload disk-to-graph reconcile.
[x] Code: [WorkspaceFileSync.fs](src/Shared/dotnet/WorkspaceFileSync.fs) `post`, [WorkspaceDavClient.fs](src/Shared/dotnet/WorkspaceDavClient.fs), [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs), and `POST /_desktop/workspace-push`.

## Download

[x] Download fetches every non-ignored directory and every non-ignored file in the selected server scope.
[x] Upload bulk caps do not restrict Download.
[x] The Download command enqueues a desktop job. The command does not block the Browser on the transfer.
[x] Enqueue: `POST /_desktop/workspace-download` with `{ label, relative, kind }`.
[x] Status: `GET /_desktop/workspace-download?id=…`.
[x] The queue holds one Running job and at most one Queued job. The queue refuses a third enqueue.
[x] Stage path: `%TEMP%/gambol-dl-tmp/{jobId}`. The job then promotes files into the mapped root.
[x] The client sets the local file mtime from the `PROPFIND` value `getlastmodified`.
[x] Code: [WorkspaceDownloadManager.fs](src/Desktop/WorkspaceDownloadManager.fs), [WorkspaceDownloadQueue.fs](src/Shared/dotnet/WorkspaceDownloadQueue.fs), and [WorkspaceFileSync.fs](src/Shared/dotnet/WorkspaceFileSync.fs) `getStaged`.
[x] `POST /_desktop/workspace-pull` still runs a blocking scoped pull on the same Get path for a mapped label.
[x] The Download command uses the manager. The Download command does not use that blocking route.

## Ledger

[x] Per-path ledger: `%LocalAppData%/Gambol/sync-ledger-{label}.json`, beside the mappings file `config.json`.
[x] The first scoped Upload or Download seeds the ledger from a full-workspace `PROPFIND` plus the local inventory.
[x] A later scoped sync updates only the touched rows.
[x] For a Directory scope or a Workspace scope, skip-if-newer uses UTC. Upload skips PUT when the server mtime is the same as the local mtime or newer. Download skips GET when the local mtime is the same as the server mtime or newer.
[x] File scope always allows the transfer.
[x] A skipped Upload still reparses.
[x] `MKCOL` stays idempotent. Directory mtime is not a skip input.
[x] After a successful Upload or Download, the client file, the server file, and the graph node share one datestamp when stamps apply.
[x] A ledger row also stores `presence`, `lastOp` (`seed`, `upload`, or `download`), and `lastServerHead` when finish-commit returns that head.
[x] Code: [WorkspaceSyncLedger.fs](src/Shared/dotnet/WorkspaceSyncLedger.fs). `POST /_desktop/workspace-sync-ledger` returns ledger rows for a mapped label.

## Auto-download

[x] A persisted change carries `SetUpdateTime` stamp ops on a rewritten document-root node.
[x] The Browser reuses those stamps to refresh a mapped App folder.
[x] An own edit uses the stamp ops on `SubmitResponse`.
[x] A remote edit uses the same stamp ops on an applied poll Change.
[x] Shared coalesce keeps at most one job per label, for a File, the nearest Directory, or the whole Workspace, so the manager cap holds.
[x] The Browser sends `POST /_desktop/workspace-download` and does not wait. There is no folder picker. A label with no mapping is dropped.
[x] The path runs only when `DesktopCapabilities.canWorkspaceSync` is true. Plain web does nothing on this path.
[x] The auto path does not poll the job. The auto path does not post a stamp-align Change.

## Layout

[x] `DataDir/{label}/`: the workspace work tree. The directory name is the label, verbatim.
[x] `.git/` under that tree: Server tracking only.
[x] A Directory File `.amb` may sit in that tree.
[x] The desktop mapping points the label at a separate absolute directory.
