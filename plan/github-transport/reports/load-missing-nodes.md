# Load missing nodes

The browser Git Load path did not create visible nodes for files that were on disk and absent from the graph. Server Parse already added those nodes. The client finished on ActorStart and did not run Load's directory-match after-step or Fetch+Poll.

## Failing path

A person runs Load. [LoadSaveCommandClient](../../../src/Client/LoadSaveCommandClient.fs) posts `/load-save-command`. The Server starts the Git Peer Actor and returns ActorStart only. The client applied `CommandDone` and stopped. The actor then pulled (often a no-op) and called [reconcileWorkspace](../../../src/Server/LazyLoadReconciliationServer.fs). New nodes existed on the Server graph. The browser never Fetch+Polled them, so the workspace root showed no new files or directories.

Desk Load already runs [ContinueDirectoryReconcile](../../../src/Client/UpdateWorkspaceLoad.fs) and then Fetch+Poll. Git Load did not.

Hypothesis 1 was true. Hypothesis 2 was false for Git (the actor already called directory-match). Hypothesis 3 was false (Added planning already creates missing nodes).

## Code change

Git Load's after-step is now the same workspace-root directory-match as Desk ReconcileServerDisk.

- [WorkspaceSyncScope.tryWorkspaceRootFromFocus](../../../src/Shared/WorkspaceSyncScope.fs) lifts any focus to the enclosing Workspace root.
- [gitLoadAfterOp](../../../src/Client/UpdateWorkspaceLoad.fs) starts `ContinueDirectoryReconcile` on that root, then the existing complete-directory-reconcile path Fetch+Polls.
- [LoadSaveCommandClient.applyResponse](../../../src/Client/LoadSaveCommandClient.fs) still applies ActorStart, then runs that after-step for Git Load only.

The actor still pulls and still calls `continueLoad`. The client after-step is what makes missing nodes appear in the UI when pull downloads nothing.

## Tests

Shared:

```
dotnet test tests/Shared.Tests -c Debug --no-build --filter 'FullyQualifiedName~tryWorkspaceRootFromFocus'
```

Passed 1, failed 0, skipped 0, total 1.

Server (filter in one command):

```
dotnet test tests/Server.Tests -c Debug --no-build --filter 'FullyQualifiedName~staging checkout already fetched|FullyQualifiedName~routed Git Load pulls|FullyQualifiedName~disk file the graph does not hold|FullyQualifiedName~workspace directory reconcile'
```

Passed 4, failed 0, skipped 0, total 4.

`src/Server/bin` was locked by `netcoredbg.exe` and `.NET Host`. Server.Tests built with `--no-dependencies` against the existing Server output. Do not treat that as a skipped suite: Total tests was 4.

Client compile gate `./scripts/client.sh build` succeeded.

New coverage: [Git Load response continues to workspace directory reconcile](../../../tests/Server.Tests/LoadSaveCommandClientTests.fs) and [git Load after-step creates a disk file the graph does not hold](../../../tests/Server.Tests/LoadSaveCommandTests.fs) (`leftover.txt` on disk, not in the graph, pull did not add it).
