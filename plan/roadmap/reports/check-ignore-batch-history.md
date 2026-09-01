# Per-path `git check-ignore` history

Question: did the product already remove one `git check-ignore` process per path? The user asked to search logs for that idea.

## Verdict

Batch `GitCheckIgnore.classify` (`check-ignore --stdin`) exists since 2026-07-21. Desktop Upload inventory and WebDAV `PROPFIND` use it (or `listIncluded`). [[src/Server/IgnoredDestination.fs]] on `POST /ambit/changes` now also calls `classify` once per work tree for new document-root destinations (empty list: no git process). The old ~500-process FileAgent fold is gone. Remaining per-path `isIgnored` is WebDAV PUT omit, not this Change path. Detail: [[ignored-destination-batch-classify.md]].

## (a) Commits and docs that claimed a per-path git was gone

No commit message contains `check-ignore`, `per-path`, `per path`, `one git`, or `GitCheckIgnore`. Pickaxe and file logs still show the work.

| When | Commit | What it actually did |
| --- | --- | --- |
| 2026-07-13 | `0d1e722` Complete throughline on text file reconciliation. | Added [[src/Server/IgnoredDestination.fs]] with private `checkIgnored`: one `git check-ignore -q --no-index -- <path>` per destination in a fold. |
| 2026-07-21 | `ebebbf0` webdav | Moved that helper to [[src/Shared/dotnet/GitCheckIgnore.fs]] as `isIgnored`. Added `classify` (`--stdin` `-z`) in the same file. IgnoredDestination switched to `isIgnored`; it did not switch to `classify`. |
| 2026-07-22 | `9d8309f` fixing webdav push/pull and parse issues. | Test `classify large ignored set does not pipe-deadlock` (2500 paths). [[src/Shared/dotnet/ProcessExec.fs]] `runCapture` drains stdout before a large stdin write. This is batch stdin, not IgnoredDestination. |
| 2026-07-22 | `4e0ef90` compressing code into GitRun | Process helper only. Still `isIgnored` per path in IgnoredDestination. |
| 2026-08-04 | `d97dae7` cleaning up / updating git behaviors. | Shared empty `GIT_DIR` recover. Both `isIgnored` and `classify` kept. |
| 2026-08-06 | `9992ec5` fix Upload performance. | Strongest “we already fixed git spawn cost” commit. [[src/Server/WorkspaceWebDav.fs]] `PROPFIND` `listChildren` stopped folding `isOmitted` (per-child `isEffectivelyIgnored`). It now uses one `listIncluded` (`git ls-files -o --exclude-standard`) plus one `classify` for empty/uncertain dirs. WORK.md dropped a Depth=infinity PROPFIND / collectEntries cost item. **Not** `POST /ambit/changes`. |

Later inventory: `GitCheckIgnore.classify` on the full walk in [[src/Shared/dotnet/WorkspaceLocalInventory.fs]] `applyIgnoreFilter` (callers added around `ebebbf0` / `2ef406c` / `52a9301`). That is desktop Push inventory, not FileAgent.

Docs: [[doc/roadmap/workspace-file-sync.md]] and [[doc/roadmap/workspaces-checklist.md]] say ignore uses `git check-ignore` and the IgnoredDestination pattern. They do not say per-path spawn was removed from graph Changes. No Decision under [[doc/Decisions/]] covers this.

## (b) Live code on `POST /ambit/changes` disk-effect validation

[[src/Server/FileAgent.fs]] `handlePostChange` (same idea in [[src/Server/DbAgent.fs]]): after `applyBatch`, unless `graphOnly`, `DocumentPersistence.validatePathMoves` then `DocumentPersistence.validateGraphDiskEffects`.

[[src/Server/DocumentPersistence.fs]] `validateGraphDiskEffects` only forwards to `IgnoredDestination.validateGraphDiskEffects`.

That function:

1. `destinationEffects`: scan all `postGraph.nodes`. Keep document-root File / Workspace / Directory artifacts whose relative path is new or changed. Skip Directory File names.
2. Skip `.gitignore` paths and empty scoped rel. Group remaining paths by work tree.
3. One `GitCheckIgnore.classify` per work tree. Then walk node-id order and return the first ignored destination error.

`classify` starts `git` with `check-ignore --no-index --stdin -z` and the full relative list. Tests in [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]] cover create/rename/reparent and a many-destination batch (ignored vs allowed, `.gitignore` kept when `.*` would ignore it).

`validatePathMoves` is filesystem destination-available checks, not git.

## (c) Is “~500 git processes” still true?

**No** for `POST /ambit/changes` disk-effect validation. One `classify` per distinct work tree (typical Upload stub Change: one named Workspace). FileAgent still has no time bound on that step.

The **count ~500** was an estimate from payload shape in [[changes-post-timeout.md]]. That spawn shape is no longer live.

WebDAV `PROPFIND` and desktop inventory already batched. `isOmitted` still uses `isEffectivelyIgnored` per path for PUT omit, which is one process per PUT, not hundreds per listing.

## Callers today

- Per path `isIgnored` / `isEffectivelyIgnored`: [[src/Server/WorkspaceWebDav.fs]] `isOmitted`.
- Batch `classify`: [[src/Shared/dotnet/WorkspaceLocalInventory.fs]] `applyIgnoreFilter`; [[src/Server/WorkspaceWebDav.fs]] `keepUncertainDirs`; [[src/Server/IgnoredDestination.fs]] `validateGraphDiskEffects`.
- One `listIncluded`: WebDAV `collectEntries` for Depth 1 / infinity.

## Phrase search

`git log --grep` for `check-ignore`, `per-path`, `one git`, `GitCheckIgnore`: empty. The user’s phrase is not in commit subjects. The nearest subject is `fix Upload performance.` (`9992ec5`). Plan/doc hits for the old unbounded per-path check-ignore are [[changes-post-timeout.md]] and this file.
