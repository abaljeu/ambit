# Load no-upstream diagnosis

## Function and predicate

[WorkspaceGit.trackedBranch](../../../src/Server/WorkspaceGit.fs) lines 121–143.

After `currentBranch` reads `.git/HEAD`, the predicate requires both of these to return a non-blank string:

- `git config --get branch.{branch}.remote`
- `git config --get branch.{branch}.merge`

Any other result, including a missing key, becomes `Error "Current branch '{branch}' has no upstream."` at line 143.

Load reaches that through [GithubTransportActor.pullTracked](../../../src/Server/GithubTransportActor.fs) lines 67–71 and 87–88, which calls `git.trackedBranch workspaceRoot`. Production wiring is `trackedBranch = WorkspaceGit.trackedBranch` at line 145.

## Directory and branch

`workspaceRoot` is `DocumentPersistPath.workspaceRootFor`: DataDir plus the enclosing Workspace name. Server `appsettings.json` sets `DataDir` to `../../data` from content root `src/Server`, so a Workspace named fambit is `data/fambit`.

That work tree’s HEAD is `ref: refs/heads/master`. The attached branch is `master`.

## Git evidence

In `data/fambit`:

- `git remote` → `origin` (exit 0)
- `git remote -v` → `origin https://github.com/abaljeu/fambit` (fetch and push)
- `git rev-parse --abbrev-ref HEAD` → `master`
- `git status -sb` → `## master` (no `...origin/...` tracking decoration)
- `git rev-parse --abbrev-ref '@{upstream}'` → exit 128, `fatal: no upstream configured for branch 'master'`
- `git config --get branch.master.remote` → exit 1, empty output
- `git config --get branch.master.merge` → exit 1, empty output
- `git config --show-origin --get-regexp '^branch\.'` → exit 1, empty output

`.git/config` has `[remote "origin"]` and no `[branch "master"]` section. Gambol source does not set those branch keys.

## Root cause

`origin` is a remote. Upstream tracking is `branch.master.remote` and `branch.master.merge`. Both are unset, so `trackedBranch` reports no upstream. `git remote` listing `origin` does not set those keys.

## Code change

None. The check matches git’s own “no upstream configured” result. The message was left as written.
