Check: pass

# 32 — Route plain Save

Spec: [[../../spec.md]]

Arch: [[../../arch.md]]

## Stage

Stage is refactor in [32 — Route plain Save](CONTEXT.md).

## 1. Path choice

1. **resolveCommand.** `PathPick.resolveCommand` in `src/Shared/PathPick.fs` matches `LoadSavePrePick`. Plain and Git return `LoadSavePath.Git`. Desk returns `LoadSavePath.Desk`.
2. **plainSavePath.** `PathPick.plainSavePath` returns Desk when the pre-pick is Plain, the workspace label is in `localLabels`, and that label is absent from `githubLabels`.
3. **Shared focus.** `src/Client/UpdateHelpers.fs` holds the shared readers.

   - `commandFocusId` is the selection focus, or the zoom root when the selection is empty.
   - `selectionSpansWorkspaces` is the one-workspace check.
   - `workspaceMappingPresence` is the remote labels and the local labels.

   `prePickForSave` in `src/Client/UpdateSave.fs` and `prePickForLoad` in `src/Client/UpdateWorkspaceLoad.fs` call those three. `loadSaveCommandRequest` uses `commandFocusId`.

## 2. Save actor

1. **Persist.** `runSaveBody` in `src/Server/GithubTransportActor.fs` calls `persistFocusWhenNotOrdinary`.
2. **Commit.** `commitSaveRepository` calls `withWorkspaceLock`, then `GitSave.commitAll` with `saveCommitMessage`. A missing repository skips that commit.
3. **Start.** `LoadSaveStart.startCommand` in `src/Server/LoadSaveStart.fs` calls `startDeskCommand` for Desk and `startGit` otherwise.

## 3. Desk Save and download

1. **Desk Save.** `deskSaveOp` in `src/Client/UpdateSave.fs` posts `ContinueDeskSave` when `canGitSave` is true.
2. **Download.** `downloadOp` in `src/Client/UpdateWorkspaceDownload.fs` posts the workspace download. `pollWorkspaceDownloadJob` records `displayName Save`.

## 4. Suite

1. **Command.** `bash scripts/check-test.sh 4125` printed `DONE` and `/tmp/ambit-full-suite-20261113-170811.log`. The log ends `full suite finished`.
2. **Server.** `tests/Server.Tests` passed 769, skipped 1, failed 0, total 770. The skip is ``Change.upload structure persistence preserves existing file mtime``.
3. **Shared.** `tests/Shared.Tests` passed 1944, skipped 3, failed 0, total 1947. The skips are

   - ``reuses existing owned path without duplicating``
   - ``planServerFilePresentOps transitions only matching absent files``
   - ``listForPush without git still returns walk``

## 5. Scan

1. **Bindings.** `python3 scripts/standards-scan.py --diff origin/staging` prints the measure block. Each measured binding is 40 lines or under.
