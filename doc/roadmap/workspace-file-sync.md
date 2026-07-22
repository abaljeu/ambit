# Workspace file sync (WebDAV + server git)

Category: Sync
Status: Planned
See also: [[workspace-webdav]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]], [[workspace-scale-import]], [[workspaces-checklist]], [[lazy-load]], [[doc/roadmap/workspace-file-persistence]], [[src/Server/IgnoredDestination.fs]], [[src/Server/WorkspaceGit.fs]], [[src/Server/GitSave.fs]]

Committed direction for synchronizing a desktop mapped folder with server `DataDir/{label}/`. **Client Upload / Download do not use git remotes or smart-HTTP pack transport.** Transport is WebDAV Class 1. The server still keeps a per-workspace git repo for ignore rules, pre-push JIT, and post-push commits.

This does not supersede [[doc/current/sync-mvp]] for live graph editing over HTTP; file sync is coarse, explicit tree sync layered on top.

## What it gives you

1. **Upload** — ensure a local folder mapping for the focused named workspace (pick-folder + Put when unmapped), then scoped WebDAV push (`PUT` / `MKCOL`), server JIT-commit of dirty DataDir before the batch, finish-commit after, and Lazy Load stub reconcile. On **Workspaces** focus: pick folder → create named workspace from folder basename → map → push whole tree. On **File** focus: push file scope then Parse.
2. **Download** — ensure mapping, then scoped WebDAV pull (`PROPFIND` inventory → `GET` into mapped root; listings include **getlastmodified** / mtime — [[workspace-webdav]]). Named Workspace / Directory / File only (not ROOT or Workspaces).
3. Never transfer `.git/` or gitignored paths (for example `.venv`).
4. **Removed from command surface:** standalone Map workspace, Git Connect, Clone, Git Push (pack), Git Status.

## Inventory (defined)

**Inventory** means the candidate path list for a scoped sync before transfer — not a vague “tree walk.” Ignore filtering always runs on that candidate list; the inventory **source** differs by direction:

| Direction | Inventory source | Then |
| --- | --- | --- |
| **Push** | Local walk/select under the mapped scope (workspace, subdirectory, or file) | Apply `.gitignore` → remaining paths are uploaded |
| **Pull** | Server `PROPFIND` under `/ambit/dav/{label}/…` for the same scope | Apply `.gitignore` to that listing → remaining paths are downloaded |

So ignore reduces candidates on **both** directions. For Pull, the inventory source is the **server**; gitignore then reduces it. Always skip `.git/` regardless of ignore rules.

## Desktop sync functions

The desktop layer’s two main sync functions:

1. **Post (Upload)** — JIT prepare-push → local scope → GitCheckIgnore → WebDAV `PUT` / `MKCOL` → finish-commit.
2. **Get (Download)** — server `PROPFIND` inventory → GitCheckIgnore → `GET`.

## What it avoids for now

- Removing existing `GitGateway` / `/ambit/git/…` code in this slice (demote from UX only; not the Map/Push/Pull path).
- Full WebDAV Class 2, DeltaV, locks, Windows drive mapping.
- Zip / rsync / delta sync, conflict UI, mirror-delete (`DELETE` deferred).
- Writing a pure `.gitignore` parser (the `git` binary remains the ignore engine).
- Changing per-file Parse / Upload behavior for Unparsed File focus.
- Fast-forward / remote-tracking / client-side merge as the sync model.

## Decision

| Topic | Decision |
| --- | --- |
| Client transport | WebDAV Class 1 under `/ambit/dav/{label}/…` → `DataDir/{label}/` |
| Server git | Keep tracking; after a push batch, commit via [[src/Server/WorkspaceGit.fs]] / [[src/Server/GitSave.fs]] |
| `.gitignore` | Essential; both directions filter candidates with `git check-ignore` (reuse [[src/Server/IgnoredDestination.fs]] pattern) |
| Ignore SoT (Push) | **Local mapped tree** ignore rules (`GIT_WORK_TREE` = mapped root) |
| Ignore SoT (Pull) | **Server `DataDir/{label}/`** ignore rules — applied when building/filtering `PROPFIND` results |
| Mapping | Unchanged: label → absolute local path ([[doc/current/workspace-local-mapping]]) |
| Scope | Focus determines workspace whole tree, directory relative prefix, or single file |
| Overwrite | Last-write-wins in scope; no FF; no delete of extras on either side in v1 |
| Auth | Same as other `/ambit` routes (no separate git PAT for Map/Push/Pull) |

## How `.gitignore` is followed

Ignore filtering applies to the **candidate inventory** on both directions. Single source of truth:

- **Push inventory** — local mapped tree ignore rules.
- **Pull inventory** — server DataDir ignore rules (prefer server applying check-ignore when emitting `PROPFIND`; desktop may also filter if it has a mapped tree, but must not redefine Pull’s authoritative ignore set).

