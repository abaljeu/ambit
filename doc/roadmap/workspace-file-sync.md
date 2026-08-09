# Workspace file sync (WebDAV + server git)

Category: Sync
Status: Partial
See also: [[workspace-webdav]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]], [[workspace-scale-import]], [[workspaces-checklist]], [[lazy-load]], [[workspace-upload-client-structure]], [[doc/roadmap/workspace-file-persistence]], [[src/Server/IgnoredDestination.fs]], [[src/Server/WorkspaceGit.fs]], [[src/Server/GitSave.fs]]

Committed direction for synchronizing a desktop mapped folder with server `DataDir/{label}/`. **Client Upload / Download do not use remotes or smart-HTTP pack transport.** Transport is WebDAV Class 1. The server still keeps a per-workspace git repo for ignore rules, pre-push JIT, and post-push commits.

This does not supersede [[doc/current/sync-mvp]] for live graph editing over HTTP; file sync is coarse, explicit tree sync layered on top.

## What it gives you

1. **Upload** — ensure a local folder mapping for the focused named workspace (pick-folder + Put when unmapped), create client-first Directory/File stubs, then scoped WebDAV body push, server JIT-commit before the batch, and finish-commit after. New File stubs show `∅` until PUT or mtime skip confirms a server body, then `…` until Parse. There is no post-upload reconcile on the Desktop path; disk→graph reconcile remains for web / repair ([[workspace-upload-client-structure]]). On **Workspaces** focus: pick folder → create named workspace from folder basename → map → push. On **File** focus: push file scope then Parse.
2. **Download** — ensure mapping, then enqueue a desktop download job (`POST /_desktop/workspace-download`); the manager pulls every non-ignored path in the selected server scope, stages under `.gambol-dl-tmp/`, promotes atomically, and preserves file mtimes from PROPFIND. Named Workspace / Directory / File only (not ROOT or Workspaces). Client does not wait or reconcile.
3. Never transfer `.git/` or ignored paths (for example `.venv`).
4. **Removed from command surface:** standalone Map workspace, Connect, Clone, pack Push, Status.

## Inventory (defined)

**Inventory** means the candidate path list for a scoped sync before transfer — not a vague “tree walk.” Ignore filtering always runs on that candidate list; the inventory **source** differs by direction:

| Direction | Inventory source | Then |
| --- | --- | --- |
| **Upload (push)** | Local walk/select under the mapped scope (workspace, subdirectory, or file) | Apply `.gitignore` → remaining paths are uploaded |
| **Download (pull)** | Server `PROPFIND` under `/ambit/dav/{label}/…` for the same scope | Apply `.gitignore` to that listing → remaining paths are downloaded |

So ignore reduces candidates on **both** directions. For Download, the inventory source is the **server**; ignore rules then reduce it. Always skip `.git/` regardless of ignore rules.

## Desktop sync functions

The desktop layer’s two main sync functions:

1. **Post (Upload)** — JIT prepare-push → local scope → GitCheckIgnore → bulk/direct Upload plan → eligible WebDAV `PUT` bodies smallest-first (file mtime via `X-Gambol-Source-Mtime`) → finish-commit.
2. **Get (Download)** — server `PROPFIND` inventory → stage under `.gambol-dl-tmp/{jobId}` → GET every file body → promote dirs atomically → set local file mtime from `getlastmodified`. Used by the desktop download manager (not blocking client pull).

## What it avoids for now

- Removing existing `GitGateway` / `/ambit/git/…` pack code (demote from UX only; not the Upload/Download path).
- Full WebDAV Class 2, DeltaV, locks, Windows drive mapping.
- Zip / rsync / delta sync, conflict UI, mirror-delete (`DELETE` deferred).
- Writing a pure `.gitignore` parser (the ignore filter still uses `git check-ignore`).
- Fast-forward / remote-tracking / client-side merge as the sync model.

## Decision

