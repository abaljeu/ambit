# Implement 12 — remote-wins pull

The code change is written. Tests did not run. Ticket Status stays `done`. Do not treat this as coded.

## 1. What changed

1. [WorkspaceGit](../../../src/Server/WorkspaceGit.fs) `pullTrackedBranch` merges the tracked remote branch with `--no-rebase --no-edit -X theirs` instead of `--ff-only`.
2. When that merge stops on an unmerged path, stage 3 keeps the remote file and a missing stage 3 removes the file, then `commit --no-edit` finishes the merge.
3. Push, `ensurePushConfig`, and the condensed error path are unchanged.
4. [WorkspaceGitTests](../../../tests/Server.Tests/WorkspaceGitTests.fs) adds Server-only, remote-only, both-edited, both-added, and both edit/delete cases, and keeps the non-fast-forward push rejection test.

## 2. Tests

`dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~pullTracked|FullyQualifiedName~saveTracked rejects a non fast forward"` failed before any test ran.

Server `bin\Debug\net10.0` is locked by `netcoredbg.exe` (pid 31956) and `.NET Host` (pid 37780). Copy of Shared and CloudAgents assemblies into the Server output failed (`MSB3027` / `MSB3021`).

## 3. Not done

1. Arch `[o]` / `[ ]` pull claims stay as written. The merge is not verified, so those claims are not checked off.
2. Code-review and the end-of-build suite did not start.
3. No commit.

## 4. Confirmation

Alan confirmed the pull works on 2026-10-14. The test-lock in section 2 still stands as the record of that run.
