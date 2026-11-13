# 32 — Route plain Save
**Status**: coded

One job: Plain Save is git Save when a remote exists, desk Save when a local mapping exists and no remote exists, and the Save actor's Neither arm when neither exists.

Type:

[x] coding

Blockers:

[x] none

## Inputs

### Working

[x] `PathPick.plainSavePath` in `src/Shared/PathPick.fs` returns Desk when the pre-pick is Plain, the workspace label is in `localLabels`, and that label is absent from `githubLabels`.
[x] `saveOpFor` in `src/Client/UpdateSave.fs` calls `plainSavePath` and posts `SubmitLoadSaveCommand`.
[x] `CommandEntry` in `src/Shared/CommandEntry.fs` lists Save keys `Ctrl+S` and `Ctrl+Shift+<`. Download is that Save command.
[x] `LoadSaveStart.startCommand` in `src/Server/LoadSaveStart.fs` starts the desk command for Desk and `GithubTransportActor.start` otherwise.
[x] `runSaveBody` in `src/Server/GithubTransportActor.fs` calls `persistFocusWhenNotOrdinary`.
[x] `commitSaveRepository` in that file commits through `GitSave.commitAll` when `WorkspaceGit.isRepo` finds that folder's `.git`.
[x] `WorkspaceGit.remoteExists` in `src/Server/WorkspaceGit.fs` requires that directory's `.git` through `WorkspaceGit.isRepo`, then reads `git remote`.
[x] `deskSaveOp` in `src/Client/UpdateSave.fs` posts `ContinueDeskSave` when `canGitSave` is true.

### Reference

[x] Solution 15 — Plain Save prefers git in [github-transport spec](../../spec.md).
[x] Story path 15 — Plain Save prefers git in [github-transport architecture](../../arch.md).

## Do not load

[x] How desk Save does local sync. The tree keeps the staging desk command.
[x] Desk Load. It is open for the later desk load/save ticket.
[x] The ROOT pull label `.`. `GithubTransportActor.reconcileLabel` uses `.` when the workspace id is `Graph.rootId`. `GraphQuery.trySyncRootByLabel` maps `.` to that root. That label is open for the later desk load/save ticket.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Plain Save with a remote runs the Save actor Remote arm. That arm commits, pulls, and pushes.
3. [x] Plain Save with a local mapping and no remote runs desk Save. `startCommand` starts Desk. `deskSaveOp` posts `ContinueDeskSave` when `canGitSave` is true.
4. [x] Plain Save with no local mapping and no remote is Neither. Neither persists through `persistFocusWhenNotOrdinary` and commits through `commitSaveRepository` when the folder is a repo.
5. [x] `Ctrl+S` and `Ctrl+Shift+<` both run Save.

## Outputs

[x] `src/Shared/PathPick.fs`
[x] `src/Shared/CommandEntry.fs`
[x] `src/Client/UpdateSave.fs`
[x] `src/Server/LoadSaveStart.fs`
[x] `src/Server/GithubTransportActor.fs`
[x] `src/Server/WorkspaceGit.fs`
[x] `plan/github-transport/spec.md`
[x] `plan/github-transport/arch.md`

## Stop when

[x] Status stays `coded`.

## Check

[x] ``plain Save with a remote and a local mapping stays on the Save actor`` in `tests/Shared.Tests/PathPickTests.fs`.
[x] ``plain Save with no remote and a local mapping chooses Desk`` in `tests/Shared.Tests/PathPickTests.fs`.
[x] ``plain Save with no remote and no local mapping stays on the Save actor`` in `tests/Shared.Tests/PathPickTests.fs`.
[x] ``plain Save of a non-git workspace ignores a parent repository`` in `tests/Server.Tests/LoadSaveCommandTests.fs`.
[x] ``plain Save with no GitHub remote commits once and does not push`` in `tests/Server.Tests/LoadSaveCommandTests.fs`.
[x] ``plain Save of a non-git workspace persists and does not push`` in `tests/Server.Tests/LoadSaveCommandTests.fs`.
[x] A reader who reads only the One job line can say what the ticket builds.
