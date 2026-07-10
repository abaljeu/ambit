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
Each workspace directory is its own git repository (server and mapped local roots). Protocol and locked decisions: [[git-sync-gateway]]. Ordered slices: [[workspace-scale-import-slice2-plan]]. Desktop remote name is **`ambit`**.

- [ ] Init empty repo in a new server directory
- [x] Commit all files to repo on server
- [ ] Add smart HTTPS git endpoints so the server can accept push/pull for each workspace repo
- [ ] Push endpoint has special semantics: reject if sender is not current (non-FF); **reject-dirty** if server working tree is uncommitted (no JIT commit on push — JIT only before pull). Locked in [[git-sync-gateway]].
- [ ] Desktop: Clone the server repo to a directory
- [ ] Desktop: Pull server repo
- [ ] Desktop: Push server repo

Client (user-facing commands for the above; Download / Upload live here rather than under Client UX):
- [ ] Connect workspace remote (point a local folder at the server; remote name `ambit`)
- [ ] Clone server workspace into a local folder
- [ ] Pull from server
- [ ] Push to server
- [ ] Show sync status (ahead / behind / local changes)

- [ ] Maybe allow any fast-forward merge.

## Desktop mapping
Partial `workspaceName` → absolute local path bindings on the desktop (see [[doc/current/workspace-local-mapping]]); `ambit` remote setup depends on the server git gateway (see [[git-sync-gateway]]).
- [x] Config file load at desktop startup
- [x] Label / absolute-path validation
- [x] Resolve `//label/relative` under mapped root
- [x] File-status / import / export via mapped paths

- [ ] Folder picker for local root

- [ ] API Get/Put mapping on workspace.

## Client UX
How users create, open, navigate, and work in workspaces in the UI (commands / keybound ops). Desktop mapping API and folder picker stay under Desktop mapping; git Download / Upload / connect commands live under Git.
- [x] **Insert…** (`f`): New Workspace (focus on Workspaces); New File / New Folder (elsewhere); pick existing file → insert Ref
- [x] **Rename** (`F2`) for Directory / File / Normal; workspace rename refused (immutable names)
- [x] **Delete** (move to TRASH), **Move Selected** (`m`), Indent / Outdent, Duplicate (link)
- [x] **Import** / **Export** via desktop-mapped `//label/...` paths
- [x] **Save** (`Ctrl+S`)


- [x] Navigate: Find, Zoom in / out / owner, Jump to Target
- [ ] Prevent Workspace nodes from moving outside Workspaces. [[workspace-file-directory-placement]]
- [ ] Parse file / Reparse from disk (on-demand hydrate of owned File)
- [ ] **Broken / unresolved references in the UI** — show when a workspace label or path reference cannot resolve; server-side file-status (not only desktop-mapped). Deferred Stage 5. See [[workspace-file-model]], [[doc/current/workspace-stage-plan]].
- [ ] **Multi-client graph merge** — eventual consistency and conflict markers across clients (separate from git push/pull). See [[future-merge-sync]], [[git-sync-gateway]], [[postgres-roadmap]].  STILL needed for non-desktop clients and direct in-app edits. git merge is not available on client side; we could employ server-side git merge.

## Lazy Load
Bringing existing trees or repos into the workspace model.
- [ ] On (successful) push, all file and directory nodes must exist in graph.
- [ ] All files, directory workspace contents are not automatically converted into graph.
- [ ] When one of these is expanded, then the file is parsed and nodes updated
- [ ] Client shows nodes are stale if file is newer than node, or not parsed.
- [ ] **Documents as load units** — one graph, many documents (`docId` / membership); load and unload whole documents rather than one giant snapshot. Stage 9 still open. See [[workspace-file-model]], [[revising-workspace-file-model]], [[postgres-roadmap]] §5.
- [ ] **Later scale (residency / memory / search / annotations)** — server lazy residency, client unload of parsed files, repo-wide query, annotation migration when files change. Explicitly deferred after Slice 1–2. See [[workspace-scale-file-and-db-management]], [[workspace-scale-import]].
