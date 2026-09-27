# 12 — WorkspaceGit tracked-branch round-trip independent code review recheck

Review range: `origin/staging...HEAD`, focused on must-fix commit `2cc8aedf`.

## 1. Good

1. **Queued Persist** — resolved. FileAgent and DbAgent now call work-tree-gated `persistGraphOps` without `CoreMailboxBackend.runBounded`, so an eight-second wait cannot return a timeout and leave a late writer. Bounds remain only around non-file work. The new FileAgent mailbox test waits beyond the former timeout, proves no write occurs while the gate is held, then proves one accepted write after release. DbAgent uses the same corrected call shape but has no matching wait-past-eight-seconds regression test.
2. **Equivalent Windows roots** — resolved. `DocumentPersistPath.normalizedWorkTreeRoots` and `WorkspaceGit.withWorkTreeGate` now share `WorkspaceGit.normalizeWorkTreeRoot` and the same platform comparer. Full paths, trailing separators, and Windows case aliases therefore de-duplicate before nested acquisition. The alias test would deadlock or fail `Assert.Single` on the prior implementation.
3. **Operation contention** — the prior Nice-to-have is complete. New tests hold the work-tree gate across `pullTracked` and `saveTracked`, then prove that each waits and continues after release.

## 2. Must-fix

None remaining from the prior review.

## 3. Verification

1. `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` found no limit breach.
2. Focused FileAgent and WorkspaceGit tests passed 35 of 35.
3. `dotnet test gambol.sln` built all projects but did not pass: Shared passed 1,722 of 1,724 with one failure and one skip; Server passed 542 of 546 with four failures; CloudAgents passed 62 of 62. The five failures are outside commit `2cc8aedf` and the focused tests pass.
4. [12 — Run the Workspace git tracked-branch round-trip](../issues/12-workspace-git-tracked-branch-round-trip.md) remains `coded`.

## 4. Summary

Good: both prior Must-fixes are resolved. Must-fix: none remaining.
