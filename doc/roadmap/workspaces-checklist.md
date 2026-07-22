# Workspaces checklist

Category: Workspace scale
See also: [[workspaces]], [[doc/current/workspace-graph]], [[doc/current/workspace-local-mapping]], [[workspace-file-sync]], [[workspace-webdav]]

Living checklist for implementing workspaces.  Mark done an item when it is done.

- Workspace = graph node type;
- 1:1 with `DataDir/{workspaceName}`;
- that directory is an independent git repo **on the server** (tracking + ignore + commits).
- Desktop holds a partial map `workspaceName` → local path; tree sync is WebDAV, not a client git remote ([[workspace-file-sync]]).

## Graph model

- [x] Workspace as a first-class graph node type and structural rules around it.
- [x] Files and Directories are only allowed to be in Directories or Workspaces. (Root is a workspace; Normal/Workspaces may intervene on the owner chain; File ancestor illegal; persistence-directory name uniqueness.) — plan: [[workspace-file-directory-placement]] (owner-chain placement; Slice B create/move cancelled; Slice C legacy reconcile cancelled)

## Server DataDir
One workspace folder per name under DataDir; ownership and on-disk layout.
- [x] Workspaces, directories and files may appear in DataDir on server. In graph, workspaces are in Workspaces subnode.  Files and directories will be in Root node.
- [x] Name collision prevention on new workspace/directory/file, and on rename or move directory/file, needs to account for the root folder structure.

## Server git tracking
Each server workspace directory is its own git repository under `DataDir/{label}/`. Used for `.gitignore` / `git check-ignore` and post-push commits — not for client pack transport. Direction: [[workspace-file-sync]].

- [x] Init empty repo in a new server directory
- [x] Commit all files to repo on server (WorkspaceGit / GitSave)
- [ ] Finish-commit after WebDAV push batch
- [x] Ignore via `git check-ignore` (IgnoredDestination pattern) — keep essential for Push / PROPFIND / PUT

## Workspace file sync (WebDAV)
Client Map / Push / Pull. Product authority: [[workspace-file-sync]]. Server DAV surface + PROPFIND datestamps: [[workspace-webdav]].

- [ ] Server WebDAV Class 1 under `/ambit/dav/{label}/…` (PROPFIND with getlastmodified / GET / PUT / MKCOL) — [[workspace-webdav]]
- [ ] PROPFIND exposes href/path, collection vs file, **getlastmodified** (mtime); optional getcontentlength
- [ ] Desktop push inventory with required check-ignore (fail if `git` missing)
- [ ] Client **Map workspace** (pick-folder + mapping Put; no remote setup)
- [ ] Client **Push** scoped to workspace / subdirectory / file → WebDAV + finish-commit + reconcile
- [ ] Client **Pull** scoped to workspace / subdirectory / file → WebDAV down (inventory uses listing mtimes)
- [ ] Ungate Map/Push/Pull from git-pack / `git.git` transport capability (Push still needs git binary for ignore)

## Desktop mapping
Partial `workspaceName` → absolute local path bindings on the desktop (see [[doc/current/workspace-local-mapping]]). Mapping does not require the local folder to be a git clone.
- [x] Config file load at desktop startup
- [x] Label / absolute-path validation
- [x] Resolve `//label/relative` under mapped root
- [x] File-status / import / export via mapped paths
- [x] Folder picker for local root
- [x] API Get/Put mapping on workspace

## Client UX
How users create, open, navigate, and work in workspaces in the UI (commands / keybound ops). Desktop mapping API and folder picker stay under Desktop mapping; Map / Push / Pull live under Workspace file sync.
- [x] **Insert…** (`f`): New Workspace (focus on Workspaces); New File / New Folder (elsewhere); pick existing file → insert Ref
- [x] **Rename** (`F2`) for Directory / File / Normal; workspace rename refused (immutable names)
- [x] **Delete** (move to TRASH), **Move Selected** (`m`), Indent / Outdent, Duplicate (link)
- [ ] **Parse / Push** (`Ctrl+Shift+>`) — parse focused Unparsed File, or push focused Workspace / scope via WebDAV ([[workspace-file-sync]]); **Pull** (`Ctrl+Shift+<`) for focused scope. See [[lazy-load]].
- [x] **Save** (`Ctrl+S`)
- [x] Navigate: Find, Zoom in / out / owner, Jump to Target
- [x] Prevent Workspace nodes from moving outside Workspaces. [[workspace-file-directory-placement]]
- [x] Parse file / Reparse from disk (on-demand hydrate of owned File) — via **Parse** branch of `Ctrl+Shift+>`
- [x] **Broken / unresolved references in the UI** — show when a workspace label or path reference cannot resolve; server-side file-status (not only desktop-mapped). See [[workspace-file-model]], [[doc/current/workspace-stage-plan]].
- [ ] **Overwrite policy** — last-write-wins in scope for v1 WebDAV sync; no FF / mirror-delete ([[workspace-file-sync]]). Graph multi-client merge remains out of scope ([[future-merge-sync]]).

## Lazy Load
Bringing existing trees into the workspace model and responding after file-tree sync. Canonical project and decisions: [[lazy-load]]. Target trigger: after WebDAV push + finish-commit ([[workspace-file-sync]]).
- [x] **Create-only stub reconciliation** — added paths create or reuse matching Directory and File stubs under the named Workspace through standard server Change lists.
- [x] **Structural stubs only** — current reconciliation does not parse file contents or create parsed child nodes.
- [x] **Complete disk-to-graph reconciliation** — added/deleted/renamed/moved paths reconcile through graph-only Changes; Git `M` marks the corresponding document Unparsed; exact `.amb`, refs/TRASH, identity, and idempotency semantics are covered. Best-effort failure remains observable without speculative repair/retry.
- [ ] **Wire reconcile after WebDAV push + finish-commit** (replace receive-pack-only trigger)
- [ ] **Expand-to-parse** — when a file is expanded, parse it and merge the result into existing nodes.
- [ ] **Richer freshness metadata/UI** — planned after reconciliation with expand-to-parse; show whether the local file is current, unparsed, older than the server file, or newer than the server file.
- [ ] **Documents as load units** — one graph, many documents (`docId` / membership); load and unload whole documents rather than one giant snapshot. See [[workspace-file-model]], [[revising-workspace-file-model]], [[postgres-roadmap]] §5.
- [ ] **Later residency/search work** — server lazy residency, client unload/LRU, repo-wide query, and annotation migration when files change. See [[workspace-scale-file-and-db-management]], [[workspace-scale-import]].
