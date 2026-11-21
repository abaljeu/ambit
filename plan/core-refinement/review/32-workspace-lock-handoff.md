# 32 — Workspace lock handoff

Ticket: [[../issues/32-workspace-lock-handoff.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- No findings.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- ``one upload posts NodeReceived once`` in [GitHub transport load pull tests](../../../tests/Server.Tests/GithubTransportLoadPullTests.fs) lands one PUT for a PullingFile node and expects one `Notice.NodeReceived`. The node is Unparsed and `pullingFile` is clear.
- ``a second upload posts no NodeReceived and marks Unparsed`` lands that PUT again. The log still has one `Notice.NodeReceived` and one `Notice.NodeUnparsed`. The node stays Unparsed.
- ``git pull of a new file posts NodeReceived once`` adds `note.txt` through `LazyLoadReconciliationServer.reconcileReceivedFiles` and expects one `Notice.NodeReceived`. The new file is Unparsed.
- ``git pull of an already received file posts no NodeReceived`` modifies a Current file. The log has no `Notice.NodeReceived` for that file, it has `Notice.NodeUnparsed`, and the file is Unparsed.
- ``reconcile emits NodeReceived`` still covers a PullingFile file the server had not held. The count is one.
- ``renamed file posts no NodeReceived``, ``moved file posts no NodeReceived``, and ``renamed and modified file posts no NodeReceived`` cover an already received file.
- ``upload lands and marks Unparsed inside the workspace lock`` still checks the lock order. The file is already Current, so the notice inside the lock is `Notice.NodeUnparsed`.
- `bash scripts/check-test.sh 7644` prints `DONE` and `/tmp/ambit-full-suite-20261111-002216.log`.
- Server.Tests passed 771, skipped 1, total 772.
- Shared.Tests passed 1937, skipped 3, total 1940.

## 5. Miss

- No findings.

## 6. Found elsewhere

- **Problem.** [Workspace upload structure](../../../src/Shared/WorkspaceUploadStructure.fs) `planServerFilePresentOps` returns no ops for `PullingFile`, `Current`, and `Unparsed`.
- **Fix.** Restore the incoming-file ops in a ticket that owns that planner. Alan has not confirmed this fix. This round did not change that planner.
- **Problem.** [07 — Git changed list sets Unparsed](../issues/07-git-changed-list-unparsed.md) was still `defined` while [12 — Git pull marks each received file Unparsed](../../parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md) is `done` and [13 — Git pull marks containing Directories Unparsed](../../parse-thread/issues/13-git-pull-marks-containing-directories-unparsed.md) is `coded`.
- **Fix.** Alan, 2026-11-10: 07 is superseded by 12 and 13. Already written on the ticket.
