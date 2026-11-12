Check: pass

# 23 — Run the Load actor for Git and Neither

Ticket: [[23-run-the-load-actor-for-git-and-neither-ticket.md]]
Review: [[23-review.md]]

## Stage

Stage is refactor in [23 — Run the Load actor for Git and Neither](CONTEXT.md).

## 1. Work tree

1. **LockedWorkspace.** `readFocusWorkspace` in `src/Server/GithubTransportActor.fs` returns `LockedWorkspace`. `runSaveBody` passes that record to `saveFetched`.
2. **Fields.** The record holds `graph`, `workspaceId`, and `root`. `readFocusWorkspace` is 15 lines. `runSaveBody` is 16 lines.

## 2. Suite

1. **Named modules.** `tests/Server.Tests` filter for `GithubTransportActorTests`, `LoadSaveCommandTests`, `ParseThreadLoadTests`, and `GithubTransportLoadPullTests` passed 70, failed 0, skipped 0. `tests/Shared.Tests` filter for `PathPickTests` passed 11, failed 0, skipped 0.
2. **Server.** `tests/Server.Tests` passed 778, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``. ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` passed.
3. **Shared.** `tests/Shared.Tests` passed 1936, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.

## 3. Scan

1. **Actor diff.** `python3 scripts/standards-scan.py --diff HEAD` prints no `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line for `src/Server/GithubTransportActor.fs`.
