# 08 — Amend a change conflict before the graph is stored
**Status**: defined
One job: The Server renames an incoming File or Directory when that node's destination path already exists, and it stores the renamed graph. Story 9 Change conflict in [Node move and disk](plan/node-move-disk/spec.md) is that rename, and the file already at the destination stays in place.

Type: coding
Blockers: [06 — Keep a nested node whose derived path differs](06-keep-a-nested-node-whose-derived-path-differs.md)

## Inputs

### Working

#### Accept validation

[ ] `DocumentPersistChange.findIncomingPathConflicts` is new in `src/Server/DocumentPersistChange.fs`.
[ ] `findIncomingPathConflicts` has type `string -> Graph -> Graph -> Result<NodeId list, string>`.
[ ] The arguments are the data directory, the pre graph, and the post graph.
[ ] `findIncomingPathConflicts` plans moves with `DocumentPathMove.planPathMovesBetweenGraphs` and `DocumentPathMove.coalescePathMoves`.
[ ] A move whose destination full path exists, and whose destination full path differs from the source full path, contributes that `nodeId`.
[ ] A move whose source and destination full paths are the same contributes no id.
[ ] A system-directory Error and a path-resolve Error return Error.
[ ] `DocumentPersistChange.conflictRenameNames` is new in `src/Server/DocumentPersistChange.fs`.
[ ] `conflictRenameNames` has type `string -> Graph -> NodeId list -> Result<(NodeId * string * string) list, string>`.
[ ] The `Graph` is the post graph.
[ ] Each triple is the node id, the post-graph file name, and the disambiguated file name.
[ ] The disambiguated name keeps the same parent path.
[ ] The candidate names come from `GraphQuery.unusedOwnedName` under that node's owner parent, with each rejected lower-case name passed in `reservedLowers`.
[ ] A candidate whose destination full path exists is rejected, and the loop asks `unusedOwnedName` for the next name.
[ ] The first candidate whose destination full path is absent is the new name.
[ ] `DocumentPersistChange.validatePathMoves` keeps type `string -> Graph -> Graph -> Result<unit, string>`.
[ ] `validatePathMoves` still returns `disk path already exists` for `executePathMoves` and `persistGraphChange`.

#### Change amendment

[ ] `ChangeAmendment.pathConflictOps` is new in `src/Shared/ChangeAmendment.fs`.
[ ] `pathConflictOps` has type `(NodeId * string * string) list -> Op list`.
[ ] Each triple becomes `Op.SetName(nodeId, oldName, newName)` in the same order.
[ ] `pathConflictOps` takes no data directory and reads no disk.
[ ] `ChangeAmendment.applyPosted` keeps type `bool -> EventId -> Ev -> Op list -> GraphAtEvent -> ApplyResult * bool * Op list`.
[ ] The compare-and-swap amendment already inside `applyPosted` stays the source-and-destination disagree path.

#### Post change

[ ] `FileAgent.processPostEvents` in `src/Server/Core/FileAgent.fs` is private and has type `LoadedFile -> Ev list -> bool -> Result<CoreChangesAccepted, string>`.
[ ] On a persisted change, `processPostEvents` calls `findIncomingPathConflicts` with `loaded.dataDir` after `applyBatch` and before `loaded.state` is stored.
[ ] `DbAgent.finishAppliedPostChange` in `src/Server/Core/DbAgent.fs` is private.
[ ] When `liveSaveDataDir` is `Some dataDir`, `finishAppliedPostChange` calls `findIncomingPathConflicts` with that data directory before `loaded.state` is stored.
[ ] When `liveSaveDataDir` is `None`, `finishAppliedPostChange` stores the posted graph and skips `findIncomingPathConflicts`.
[ ] A graph-only change skips `findIncomingPathConflicts` in both agents.
[ ] On Error, the agent returns that Error and leaves `loaded.state` as it was.
[ ] On a name list, the agent applies each `Op.SetName` with `Graph.setName`, appends those ops to the confirmation event, and stores that graph.
[ ] When the name list is non-empty, the accepted result has `externalChanges` true.
[ ] The bytes at the original destination path stay on disk.

#### Copy-from

