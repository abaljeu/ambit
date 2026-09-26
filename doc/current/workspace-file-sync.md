# Workspace file sync

Category: Sync
See also: [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]], [[doc/current/workspace-graph.md]], [[doc/current/sync-mvp.md]], [[plan/transport-layer/project.md]], [[plan/auto-download-persisted-files/project.md]], [[doc/roadmap/workspace-webdav.md]], [[doc/roadmap/workspace-upload-client-structure.md]], [[plan/github-transport/project.md]]

Implemented baseline for App folder ↔ Server `DataDir/{label}/` tree transfer. Transport is WebDAV Class 1. This is not Graph Sync ([[doc/current/sync-mvp.md]]). This is not send to and from GitHub ([[plan/github-transport/project.md]]).

Mapping a Workspace label to a local folder is [[doc/current/workspace-local-mapping.md]]. Desktop host endpoints are [[doc/current/desktop-local-files.md]]. Directory File `.amb` as a graph artifact is [[doc/current/workspace-graph.md]].

## 1. Transport

Client Upload and Download do not use remotes or smart-HTTP pack transport. The Server mount is `/ambit/dav/{label}/…` onto `DataDir/{label}/`. Class 1 methods in use: `PROPFIND`, `GET`, `PUT`, `MKCOL`. Server HTTP surface and required `getlastmodified` listings: leftover [[doc/roadmap/workspace-webdav.md]].

After a Push batch the desktop calls `_prepare-push` then `_finish-commit` on that mount. `_prepare-push` runs `WorkspaceGit.jitCommitBeforeWorkspacePush` when DataDir is dirty. `_finish-commit` add/commits through [[src/Server/WorkspaceGit.fs]] / [[src/Server/GitSave.fs]] so Server `HEAD` advances.

Same `/ambit` session auth as other app routes. No separate PAT for Upload or Download.

## 2. Inventory and ignore

**Inventory** is the candidate path list for a scoped transfer before bytes move. Ignore filtering runs on that list. The inventory **source** differs by direction.

| Direction | Inventory source | Then |
| --- | --- | --- |
| **Upload** | Local walk under the mapped scope (Workspace, Directory prefix, or File) | `git check-ignore --no-index` against the mapped root; remaining paths may upload |
| **Download** | Server `PROPFIND` under `/ambit/dav/{label}/…` for the same scope | DataDir check-ignore reduces the listing; remaining paths may download |

Always skip `.git/` regardless of ignore rules. `.gitignore` files themselves stay transferable (same exception as [[src/Server/IgnoredDestination.fs]]).

Upload ignore source of truth is the local mapped tree (`GIT_WORK_TREE` = mapped root). Download ignore source of truth is Server `DataDir/{label}/` rules applied when building `PROPFIND`. Server also rejects `PUT` to an ignored destination.

Code: [[src/Shared/dotnet/WorkspaceLocalInventory.fs]], [[src/Shared/dotnet/GitCheckIgnore.fs]], [[src/Server/IgnoredDestination.fs]].

If the desktop ignore-filter binary is not on PATH, Upload still walks the scope and still skips `.git/`. It does not fail the command. The result detail says the filter was skipped.

WebDAV still transfers an exact `.amb` file so Upload and Download keep the Directory File body. Graph consumers treat that file as the persistence artifact of the containing Directory or Workspace. DAV inventory must not create a child File Node named `.amb`.

## 3. Upload

**Post** on the desktop: JIT `_prepare-push` → local inventory → classify/plan → client Directory / File stubs → eligible WebDAV `PUT` bodies smallest-first (local mtime on `X-Gambol-Source-Mtime`) → `_finish-commit`.

Client-first stubs and bulk caps: leftover [[doc/roadmap/workspace-upload-client-structure.md]]. Implemented caps that the planner uses:

- Eligible bulk file is `≤1 MiB`. Only eligible files and bytes count toward caps.
- At most 1,500 eligible files and 16 MiB eligible bytes keep full structure and upload every eligible body.
- Over either cap: keep immediate-child structure and upload eligible top-level bodies.
- Oversized selected file: keep a `NoServerFile` stub; transfer no body.
- Direct single-file Upload keeps a 4 MiB body limit and does not mtime-skip.

New File stubs show `∅` until PUT or mtime skip confirms a server body, then `…` until Parse. Desktop Upload does not run post-upload disk→graph reconcile. Reconcile remains for web / repair ([[doc/roadmap/lazy-load.md]]).

