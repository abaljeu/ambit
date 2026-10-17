# 29 — git Load commits before pull

**Status:** `coded`
**Type:** bug-fixing
**Blocked by:** None — can start immediately
**Trackers:** project spec
Actual: 2h25m

## Context

A person runs git Load on a Workspace. The work tree has an edit to a tracked file. The incoming pull would overwrite that file. git refuses the merge, and git Load fails with that overwrite error. The spec says git Load commits the work tree before the pull. The Load pull reads the tracked branch and then pulls. That pull skips the commit. git Save already commits inside the same work-tree gate.

## What to build

git Load commits every work-tree edit inside the same work-tree gate, then pulls. The commit message is "gambol: git Load". A clean work tree is NothingToCommit. That case succeeds, git writes no commit, and Load pulls. git Load leaves the push to git Save. Received paths are the changes the pull brought in. The Load commit records files already persisted. That commit is not a received change.

### 1. Commit then pull

1. [x] **Commit inside the gate** — git Load calls commitAll with message "gambol: git Load" and no client hint inside the work-tree gate, then pullTracked. git Load does not push.
2. [x] **Nothing to commit** — A clean work tree is LoadCommit.NothingToCommit. That case is success. git writes no commit. Load pulls.
3. [x] **Pull diff only** — Inside that same gate, git Load reads HEAD after the Load commit and immediately before the pull. Received paths are that head through the head after the pull. The two heads are LoadPullHeads. A path only the Load commit records is not received.
4. [x] **Pull leaves the file** — A tracked file has local edits. Load commits it. The pull does not change that file. The committed content stays. The path is not received.
5. [x] **Pull replaces the file** — A tracked file has local edits. Load commits it. The pull changes that file. The pulled content replaces it. Load succeeds with no overwrite error. The path is received.

### Acceptance Criteria

1. [x] **Commit order** — runLoad with a fake git records commitAll before pullTracked. The message is "gambol: git Load". NothingToCommit still pulls. The fake push is not called.
2. [x] **Dirty tracked file** — A tracked file edit that the incoming pull would overwrite does not stop git Load. The pull completes. The remote file content lands. The Load commit stays on the local branch.
3. [x] **Committed leftover** — A file the Load commit records, and the pull does not change, is not reconciled and is not marked Unparsed.
4. [x] **Clean tree** — runLoad on a clean tracked work tree succeeds. The pull lands. The log has no "gambol: git Load" commit.
5. [x] **Pull leaves the local file** — A tracked file the Load commit records, and the pull does not change, keeps that committed content. The file node stays Current.
6. [x] **Pull replaces the local file** — A tracked file the Load commit records, and the pull changes, becomes the pulled content. Load succeeds. The file node is marked Unparsed.

## Finish-up

1. [x] **Amendments** — The Load pull commits, then pulls. Received paths are the pull diff. Comments record the remaining side effects.
2. [x] **Basis and reference docs** — The project spec states the two dirty-file results. A file only the Server changed stays the Server file and is not a received path. A content conflict becomes the remote file and is a received path. The current parse-persist doc already states that git Load commits the work tree before the pull. That doc stays as written.
3. [x] **Documentation** — The current parse-persist doc already states the commit. This ticket adds no doc text.

## See also

1. [Github transport actor](../../../src/Server/GithubTransportActor.fs)
2. [github-transport spec](../spec.md)
3. [github-transport architecture](../arch.md)
4. [Parse and persist](../../../doc/current/parse-persist.md)

## Comments

1. **Received paths** — Alan: the Load commit is not received. It records files already persisted. runLoad reads HEAD inside the pull gate, after commitAll and before pullTracked, and again after the pull. Received paths are that range. A path only the Load commit records stays out of the received list.
2. **Direct pull** — WorkspaceGit.pullTracked still pulls with no commit. A dirty tracked file still blocks that direct pull. git Load uses the actor gate.
3. **Nothing to commit** — LoadCommit names Committed, NothingToCommit, and Failed. A clean work tree is NothingToCommit. That case succeeds and writes no commit. Load then pulls. Failed stops the pull. The head pair is LoadPullHeads.
4. **No push** — The Load commit stays local until a later git Save.
5. **Whole tree** — commitAll stages the whole work tree. Untracked files ride along with the dirty tracked file.
6. **Tracked branch read** — The actor Load path no longer calls trackedBranch itself. commitAll returns the tracked branch, and the pull uses that value.
7. **Committed leftover** — [git Load does not reconcile a disk file into the graph](../../../tests/Server.Tests/LoadSaveCommandTests.fs) passes. The Load commit records `leftover.txt`. The pull does not change it. The name stays out of the Workspace graph and is not marked Unparsed.
8. **Arch re-check** — Received paths are the pull diff. [parse-thread architecture](../../parse-thread/arch.md) **Received files** and [core-refinement architecture](../../core-refinement/arch.md) **git pull finish** hold for this window. github-transport architecture for the commit-then-pull holds.
9. **Dirty file and the pull** — Alan: Load commits a tracked file that has local edits. When the pull does not change that file, the committed content stays and the path is not received. When the pull changes that file, the pulled content replaces it, Load succeeds with no overwrite error, and the path is received. The spec states the same rule. A file only the Server changed stays the Server file and is not a received path. A content conflict becomes the remote file and is a received path.

## Time

- 2026-10-17 45m — commit inside the Load pull gate, order test, dirty-file test (from chat)
- 2026-10-17 30m — received paths are the pull diff; committed leftover stays out of the graph (from chat)
- 2026-10-17 30m — NothingToCommit on a clean tree; LoadPullHeads for the pull diff (from chat)
- 2026-10-17 40m — a dirty file the pull leaves stays committed and is not received; a dirty file the pull changes is replaced and is received (from chat)
