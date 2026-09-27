# 12 — WorkspaceGit tracked-branch round-trip independent code review

Review range: `origin/staging...HEAD` for GitHub PR 139.

## 1. Standards

### 1.1. Good — repository standards

1. The mechanical standards scan found no F# line, function, or file-size limit breach.
2. The split from `src/Server/DocumentPersistence.fs` into `src/Server/DocumentPersistChange.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Server/DocumentPersistWrite.fs` is a separate commit and keeps each source file below the repository limit.

### 1.2. Must-fix — standards

None.

### 1.3. Nice-to-have — standards

None.

## 2. Spec

### 2.1. Good — tracked-branch behavior and scope

1. `src/Server/WorkspaceGit.fs` reads the remote, attached branch, and upstream from the selected work tree. `pullTracked` uses whole-tree `pull --ff-only`; `saveTracked` calls `GitSave.commitAll` and then pushes `HEAD` to the tracked upstream. Neither path uses a file pathspec, checkout, switch, older commit, GitHub credential parameter, or Graph-to-file Persist call.
2. `.gitignore`, condensed errors, a conflict path, remote present and absent, tracked pull, commit then push, fast-forward pull, non-fast-forward push rejection, and direct gate waiting have temp-work-tree coverage in `tests/Server.Tests/WorkspaceGitTests.fs`. All 27 tests in that module pass.
3. The changed Persist-related modules have 119 passing focused tests.
4. [12 — Run the Workspace git tracked-branch round-trip](../issues/12-workspace-git-tracked-branch-round-trip.md) remains `coded`. The checkmarks in [github-transport architecture](../arch.md) do not claim Peer Actor or Command wiring.

### 2.2. Must-fix — bounded Persist rejects instead of only waiting

1. [16 — Persist/git work-tree gate](../issues/16-persist-git-work-tree-gate.md) requires a contending Persist caller to wait and continue instead of rejecting as busy. `DocumentPersistChange.persistGraphChangeWith` waits through `DocumentPersistPath.withWorkTreeGates`, but production callers in `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` wrap that wait in `CoreMailboxBackend.runBounded`. After eight seconds, `src/Server/Core/CoreMailboxBackend.fs` returns `Error "change processing timed out"` and explicitly abandons the still-running task. The task can later acquire the gate and write while a later accepted Change is also processing. A git operation that holds the gate for more than eight seconds therefore makes Persist reject and then continue in the background. This contradicts the queue-until-free decision and creates a late concurrent-write risk.
2. `tests/Server.Tests/WorkspaceGitTests.fs` tests `DocumentPersistWrite.writeDocument` directly, so it bypasses both bounded production paths and cannot detect this defect.

### 2.3. Must-fix — equivalent Windows roots can self-deadlock

1. `DocumentPersistPath.normalizedWorkTreeRoots` uses case-sensitive `List.distinct` and `List.sort`, while `WorkspaceGit.withWorkTreeGate` uses an ordinal-ignore-case dictionary on Windows and trims trailing separators in its private gate key. Two pre/post Workspace roots that differ only by case or a trailing separator can survive normalization as two entries and then resolve to the same semaphore. `withWorkTreeGates` nests those acquisitions, so the second acquisition waits forever on the gate already held by the same call. Normalize, compare, de-duplicate, and order roots with the same identity rules as the gate.

### 2.4. Nice-to-have — operation-level gate coverage

1. Add contention tests that hold the Workspace gate while calling `pullTracked` and `saveTracked`, then release it and prove each operation continues. Current tests prove the gate primitive and a direct Persist write, but do not exercise pull or commit under contention.

## 3. Verification

1. `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` passed with no limit finding.
2. `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --no-restore --filter FullyQualifiedName~WorkspaceGitTests` passed 27 of 27 tests.
3. The focused changed Persist test modules passed 119 of 119 tests.
4. `dotnet test gambol.sln` did not pass: one unrelated `AmbDocumentTests` assertion and four unrelated Server Actor/database tests failed. The Shared failure reproduces alone. No failed full-suite test is in a file changed by this PR.

## 4. Summary

Standards: 0 findings. Spec: 2 Must-fix findings and 1 Nice-to-have finding. The worst issue is that production Persist can return a timeout while its abandoned gate waiter later writes, contrary to the locked wait-then-continue behavior.