[ ] `GraphQuery.unusedOwnedName` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> string -> Set<string> -> string`.
[ ] Private `numberedSiblingName` in that function builds `notes.txt`, then `notes1.txt`, then `notes2.txt`.
[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` calls `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent` and reads `handlers.graphAtEvent ()`.
[ ] `DbAgent.persist` in `src/Server/Core/DbAgent.fs` returns the handlers for `DbAgent.createForTestWithDependencies`.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no existing public signature.
[ ] `FileAgent.validatePostChange` calls `validatePathMoves` today. This ticket changes that call to `findIncomingPathConflicts` for the destination check.
[ ] `DbAgent.validatePostChange` calls `validatePathMoves` when `liveSaveDataDir` is `Some`. This ticket changes that call the same way.
[ ] `FileAgent.applyOne` and `DbAgent.applyOneEvent` keep calling `applyPosted`.
[ ] `executePathMoves` keeps calling `validateDestinationAvailable`.

#### Tests the ticket leaves as they are

[ ] ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in `tests/Server.Tests/DocumentPersistChangeTests.fs` expects a system-directory Error.
[ ] ``persistGraphChange rejects destination artifact conflict`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` expects `persistGraphChange` to return Error when the destination exists.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Accept validation**, **Change amendment**, and **Post change**, §4 flow 5 **Change conflict**, §6 seam 9, and §8 questions 3, 4, and 6.
[ ] [08 — Amend a change conflict before the graph is stored scope](plan/node-move-disk/reports/08-amend-a-change-conflict-before-the-graph-is-stored-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/WorkspaceGit.fs` stays as it is. This ticket runs no `gitMove` and no `moveAcrossWorkspaces`.
[ ] `src/Server/NodeLockPool.fs` stays as it is.
[ ] `src/Shared/GraphQuery.fs` stays as it is. `unusedOwnedName` stays the name picker.
[ ] `executePathMoves` and `persistGraphChange` stay on `validatePathMoves`.
[ ] `src/Server/Core/PersistThread.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Server/DocumentPersistChange.fs` with `findIncomingPathConflicts` and `conflictRenameNames` as Working specifies.
3. [ ] Expand `src/Shared/ChangeAmendment.fs` with `pathConflictOps` as Working specifies.
4. [ ] Change `src/Server/Core/FileAgent.fs` so `processPostEvents` amends, then stores, as Working specifies.
5. [ ] Change `src/Server/Core/DbAgent.fs` so `finishAppliedPostChange` amends, then stores, as Working specifies.
6. [ ] Expand `tests/Server.Tests/DocumentPersistChangeTests.fs` with the three tests in Outputs.

## Outputs

[ ] `src/Server/DocumentPersistChange.fs` — `findIncomingPathConflicts` and `conflictRenameNames`.
[ ] `src/Shared/ChangeAmendment.fs` — `pathConflictOps`.
[ ] `src/Server/Core/FileAgent.fs` — `processPostEvents` stores the amended graph.
[ ] `src/Server/Core/DbAgent.fs` — `finishAppliedPostChange` stores the amended graph when a data directory is set.
[ ] Distinct source files: 4. `src/Server/DocumentPersistChange.fs`, `src/Shared/ChangeAmendment.fs`, `src/Server/Core/FileAgent.fs`, and `src/Server/Core/DbAgent.fs`.
[ ] `tests/Server.Tests/DocumentPersistChangeTests.fs` — the three tests below.
[ ] ``a change conflict is amended before the graph is stored`` — a File rename would land on `notes.txt`, and that path already exists on disk. `FileAgent` `applyEvent` returns `externalChanges` true. The stored graph names that File `notes1.txt`. The confirmation event ops include `Op.SetName` to `notes1.txt`. The bytes at `notes.txt` stay on disk.
[ ] ``DbAgent amends a change conflict when a data directory is set`` — `DbAgent.createForTestWithDependencies` with `Some dataDir` stores the incoming File as `notes1.txt` and leaves the bytes at `notes.txt`.
[ ] ``DbAgent stores the posted name when no data directory is set`` — `createForTestWithDependencies` with `None` stores the incoming File as `notes.txt`.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/08-amend-a-change-conflict-before-the-graph-is-stored-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `applyPosted`, `validatePathMoves`, `executePathMoves`, `GraphQuery.unusedOwnedName`, `gitMove`, or `moveAcrossWorkspaces`.
[ ] Stop when the edit contradicts story 9 Change conflict in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``a change conflict is amended before the graph is stored``
  - `tests/Server.Tests`: ``DbAgent amends a change conflict when a data directory is set``
  - `tests/Server.Tests`: ``DbAgent stores the posted name when no data directory is set``
  - `tests/Server.Tests`: ``validatePathMoves refuses rename of non-allowlisted SYSTEM file``
  - `tests/Server.Tests`: ``persistGraphChange rejects destination artifact conflict``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/08-amend-a-change-conflict-before-the-graph-is-stored-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
