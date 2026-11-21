# 05 — Relocate in the same workspace

Ticket: [[05-relocate-in-the-same-workspace-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]
Review: [[05-review.md]]

Check: pass

## 1. Change

[x] No change.

## 2. Diff

[x] `DocumentPathMove.sameWorkspaceMoves` in `src/Shared/DocumentPathMove.fs` keeps a File or a Directory whose enclosing Workspace id is `Some` on the pre graph and the post graph and those ids are equal.
[x] `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` in `src/Server/DocumentPersistPath.fs` resolve the repository root and the git path.
[x] `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs` makes one `GitSave.runGit` call with `mv --`, the old git path, and the new git path.
[x] Each new source binding is 40 lines or under. Each new source line is 100 characters or under.
[x] The new functions are the only copies of that classification, that prefix strip, and that `git mv` call.

## 3. Hard finding

[x] ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` stays. Outputs names that filter string. The name is 100 characters. The `let` line that binds it is 113 characters.
[x] `.agents/rules/fsharp-source.md` applies the 100-character line rule to source code.
[x] Alan is deciding that binding separately.

## 4. Suite

[x] `scripts/agent-test.sh --full` passed. `bash scripts/check-test.sh 3184` printed `DONE` and `/tmp/ambit-full-suite-20261112-231810.log`.
[x] Server passed 781, skipped 1, failed 0, total 782.
[x] Shared passed 1937, skipped 3, failed 0, total 1940.

## 5. Scan

[x] `python3 scripts/standards-scan.py --diff origin/staging` prints one LONG line: the classification test binding in section 3.
