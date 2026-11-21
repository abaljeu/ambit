Check: pass

# 32 — Route plain Save

Status is `coded`.

## 1. Current

[[../../../../doc/current/http-contract.md]]

[[../../../../doc/current/workspace-file-sync.md]]

[x] Save is `POST /ambit/load-save-command`.
[x] `PathPick.plainSavePath` returns Desk when the workspace label is in `workspaceMappedLabels` and absent from `workspaceRemotes`.
[x] `WorkspaceGit.remoteExists` requires that folder's `.git` through `WorkspaceGit.isRepo`.
[x] The Neither arm commits through `commitSaveRepository` when `WorkspaceGit.isRepo` finds that folder's `.git`.
[x] `CommandEntry` Save keys are `Ctrl+S` and `Ctrl+Shift+<`. Download is that Save command.
[x] `downloadOp` enqueues `workspace-download`. Save does not call `downloadOp`.

## 2. User

[[../../../../doc/user/reference/commands/files.md]]

[x] `Ctrl+S` and `Ctrl+Shift+<` both run Save.
[x] A local mapping and no git remote runs desk Save. desk Save posts the data-directory save when that save is enabled.
[x] A folder with its own `.git` and no remote commits on Save.

## 3. Found elsewhere

[[../../spec.md]]

[[../../arch.md]]

[[../../map.md]]

[[32-route-plain-save-ticket.md]]

[x] Plain Save is git when a remote exists, desk when a local mapping exists and no remote exists, and Neither when neither exists.
[x] How desk Save does local sync is unspecified. The tree keeps the staging desk command: `LoadSaveStart.startCommand` for Desk, then `deskSaveOp`.
[x] Desk Load is open for the later desk load/save ticket.
[x] The ROOT pull label `.` is open for that ticket. `GithubTransportActor.reconcileLabel` uses `.` when the workspace id is `Graph.rootId`. `GraphQuery.trySyncRootByLabel` maps `.` to that root.