| Topic | Decision |
| --- | --- |
| Client transport | WebDAV Class 1 under `/ambit/dav/{label}/…` → `DataDir/{label}/` |
| Server git | Keep tracking; after a push batch, commit via [[src/Server/WorkspaceGit.fs]] / [[src/Server/GitSave.fs]] |
| `.gitignore` | Essential; both directions filter candidates with `git check-ignore` (reuse [[src/Server/IgnoredDestination.fs]] pattern) |
| Ignore SoT (Upload) | **Local mapped tree** ignore rules (`GIT_WORK_TREE` = mapped root) |
| Ignore SoT (Download) | **Server `DataDir/{label}/`** ignore rules — applied when building/filtering `PROPFIND` results |
| Mapping | Unchanged: label → absolute local path ([[doc/current/workspace-local-mapping]]) |
| Scope | Focus determines workspace whole tree, directory relative prefix, or single file |
| Overwrite | Last-write-wins in scope; no FF; no delete of extras on either side in v1 |
| Auth | Same as other `/ambit` routes (no separate PAT for Upload/Download) |
| Upload concurrency | Pipelined `PUT`/`MKCOL` in depth waves (dirs by depth, then files); cap ~12 in flight — not zip/tar batching |
| Desktop Upload structure | Client builds selected Directory stubs and `NoServerFile` File stubs from inventory; PUT/mtime presence changes Files to Unparsed; no post-upload reconcile or TreeStructure placeholders — [[workspace-upload-client-structure]] |
| Web / repair stubs | Disk→graph reconcile from DataDir remains ([[lazy-load]]) |

## How `.gitignore` is followed

Ignore filtering applies to the **candidate inventory** on both directions. Single source of truth:

- **Upload inventory** — local mapped tree ignore rules.
- **Download inventory** — server DataDir ignore rules (prefer server applying check-ignore when emitting `PROPFIND`; desktop may also filter if it has a mapped tree, but must not redefine Download’s authoritative ignore set).

| Where | Mechanism |
| --- | --- |
| **Desktop Upload** | Walk/select scoped local path; always skip `.git/`; filter with `git check-ignore --no-index` against the mapped work tree (same idea as IgnoredDestination: `GIT_WORK_TREE` + temp/shared `GIT_DIR` so the folder need not be a client clone). Fail Upload if the ignore filter is unavailable or check-ignore errors — do not silently upload ignored trees. Then `PUT` / `MKCOL`, then finish-commit. |
| **Server Download inventory (`PROPFIND`)** | Build listing from `DataDir/{label}/` under scope; skip `.git/`; **apply check-ignore against the server work tree** so the multistatus is already the reduced file list. Prefer this as the only required Download filter. |
| **Desktop Download (optional)** | May re-filter the `PROPFIND` list with local mapped-tree check-ignore if a mapping exists; optional belt-and-suspenders, not the Download SoT. |
| **Server `PUT`** | Reject PUT to an ignored destination (same DataDir rules). |
| **Server commit** | Normal `git add` honors workspace `.gitignore` / excludes. |
| **`.gitignore` files themselves** | Still transferable (not treated as ignored destinations for the ignore-file path) — same exception as IgnoredDestination. |

**Not required for Upload/Download transport:** local folder is a git repo, `ambit` remote, or `git.git` capability for pack transport.

**Required on desktop for Upload:** ignore-filter binary on PATH (`git check-ignore`). Ensure-map and Download can work without it when the server filters `PROPFIND`; Upload must not.

## WebDAV subset (v1)

Server mount, Class 1 methods, and **PROPFIND properties** (including required `getlastmodified` / mtime): [[workspace-webdav]].

| Method | Use |
| --- | --- |
| `PROPFIND` | Server-side inventory for Download (Depth 1 or infinity) under `/ambit/dav/{label}/…` — must expose path, collection vs file, and **datestamp**; omit ignored paths |
| `GET` | Download each remaining candidate |
| `PUT` | Upload / overwrite file (create parent dirs as needed) |
| `MKCOL` | Create directory |
| `DELETE` | Deferred (no mirror-delete in v1) |
| `LOCK` / DeltaV | Out of scope |

## Command surface

| Intent | Target |
| --- | --- |
| Upload (`Ctrl+Shift+>`) | Ensure map (pick-folder if needed) → client stubs → WebDAV body push + JIT + finish-commit; Workspaces focus creates workspace from folder; File focus transitions body presence then Parses |
| Download (`Ctrl+Shift+<`) | Ensure map → `POST /_desktop/workspace-download` (returns immediately; manager runs pull) |

