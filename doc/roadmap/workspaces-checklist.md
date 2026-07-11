# Workspaces checklist

Category: Workspace scale
See also: [[workspaces]], [[doc/current/workspace-graph]], [[doc/current/workspace-local-mapping]]

Living checklist for implementing workspaces.  Mark done an item when it is done.

- Workspace = graph node type; 
- 1:1 with `DataDir/{workspaceName}`; 
- that directory is an independent git repo.
- Desktop holds a partial map `workspaceName` → local path (also a git repo); the server is the push target.

## Graph model

- [x] Workspace as a first-class graph node type and structural rules around it.
- [x] Files and Directories are only allowed to be in Directories or Workspaces. (Root is a workspace.) — plan: [[workspace-file-directory-placement]] (Slice A done; Slice B create/move cancelled; Slice C legacy reconcile cancelled)

## Server DataDir
One workspace folder per name under DataDir; ownership and on-disk layout.
- [x] Workspaces, directories and files may appear in DataDir on server. In graph, workspaces are in Workspaces subnode.  Files and directories will be in Root node.
- [x] Name collision prevention on new workspace/directory/file, and on rename or move directory/file, needs to account for the root folder structure.

## Git
Each workspace directory is its own git repository (server and mapped local roots). Protocol and locked decisions: [[git-sync-gateway]]. Completed G0–G7 implementation record: [[workspace-scale-import-slice2-plan]]. Desktop remote name is **`ambit`**. Git transport ends after the client commands and sync status below; response to changed files belongs to Lazy Load.

- [x] Init empty repo in a new server directory
- [x] Commit all files to repo on server
- [x] Add smart HTTPS git endpoints at `/ambit/git/{label}.git` with stock service paths **`git-upload-pack`** / **`git-receive-pack`** (custom policy is middleware; no single-file GET)
- [x] workspace-push semantics: reject if sender is not current (non-FF); **reject-dirty** if server working tree is uncommitted (no JIT on push — JIT only before workspace-pull / upload-pack). Locked in [[git-sync-gateway]].
- [x] Git auth: HTTPS PAT via HTTP Basic; issue at `/ambit/git-token` after login; cookie alone insufficient ([[git-sync-gateway]])
- [x] Desktop: Clone the server repo to a directory (stock `git clone` against gateway URL)
- [x] Desktop: workspace-pull against gateway (`git pull` / fetch)
- [x] Desktop: workspace-push against gateway (`git push`)

Client (user-facing commands for the above; Download / Upload live here rather than under Client UX):
- [x] Connect workspace remote (point a local folder at the server; remote name `ambit`)
- [x] Clone server workspace into a local folder
- [x] workspace-pull from server
- [x] workspace-push to server
- [x] Show sync status (ahead / behind / local changes)

- [ ] Maybe allow any fast-forward merge.

## Desktop mapping
Partial `workspaceName` → absolute local path bindings on the desktop (see [[doc/current/workspace-local-mapping]]); `ambit` remote setup depends on the server git gateway (see [[git-sync-gateway]]).
- [x] Config file load at desktop startup
- [x] Label / absolute-path validation
- [x] Resolve `//label/relative` under mapped root
- [x] File-status / import / export via mapped paths

- [x] Folder picker for local root

- [x] API Get/Put mapping on workspace.

## Client UX
How users create, open, navigate, and work in workspaces in the UI (commands / keybound ops). Desktop mapping API and folder picker stay under Desktop mapping; git Download / Upload / connect commands live under Git.
- [x] **Insert…** (`f`): New Workspace (focus on Workspaces); New File / New Folder (elsewhere); pick existing file → insert Ref
- [x] **Rename** (`F2`) for Directory / File / Normal; workspace rename refused (immutable names)
- [x] **Delete** (move to TRASH), **Move Selected** (`m`), Indent / Outdent, Duplicate (link)
- [x] **Parse / Upload** (`Ctrl+Shift+>`) — parse focused Unparsed File, or push focused Workspace; **Pull** (`Ctrl+Shift+<`) for focused Workspace (desktop Git). See [[lazy-load]].
- [x] **Save** (`Ctrl+S`)


- [x] Navigate: Find, Zoom in / out / owner, Jump to Target
- [ ] Prevent Workspace nodes from moving outside Workspaces. [[workspace-file-directory-placement]]
- [x] Parse file / Reparse from disk (on-demand hydrate of owned File) — via **Parse / Upload** (`Ctrl+Shift+>`)
- [ ] **Broken / unresolved references in the UI** — show when a workspace label or path reference cannot resolve; server-side file-status (not only desktop-mapped). Deferred Stage 5. See [[workspace-file-model]], [[doc/current/workspace-stage-plan]].
- [ ] **Multi-client graph merge** — eventual consistency and conflict markers across clients (separate from git push/pull). See [[future-merge-sync]], [[git-sync-gateway]], [[postgres-roadmap]].  STILL needed for non-desktop clients and direct in-app edits. git merge is not available on client side; we could employ server-side git merge.

## Lazy Load
Bringing existing trees or repos into the workspace model and responding after Git changes. Canonical project and decisions: [[lazy-load]].
- [x] **Create-only post-receive reconciliation** — after successful server receive, added paths create or reuse matching Directory and File stubs under the named Workspace through standard server Change lists; initial push is supported.
- [x] **Structural stubs only** — current reconciliation does not parse file contents or create parsed child nodes.
- [x] **Complete disk-to-graph reconciliation** — added/deleted/renamed/moved paths reconcile through graph-only post-receive Changes; Git `M` marks the corresponding document Unparsed; exact `.amb`, refs/TRASH, identity, and idempotency semantics are covered. Best-effort failure remains observable without speculative repair/retry.
- [ ] **Expand-to-parse** — when a file is expanded, parse it and merge the result into existing nodes.
- [ ] **Richer freshness metadata/UI** — planned after reconciliation with expand-to-parse; show whether the local file is current, unparsed, older than the server file, or newer than the server file.
- [ ] **Documents as load units** — one graph, many documents (`docId` / membership); load and unload whole documents rather than one giant snapshot. See [[workspace-file-model]], [[revising-workspace-file-model]], [[postgres-roadmap]] §5.
- [ ] **Later residency/search work** — server lazy residency, client unload/LRU, repo-wide query, and annotation migration when files change. See [[workspace-scale-file-and-db-management]], [[workspace-scale-import]].
