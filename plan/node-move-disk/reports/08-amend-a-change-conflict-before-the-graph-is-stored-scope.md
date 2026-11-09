# 08 — Amend a change conflict before the graph is stored (scope)

Slice: Flow 5 **Change conflict** in [arch.md](../arch.md) section 4 and section 8 questions 3, 4, and 6. Post change runs Accept validation, hands conflicting nodes to Change amendment, emits `Op.SetName` on each incoming move or rename (file name only), stores the amended graph. Seam 9 in `tests/Server.Tests/DocumentPersistChangeTests.fs`. No `gitMove`, `moveAcrossWorkspaces`, or node lock in this slice.

## Signatures

[ ] `DocumentPersistChange.validatePathMoves` is `string -> Graph -> Graph -> Result<unit, string>` in `src/Server/DocumentPersistChange.fs`.
[ ] `DocumentPersistChange.validatePathMoves` calls private `validateDestinationAvailable`, which returns `Error $"disk path already exists: {newFullPath}"` when the destination path exists and differs from the source.
[ ] `DocumentPersistChange.executePathMoves` still calls `validateDestinationAvailable` for disk relocate; that path stays an Error in this slice because Post change does not relocate here.
[ ] `DocumentPersistChange.validateGraphDiskEffects` is `string -> Graph -> Graph -> Result<unit, string>` and delegates to `IgnoredDestination.validateGraphDiskEffects`.
[ ] `ChangeAmendment.applyPosted` is `bool -> EventId -> Ev -> Op list -> GraphAtEvent -> ApplyResult * bool * Op list` in `src/Shared/ChangeAmendment.fs`.
[ ] `FileAgent.processPostEvents` is `LoadedFile -> Ev list -> bool -> Result<CoreChangesAccepted, string>` (private).
[ ] `FileAgent.validatePostChange` is `LoadedFile -> bool -> Graph -> Graph -> Result<unit, string>` (private).
[ ] `DbAgent.finishAppliedPostChange` is `LoadedPersist -> bool -> GraphAtEvent -> Ev list -> Ev list -> bool -> Result<CoreChangesAccepted, string>` (private).
[ ] `DbAgent.validatePostChange` is `LoadedPersist -> bool -> Graph -> Graph -> Result<unit, string>` (private).
[ ] `Op.SetName` is `SetName of nodeId: NodeId * oldName: string * newName: string` in `src/Shared/History.fs`.
[ ] `Filename` is `Empty | Invalid of string | Ok of string` in `src/Shared/Filename.fs`; node names use `Filename` on `Node`.
[ ] Recommend a new public `DocumentPersistChange` function (for example `findIncomingPathConflicts`) with `string -> Graph -> Graph -> Result<NodeId list, string>` that lists incoming document roots whose derived destination full path already exists on disk, without emitting `disk path already exists` for the post-change path.
[ ] Recommend a new pure public `ChangeAmendment` function (for example `pathConflictRenameOps`) with `Graph -> NodeId list -> Op list` that builds `Op.SetName` for each incoming node; it takes no data directory.
[ ] Keep `ChangeAmendment.applyPosted` signature unchanged; post-change path-conflict amendment runs after `applyPosted` in the agents, not inside it.

## Match sites