Code: [[src/Shared/dotnet/WorkspaceFileSync.fs]] `post`, [[src/Shared/dotnet/WorkspaceDavClient.fs]], [[src/Shared/dotnet/WorkspaceCloudUpload.fs]], `POST /_desktop/workspace-push`.

## 4. Download

Download fetches every non-ignored directory and file in the selected server scope. Upload bulk caps do not restrict Download.

The Download command enqueues a desktop job. It does not block the Browser on the transfer.

| Piece | Behavior |
| --- | --- |
| Enqueue | `POST /_desktop/workspace-download` with `{ label, relative, kind }` |
| Status | `GET /_desktop/workspace-download?id=…` |
| Queue | One **Running** job and at most one **Queued** job; a third enqueue is refused |
| Stage | `%TEMP%/gambol-dl-tmp/{jobId}` then promote into the mapped root |
| Mtime | Set local file mtime from `PROPFIND` `getlastmodified` |

Code: [[src/Desktop/WorkspaceDownloadManager.fs]], [[src/Shared/dotnet/WorkspaceDownloadQueue.fs]], [[src/Shared/dotnet/WorkspaceFileSync.fs]] `getStaged`.

`POST /_desktop/workspace-pull` still runs a blocking pull on the same Get path. The Download command uses the manager, not that blocking route.

## 5. Sync ledger and mtime skip

Per-path ledger: `%LocalAppData%/Gambol/sync-ledger-{label}.json` beside mappings (`config.json`). Seeded on first scoped Upload or Download from full-workspace `PROPFIND` plus local inventory. Later scoped syncs update only touched rows.

**Skip-if-newer (UTC), Directory or Workspace scope:** Upload skips PUT when server mtime is the same or newer than local. Download skips GET when local mtime is the same or newer than server. **File scope:** always allow transfer.

Skipped Upload still reparses. Directories: `MKCOL` stays idempotent; directory mtime is not used for skip. After a successful Upload or Download, client file, server file, and graph node share the same datestamp when stamps apply.

Ledger rows also store `presence` and `lastOp` (`seed` / `upload` / `download`) and `lastServerHead` when finish-commit returns it. Selective delete propagation is not implemented.

Code: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]], `POST /_desktop/workspace-sync-ledger`.

## 6. Auto-download on persist

Persisted changes carry `SetUpdateTime` stamp ops on rewritten document-root nodes. The Browser reuses those stamps to refresh a mapped App folder:

- Own edits: `SubmitResponse` stamp ops.
- Remote edits: applied poll Changes with the same stamp ops.
- Shared coalesce keeps at most one job per label (File, nearest Directory, or whole Workspace) so the manager cap holds.
- Fire-and-forget `POST /_desktop/workspace-download`. No folder picker. Labels without a mapping are dropped.
- Gated on `DesktopCapabilities.canWorkspaceSync`. Plain web is a no-op.
- The auto path does not poll the job and does not post a stamp-align Change.

HITL checks remain tabled on [[plan/auto-download-persisted-files/project.md]].

## 7. Command surface

| Intent | Target |
| --- | --- |
| Upload (`Ctrl+Shift+>`) | Ensure map (pick-folder + Put when unmapped) → client stubs → WebDAV body push + prepare-push + finish-commit. Workspaces focus creates a named Workspace from the folder basename. File focus then Parses. |
| Download (`Ctrl+Shift+<`) | Ensure map → enqueue `workspace-download`. Named Workspace / Directory / File only (not ROOT or Workspaces). |

Standalone Map, Connect, Clone, pack Push, and Status are removed from the palette.

ROOT and Workspaces cannot acquire a mapping. Focus sets scope: Workspace → whole tree; Directory → relative prefix; File → one path.

## 8. On-disk layout

```text
{DataDir}/
  home/                  ← workspace work tree (verbatim label)
    .git/                ← Server tracking only; never transferred
    src/
      lib.fs
    doc/
      specs/
        .amb
```

Desktop mapping points label `home` at a separate absolute directory. That folder need not be a git clone.

## 9. Not implemented

These are not in the current program. They are not product exclusions.

- Overwrite / freshness UI beyond `#cmd-last-result`.
- Mirror-delete (`DELETE`) / WebDAV Class 2.
- Expand-to-parse and richer freshness metadata.
- Selective delete propagation from ledger `presence` / `lastOp`.

Forward GitHub remote pull/push stays on [[plan/github-transport/project.md]].