| Where | Mechanism |
| --- | --- |
| **Desktop Push** | Walk/select scoped local path; always skip `.git/`; filter with `git check-ignore --no-index` against the mapped work tree (same idea as IgnoredDestination: `GIT_WORK_TREE` + temp/shared `GIT_DIR` so the folder need not be a client clone). Fail Push if `git` is missing or check-ignore errors — do not silently upload ignored trees. Then `PUT` / `MKCOL`, then finish-commit. |
| **Server Pull inventory (`PROPFIND`)** | Build listing from `DataDir/{label}/` under scope; skip `.git/`; **apply check-ignore against the server work tree** so the multistatus is already the reduced file list. Prefer this as the only required Pull filter. |
| **Desktop Pull (optional)** | May re-filter the `PROPFIND` list with local mapped-tree check-ignore if a mapping exists; optional belt-and-suspenders, not the Pull SoT. |
| **Server `PUT`** | Reject PUT to an ignored destination (same DataDir rules). |
| **Server commit** | Normal `git add` honors workspace `.gitignore` / excludes. |
| **`.gitignore` files themselves** | Still transferable (not treated as ignored destinations for the ignore-file path) — same exception as IgnoredDestination. |

**Not required for Map/Push/Pull transport:** local folder is a git repo, `ambit` remote, or `git.git` capability.

**Required on desktop for Push:** `git` on PATH (ignore only). Map and Pull can work without desktop `git` when the server filters `PROPFIND`; Push must not.

## WebDAV subset (v1)

Server mount, Class 1 methods, and **PROPFIND properties** (including required `getlastmodified` / mtime): [[workspace-webdav]].

| Method | Use |
| --- | --- |
| `PROPFIND` | Server-side inventory for Pull (Depth 1 or infinity) under `/ambit/dav/{label}/…` — must expose path, collection vs file, and **datestamp**; omit ignored paths |
| `GET` | Download each remaining Pull candidate |
| `PUT` | Upload / overwrite file (create parent dirs as needed) |
| `MKCOL` | Create directory |
| `DELETE` | Deferred (no mirror-delete in v1) |
| `LOCK` / DeltaV | Out of scope |

## Libraries

Nothing WebDAV-related is in the repo today. Server library spike and fallback: [[workspace-webdav]]. Desktop uses HttpClient for PROPFIND / GET / PUT / MKCOL. Ignore stays `git check-ignore` (IgnoredDestination-style helper).

## Command surface

| Intent | Target |
| --- | --- |
| Upload (`Ctrl+Shift+>`) | Ensure map (pick-folder if needed) → WebDAV push + JIT + finish-commit + reconcile; Workspaces focus creates workspace from folder; File focus then Parses |
| Download (`Ctrl+Shift+<`) | Ensure map → WebDAV pull for named Workspace / Directory / File |
| Map / Connect / Clone / Git Push / Status | **Removed** from palette and desktop pack routes |

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
  loop each remaining path
    Desk->>Dav: MKCOL and PUT /ambit/dav/label/rel
  end
  Desk->>Dav: POST finish commit
  Dav->>Git: ensureInit add commit
  UI->>Dav: POST reconciliation/directory
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
- **Capabilities:** Upload / Download need `workspacePaths` plus file import/export, **not** `git.git` for transport. Upload still needs the git binary for local ignore.
- **Lazy Load:** stub reconcile runs after WebDAV push + finish-commit (not after receive-pack). See [[lazy-load]].

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

Desktop mapping ([[doc/current/workspace-local-mapping]]) points label `home` at a separate absolute directory. That folder need not be a git clone.

## Implementation steps

1. Shared scope / path helpers + Shared.Tests.
2. Share check-ignore helper — extract / reuse IgnoredDestination-style API for desktop Push inventory and server WebDAV `PROPFIND` / `PUT`.
3. Server WebDAV — NWebDav spike or hand-roll; **PROPFIND applies DataDir check-ignore**; finish-commit; Server.Tests including ignore omission / rejection.
4. Desktop — Push: local walk + required check-ignore + HttpClient WebDAV; Pull: PROPFIND then GET (optional local re-filter).
5. Client commands — Upload / Download with ensure-map; Workspaces create-from-folder; file Parse after Upload; ungated from git-pack transport.
6. Post-push reconcile wiring + current-doc refresh when behavior lands.

## Tests

- Push inventory excludes `.venv` / nested gitignore cases via check-ignore against the mapped tree.
- Server `PROPFIND` omits ignored entries (DataDir rules); ignored PUT rejected.
- Finish-commit moves HEAD; reconcile stubs.
- Push fails clearly if `git` is missing on desktop.

## Success criteria

- Push then Pull a subdirectory without git remotes or pack transport.
- Push: local scope → check-ignore → PUT/MKCOL → finish-commit; `.venv` never uploaded.
- Pull: server PROPFIND inventory → DataDir check-ignore → GET; listings never leak ignored trees.
- After Push + finish, server repo has a new commit.
- Upload / Download do not call `/_desktop/git-push|pull|clone|remote|status` or `/ambit/git/…` pack transport.
