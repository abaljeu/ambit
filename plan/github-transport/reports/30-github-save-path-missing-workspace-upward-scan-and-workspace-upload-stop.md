Save resolved the Workspace, then committed and pushed, and did not persist the focus or pull.

## Evidence

[ ] `runSaveStep` received `workspaceId` and did not use it.
[ ] The body called `saveTracked`, which commits and then pushes.
[ ] `runBody` already called `workspaceFromFocus` for Save and for Load.
[ ] `workspaceFromFocus` calls `GraphQuery.enclosingWorkspace`.
[ ] Load's pull resolution is `git.pullTracked`, mapped to `WorkspaceGit.pullTrackedBranch`.
[ ] Save did not call that pull.