## Upload limits (locked)

| Trigger | Bulk Workspace/Directory Upload |
| --- | --- |
| Eligible file | `≤1 MiB`; only eligible files and bytes count toward caps |
| ≤ **1,500 eligible files** and ≤ **16 MiB eligible bytes** | Keep full structure and upload every eligible body |
| Over either cap | Keep immediate-child structure and upload every eligible top-level body regardless of aggregate cap |
| Oversized selected file | Keep `NoServerFile` stub; transfer no body |

Direct single-file Upload retains the 4 MiB body limit and never mtime-skips. Body PUTs run smallest-first with path as a stable tie-break. Upload sends local file mtime on PUT; server sets `LastWriteTimeUtc`.

## Download is unlimited

Download fetches every non-ignored directory and file in the selected server scope. Full / TreeStructure / TopLevel modes, per-file placeholders, and the 1 MiB / 4 MiB / 16 MiB / 1,500 Upload limits do not restrict Download. Ignore filtering, staging and atomic promotion, mtime preservation, ledger freshness skips, and overwrite rules remain unchanged.

## Download manager (locked)

- One **Running** + at most one **Queued** job; further enqueue → refuse (retry later).
- Endpoints: `POST /_desktop/workspace-download` (enqueue), `GET /_desktop/workspace-download?id=…` (status).
- Client Download: ensure map → POST → `okDetail`; no wait, no reconcile, no progress UI.

## Auto-download on persist

Persisted changes already carry `SetUpdateTime` stamp ops naming the rewritten document-root nodes ([[src/Server/DocumentPersistence.fs]], `PersistStamp` in [[src/Shared/History.fs]]). The Browser MVU loop reuses those stamps to keep the App's mapped folder current without any extra command:

- Own edits: the `SubmitResponse` handler in [[src/Client/Update.fs]] extracts affected File targets from `stampOps`.
- Remote edits: the `PollDone` handler extracts them from the applied poll `Change list` (same `SetUpdateTime` ops).