[ ] `DocumentPersistChange.validatePathMoves` is defined in `src/Server/DocumentPersistChange.fs` in module `DocumentPersistChange`.
[ ] Private `validateDestinationAvailable` in `DocumentPersistChange` matches on `pathExists newFullPath` before the `disk path already exists` Error.
[ ] `FileAgent.validatePostChange` in `src/Server/Core/FileAgent.fs` calls `DocumentPersistChange.validatePathMoves loaded.dataDir` then `validateGraphDiskEffects` when `graphOnly` is false.
[ ] `FileAgent.processPostEvents` in `FileAgent.fs` calls `applyBatch`, then `validatePostChange loaded graphOnly preGraph newState.graph`, then assigns `loaded.state.Value <- newState` on Ok.
[ ] `FileAgent.applyOne` in `FileAgent.fs` calls `ChangeAmendment.applyPosted` inside the `Ev.ops` branch.
[ ] `FileAgent.coreMailboxAccess` exposes `applyEvent = fun event graphOnly -> processPostEvents loaded [ event ] graphOnly` in `FileAgent.fs`.
[ ] `DbAgent.validatePostChange` in `src/Server/Core/DbAgent.fs` calls `validatePathMoves` only when `liveSaveDataDir` is `Some dataDir`; returns `Ok ()` when `graphOnly` is true or `liveSaveDataDir` is `None`.
[ ] `DbAgent.finishAppliedPostChange` calls `validatePostChange` with `preGraph = loaded.state.Value.graph` and `newState.graph`, then `commitPostChange` which sets `loaded.state.Value <- stateToStore` on Ok.
[ ] `DbAgent.applyOneEvent` in `DbAgent.fs` calls `ChangeAmendment.applyPosted`.
[ ] `DbAgent.processPostEvents` in `DbAgent.fs` calls `applyBatch` then `finishAppliedPostChange` when `newState.eventId` differs from loaded.
[ ] `DbAgent` mailbox `applyEvent` in `DbAgent.fs` forwards to `processPostEvents loaded [ event ] graphOnly`.
[ ] `CoreEventDispatch` persists through `context.persist.applyEvent` in `src/Server/Core/CoreEventDispatch.fs` (routes to FileAgent or DbAgent handlers).
[ ] Ripple if `validatePathMoves` signature changes: `FileAgent.validatePostChange`, `DbAgent.validatePostChange`, test ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in `tests/Server.Tests/DocumentPersistChangeTests.fs`.
[ ] Prefer no `validatePathMoves` signature change: add `findIncomingPathConflicts` (or equivalent) and branch only in `validatePostChange` / a small private post-change helper in each agent.
[ ] Ripple for new post-change amend loop: only `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` (and possibly private helpers beside `validatePostChange`); `applyPosted` callers stay as `FileAgent.applyOne` and `DbAgent.applyOneEvent`.

## Existing tests

[ ] ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in `tests/Server.Tests/DocumentPersistChangeTests.fs` calls `DocumentPersistChange.validatePathMoves` directly and expects a system-directory Error, not `disk path already exists`.
[ ] ``persistGraphChange rejects destination artifact conflict`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` expects `persistGraphChange` to fail with `already exists` on disk relocate; this slice does not change that test because relocate stays out of scope.
[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` uses `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent` and `handlers.graphAtEvent ()` to read stored graph state after post change.
[ ] ``DbAgent applyEvent applies Ev Ops without leftover Change`` in `PersistApplyTests.fs` uses `DbAgent.createForTest` handlers the same way (no `liveSaveDataDir`, so path-conflict check is skipped today).
[ ] `ChangeAmendmentTests` in `tests/Shared.Tests/ChangeAmendmentTests.fs` exercise CAS `applyPosted` amendment via private `applyPosted` helper; none cover disk path conflict.
[ ] Seam 9 — **Change amendment / Post change** from [arch.md](../arch.md) section 6: ``a change conflict is amended before the graph is stored`` in `DocumentPersistChangeTests.fs` (not present in the tree yet).

## Copy-from

[ ] `GraphQuery.unusedOwnedName` and private `numberedSiblingName` in `src/Shared/GraphQuery.fs` pick the next sibling file name (`base`, `base1.ext`, `base2.ext`, …).
[ ] `ViewModelDeleteOps` move-to-trash fold uses `GraphQuery.unusedOwnedName` and emits `Op.SetName(item.child.id, baseName, newName)` in `src/Shared/ViewModelDeleteOps.fs`.
[ ] `FileNodeOps.planCreateOwnedSpecial` uses `GraphQuery.unusedOwnedName` when creating owned files in `src/Shared/FileNodeOps.fs`.
[ ] CAS path-conflict child insert pattern `ambConflictChildOps` in `ChangeAmendment.fs` is for text/name CAS, not file-name disambiguation on disk; do not copy for this slice.
[ ] Post-change test wiring pattern: `PersistApplyTests.fs` `applyThenAppend` and `handlers.graphAtEvent ()` after `handlers.applyEvent event false`.
[ ] [arch.md](../arch.md) question 4 does not name the disambiguating file-name characters.
[ ] Recommend deterministic suffix: reuse `GraphQuery.numberedSiblingName` style on the incoming base name (`notes.txt` → `notes1.txt`, `notes2.txt`, …) under the same parent derived path; loop until neither graph siblings nor the on-disk full path collide.
[ ] No existing server disambiguator searches for `"copy"` or `-2` in path-conflict code; `GraphQuery.ownedNameTaken` and `unusedOwnedName` are the graph-side collision helpers.

## write-ticket

[ ] Flow 5 order in [arch.md](../arch.md) section 4: Post change → Change amendment (path conflict) → Accept validation → store; amendment runs before `loaded.state` is stored, not inside `persistGraphChange`.
[ ] Question 3: conflict is an on-disk file at the incoming node’s derived destination full path (path comparison), not two concurrent changes.
[ ] Question 4: amend the incoming node with `Op.SetName`; leave the existing file at the destination unchanged.
[ ] Question 6: disk access stays in `FileAgent` / `DbAgent` via `dataDir` or `liveSaveDataDir`; `ChangeAmendment` stays pure; DbAgent with `liveSaveDataDir = None` skips conflict check.
[ ] Mark `disk path already exists` from `validatePathMoves` obsolescent on the post-change path only; other validation Errors (SYSTEM refuse, graph disk effects, and relocate-time checks) remain Errors.

### Source files

[ ] `src/Server/DocumentPersistChange.fs` — split or extend validation so post change can list conflicting incoming roots; optionally a disk-aware disambiguation helper that keeps the same parent path.
[ ] `src/Shared/ChangeAmendment.fs` — new pure function that returns `Op.SetName` list for incoming node ids given the post-apply graph.
[ ] `src/Server/Core/FileAgent.fs` — after `applyBatch`, detect conflicts, call Change amendment, re-apply rename ops into `newState` / confirmation ops, then store; no relocate.
[ ] `src/Server/Core/DbAgent.fs` — same amend loop in `finishAppliedPostChange` when `liveSaveDataDir` is `Some`; no-op conflict path when `None`.

### Tests

[ ] Add ``a change conflict is amended before the graph is stored`` to `tests/Server.Tests/DocumentPersistChangeTests.fs` (seam 9).
[ ] That test should seed a data directory, post a Change that would collide on disk (occupied destination path), call `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent event false`, then read `(FileAgent.persist ...).handlers.graphAtEvent ()` for the stored graph and assert an amended file name on the incoming node (and optionally amended ops on the accepted event via `getEventsSince` if the test asserts landed ops).
[ ] Alternative public seam with the same behavior: `CoreMailboxApi.postEvents` plus `CoreMailboxApi.graphAtEvent` on a `CoreMailbox.createFile` host (used widely in `CoreMailboxDoorTests.fs`); prefer FileAgent handlers in `DocumentPersistChangeTests.fs` to match `PersistApplyTests.fs` without moving the test file.
[ ] Optional unit test in `tests/Shared.Tests/ChangeAmendmentTests.fs` for the new pure rename-op builder (counts toward the test budget if added).
[ ] ``persistGraphChange rejects destination artifact conflict`` in `DocumentPathMoveExecutionTests.fs` does not change for this ticket.
[ ] ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` does not change for this ticket unless `validatePathMoves` signature or SYSTEM checks move.
[ ] Test budget for this slice: at most 4 test changes; source budget: at most 8 source files (4 source files listed above).
