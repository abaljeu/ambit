# 32 — Workspace lock handoff — implement scope

Ticket: [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md). Project: [core-refinement](../project.md).

Alan, 2026-11-10: fetch runs to completion first and holds no `NodeLockPool.withLock`. The pull is the merge plus the Unparsed mark, and that pull holds the Workspace `withLock`.

## Signatures

- `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` is `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
- `NodeLockRejection` has one case, `HolderHoldsLock`. An id absent from `graph.nodes` runs the action and returns `Ok`.
- `GithubTransportGit.pullTracked` in `src/Server/GithubTransportActor.fs` was one `git pull` inside the Workspace lock, together with the commit.
- `WorkspaceGit.pullTrackedBranch` in `src/Server/WorkspaceGit.fs` ran `git pull --no-rebase --no-edit -X theirs`.
- `WorkspaceGit.changedPathsBetween` is the git changed list. [12 — Git pull marks each received file Unparsed](../../parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md) is `done`. [13 — Git pull marks containing Directories Unparsed](../../parse-thread/issues/13-git-pull-marks-containing-directories-unparsed.md) is `coded`. `LazyLoadReconciliationServer.reconcileFromChanges` posts that mark.
- `CoreDoor.actorChanges` `loadNode` is `CoreMailboxApi.mbLoadNode`, which logs `EventBody.Load` and pushes the Parse stack.
- `WorkspaceWebDav.handlePut` wrote the PUT body with no `NodeLockPool`.

## Match sites

- `GithubTransportActor.commitInLock` held `NodeLockPool.withLock` around `commitAll` and the pull continuation.
- `GithubTransportActor.runLoadOn` called that locked pull, then `applyPulledChanges` outside the lock.
- `GithubTransportActor.commitAndPull` held one lock around the git Save commit and the pull.
- `WorkspaceWebDav.registerRoutes` is the PUT land. `RouteRegistration.registerPersistenceAndRoutes` calls it.

## Existing tests

- ``git Load commits before pull`` in `tests/Server.Tests/GithubTransportLoadPullTests.fs` records commit then pull.
- ``git Load and git Save wait while the Workspace lock is held`` records that commit waits on a held Workspace lock.
- ``git Load pull marks a received file and its directory Unparsed`` and ``git Save pull marks a received file Unparsed before push`` use the changed-list mark.
- ``pullTracked fast forwards the current tracked branch`` in `tests/Server.Tests/WorkspaceGitTests.fs` runs `WorkspaceGit.pullTracked`.

## Copy-from

- `NodeLockPool.withLock` is the lock. A nested call on the holder returns `Error HolderHoldsLock`.
- `LazyLoadReconciliationServer.reconcileFromChanges` is the Unparsed mark. This ticket does not restate the changed-list rules. [07 — Git changed list sets Unparsed](../issues/07-git-changed-list-unparsed.md) is superseded by 12 — Git pull marks each received file Unparsed and 13 — Git pull marks containing Directories Unparsed. The code home is those two tickets.

## implement

### Ticket claims that differ from the code

- Fetch and merge were one `git pull` inside the Workspace lock.
- The Unparsed mark and the Parse push ran after that lock released.
- Upload PUT wrote bytes with no Workspace `withLock` and no Unparsed mark in that write.

### Named new tests at public seams

- ``fetch completes before the workspace lock then the pull merges and marks`` calls `GithubTransportActor.withWorkspaceLock` and `GithubTransportActor.runLoad`.
- ``upload lands and marks Unparsed inside the workspace lock`` calls `WorkspaceUploadLock.landMarkRelease`.