Extraction and coalescing are pure Shared logic: `WorkspaceUploadStructure.autoDownloadFileTargets` resolves each stamped node id to a File `WorkspaceSyncScope`, and `WorkspaceSyncScope.coalesceDownloadTargets` reduces the pending `(label, relative)` set to **at most one job per label** — one file → File scope; several → nearest common Directory, else the whole Workspace (matching the manager's 1 running + 1 queued cap).

The client seam is a `pendingAutoDownloads` VM field plus a debounced `AutoDownloadTick` (mirrors the `PollTick` timer in [[src/Client/App.fs]]). Both handlers accumulate targets and arm the tick; the tick coalesces, keeps only labels with an existing mapping (read-only `lookupMappedPath`; never `pickFolder`/`ensureMapped`), fires `POST /_desktop/workspace-download` fire-and-forget, and clears the field.

Gated on `DesktopCapabilities.canWorkspaceSync` (plain web is a no-op). **No feedback loop:** the auto path never polls the job and never posts a stamp-align `Change`, because persist already made server file mtime = graph node `updateTime` and WebDAV download preserves that mtime onto the local file (local == server == graph). A stamp-only change therefore produces no new triggering `stampOps`.

## Sync ledger + mtime skip (shipped)

Per-path ledger in `%LocalAppData%/Gambol/sync-ledger-{label}.json` beside mappings (`config.json`). Seeded on first scoped Upload/Download from full-workspace PROPFIND + local inventory. Later scoped syncs update only touched rows.

**Skip-if-newer (UTC), directory scope:** Upload skips PUT when server ≥ local; Download skips GET when local ≥ server. **Single-file scope:** always allow transfer (do not skip for newer/same target). Skipped Upload still reparses. Locked detail: [[workspace-upload-client-structure]]. Directories: MKCOL remains idempotent (405 ok); dir mtime not tracked. After successful Upload or Download, client file, server file, and graph node share the same datestamp.

After successful transfer, ledger row gets current local/server mtimes and `lastServerHead` when finish-commit returns it. Rows include `presence` and `lastOp` for future selective delete propagation.

**Deferred:** selective delete propagation — no mirror-delete either direction (retain extras).

| Map / Connect / Clone / pack Push / Status | **Removed** from palette and desktop pack UX |

ROOT and Workspaces cannot acquire a mapping. Focus determines scope: workspace → whole tree; directory → relative prefix; file → one path.

## Minimal API / ops

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
sequenceDiagram
  participant UI as Client
  participant Desk as Desktop
  participant Dav as Server_WebDAV
  participant Git as Server_WorkspaceGit

  UI->>Desk: Ensure map (pick-folder + PUT if needed)
  Desk->>Dav: POST prepare-push (JIT dirty DataDir)
  Dav->>Git: jitCommitBeforeWorkspacePush
  UI->>Desk: Upload local scope
  Desk->>Desk: walk scope skip .git check-ignore
  UI->>UI: create selected Directory and NoServerFile stubs
  loop eligible bodies smallest-first
    Desk->>Dav: PUT /ambit/dav/label/rel
  end
  Desk->>Dav: POST finish commit
  Dav->>Git: ensureInit add commit
  UI->>UI: present bodies become Unparsed; direct File Parses
  UI->>Desk: Download scoped get
  Desk->>Dav: PROPFIND scope
  Dav->>Dav: check-ignore DataDir filter listing
  Dav-->>Desk: inventory minus ignored
  loop each remaining file
    Desk->>Dav: GET
    Desk->>Desk: write under mapped root
  end
```

- **Pre-push JIT:** `_prepare-push` commits dirty DataDir before the WebDAV batch (`jitCommitBeforeWorkspacePush`).
- **End-of-push commit:** explicit finish endpoint after the batch.
- **Capabilities:** Upload / Download need `workspacePaths` plus file import/export, **not** `git.git` for pack transport. Upload still needs the ignore-filter binary for local ignore.
- **Lazy Load:** disk→graph reconcile remains for web / repair. See [[lazy-load]].
- **Desktop Upload structure:** client stubs replace post-upload reconcile on the Desktop path — [[workspace-upload-client-structure]].

## On-disk layout

```text
{DataDir}/
  home/                  ← workspace work tree (verbatim label)
    .git/                ← server-side tracking only; never transferred
    src/
      lib.fs
    docs/
      specs/
        .amb
```

Desktop mapping ([[doc/current/workspace-local-mapping]]) points label `home` at a separate absolute directory. That folder need not be a clone.

## Status vs code

Shipped:

- [x] Shared scope / path helpers + Shared.Tests.
- [x] Shared `GitCheckIgnore` for desktop Upload inventory and server WebDAV `PROPFIND` / `PUT`.
- [x] Server WebDAV Class 1 + DataDir check-ignore on `PROPFIND` + `_prepare-push` / `_finish-commit` ([[workspace-webdav]]).
- [x] Desktop `/_desktop/workspace-push` / `workspace-pull` / `workspace-download` (HttpClient WebDAV + download manager).
- [x] Client Upload / Download with ensure-map; Workspaces create-from-folder; file Parse after Upload; ungated from pack transport.
- [x] Client-first Desktop Upload stubs and explicit `NoServerFile` → Unparsed body-presence transitions; web / repair reconcile retained.
- [x] Sync ledger + mtime skip (Upload/Download); selective delete still deferred.

Still open (see [[workspaces-checklist]], [[lazy-load]], [[workspace-upload-client-structure]]):

- [ ] Overwrite / freshness UI beyond `#cmd-last-result`.
- [ ] Expand-to-parse and richer freshness metadata.
- [ ] Mirror-delete / Class 2.

## Tests

- Push inventory excludes `.venv` / nested gitignore cases via check-ignore against the mapped tree.
- Server `PROPFIND` omits ignored entries (DataDir rules); ignored PUT rejected.
- Finish-commit moves HEAD; reconcile stubs.
- Upload fails clearly if the desktop ignore filter is unavailable.

## Success criteria

- Upload then Download a subdirectory without remotes or pack transport.
- Upload: local scope → check-ignore → PUT/MKCOL → finish-commit; `.venv` never uploaded.
- Download: server PROPFIND inventory → DataDir check-ignore → GET; listings never leak ignored trees.
- After Upload + finish, server repo has a new commit.
- Upload / Download do not call `/_desktop/git-push|pull|clone|remote|status` or `/ambit/git/…` pack transport.
