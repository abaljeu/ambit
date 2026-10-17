# Code review — git Load commits before pull

- Range: `git diff origin/staging...HEAD`
- Pull request: [git Load commits before pull](https://github.com/abaljeu/ambit/pull/256)
- Spec: [29 — git Load commits before pull](plan/github-transport/issues/29-git-load-commits-before-pull.md) and [github-transport spec](plan/github-transport/spec.md)
- Arch: [github-transport architecture](plan/github-transport/arch.md), [parse-thread architecture](plan/parse-thread/arch.md) Received files, [core-refinement architecture](plan/core-refinement/arch.md) git pull finish

## 1. Standards

1. **Must fix** — **Hard — unused tracked branch** — [core-agent-behavior](.agents/rules/core-agent-behavior.md): "Remove imports/variables/functions that YOUR changes made unused." `GithubTransportGit.trackedBranch` in [Github transport actor](src/Server/GithubTransportActor.fs). `pullTracked` and `saveTracked` take the branch from `commitAll`. `workspaceGit` still sets `WorkspaceGit.trackedBranch`. The test records still fill the field. Nothing reads it.
2. **Should fix** — **Judgement — Duplicated Code** — [Github transport load pull tests](tests/Server.Tests/GithubTransportLoadPullTests.fs). `git Load pulls when a dirty tracked file would be overwritten` and `git Load pulls a clean work tree and writes no commit` repeat the bare-remote clone that `cloneRemote` already performs: `init --bare -b master`, `remote add origin`, `push -u origin master`, and `clone`. The clean test also repeats the production Load call that `runProductionLoad` already performs.
3. **Nit** — **Judgement — Mysterious Name** — `headsAfterPull` in [Github transport actor](src/Server/GithubTransportActor.fs). The name says the function reads heads. The body also pulls: `git.pullTracked workspaceRoot tracked`.
4. Scan note — `readLoadCommit` (8 lines), `headsAfterPull` (7 lines), `changedAfterPull` (13 lines), `workTreeGate` (5 lines), and `nothingToCommitMessage` (2 lines) stay under the 40-line function limit in [F# source](.agents/rules/fsharp-source.md). The scan output is those sizes only.

## 2. Spec

1. **Should fix** — **Partial — Failed stops the pull** — [29 — git Load commits before pull](plan/github-transport/issues/29-git-load-commits-before-pull.md) says `LoadCommit.Failed` stops the pull. `pullTracked` returns that error before `headsAfterPull`. No test passes a commit error and checks that the pull does not run.
2. Note — These requirements hold in `pullTracked` and in the production tests. The commit message is `gambol: git Load` with no client hint, inside the work-tree gate, before the pull. `LoadCommit.NothingToCommit` still pulls and writes no commit. Received paths are `changedPathsBetween` of the head after the Load commit and the head after the pull, so the Load commit is not received. A dirty file the pull leaves stays the committed content and is not received. A dirty file the pull changes becomes the pulled content, Load succeeds, and the path is received. Load does not call `pushTracked`. `WorkspaceGit.pullTracked` still pulls with no commit. The focused suite passed: 17 passed, 0 failed, 0 skipped (`GithubTransportLoadPullTests`, `GithubTransportActorTests`, and `git Load does not reconcile a disk file into the graph`).

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 3 findings. Worst: `trackedBranch` is unused after this change.
- Spec: 1 finding. Worst: no test locks `LoadCommit.Failed`.
- Arch: 0 findings.
