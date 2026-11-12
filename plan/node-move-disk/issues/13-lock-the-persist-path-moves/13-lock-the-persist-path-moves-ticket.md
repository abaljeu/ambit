# 13 — Lock the persist path moves
**Status**: coded
One job: The Server holds `NodeLockPool.withLock` around each coalesced path move in `persistGraphChangeWith`, so `File.Move` and `Directory.Move` wait on the node lock. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait. The git relocation stays on later tickets.

Type: coding
Blockers: [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md), [12 — Compare locks on the live graph](../12-compare-locks-on-the-live-graph/12-compare-locks-on-the-live-graph-ticket.md), [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)

11 — Expand the node lock pool, 12 — Compare locks on the live graph, and 31 — Migrate onto the workspace lock have passed review. This ticket waits on no `defined` ticket.

## Scope

### Signatures

[x] `DocumentPathMove` in `src/Shared/DocumentPathMove.fs` is `{ nodeId: NodeId; oldPath: string; newPath: string }`.
[x] `DocumentPathMove.planPathMovesBetweenGraphs` has type `Graph -> Graph -> DocumentPathMove list`.
[x] `DocumentPathMove.coalescePathMoves` has type `Graph -> DocumentPathMove list -> DocumentPathMove list`.
[x] `DocumentPersistChange.persistGraphChangeWith` is `private` in `src/Server/DocumentPersistChange.fs` with type `(NodeId list -> Set<NodeId>) -> (NodeId list -> NodeId list) -> string -> Graph -> Graph -> Result<PersistGraphOk, string>`.
[x] `DocumentPersistChange.executePathMoves` has type `string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>`.
[x] `DocumentPersistChange.executePathMove` is `private` with type `(bool * string) -> (bool * string) -> Result<unit, string>`.
[x] `DocumentPersistChange.persistGraphChange` has type `string -> Graph -> Graph -> Result<PersistGraphOk, string>`.
[x] `DocumentPersistChange.persistGraphOps` has type `string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>`.
[x] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[x] `NodeLockPool.withLockResult` is `internal` with type `Graph -> NodeId -> (unit -> Result<'value, string>) -> Result<'value, string>`.
[x] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>`.

### Match sites

[x] `DocumentPersistChange.persistGraphChange` in `src/Server/DocumentPersistChange.fs` calls `persistGraphChangeWith` with `DocumentPartition.documentRootsAffectedByGraphChange` and `DocumentPersistPath.enumerateDocumentRoots`.
[x] `DocumentPersistChange.persistGraphOps` in the same file calls `persistGraphChangeWith` with `DocumentOpImpact.documentRootsAffectedByOps` and `id`.
[x] `DocumentPersistChange.persistGraphChangeWith` calls `DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph`, then `executePathMoves dataDir preGraph postGraph moves`, then `DocumentPersistWrite.writeDocumentsSoft dataDir postGraph`, then `DocumentPersistPath.stampExistingDocuments`.
[ ] `DocumentPersistChange.executePathMoves` pipes `moves` through `DocumentPathMove.coalescePathMoves preGraph`, folds with `resolveMovePaths` and `executePathMove`.
[x] `DocumentPersistChange.executePathMove` calls `Directory.Move(oldFullPath, newFullPath)` when `oldIsDirectory` and `Directory.Exists oldFullPath`, and `File.Move(oldFullPath, newFullPath)` when not a directory and `File.Exists oldFullPath`.
[x] `DocumentPathMove.planPathMovesBetweenGraphs` constructs `{ nodeId; oldPath; newPath }` records when `expandedPathForNodeId` differs between graphs.
[x] `DocumentPersistChange.validatePathMoves` also calls `planPathMovesBetweenGraphs` and `coalescePathMoves` before disk checks; it does not call `executePathMove`.
[ ] `src/Server/DocumentPersistChange.fs` has no reference to `NodeLockPool` today.

### Existing tests

[x] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` writes `graphWithNestedDocs`, renames the File with `Graph.setName`, calls `persistGraphChange`, and asserts the old file path is absent and the new file path exists under `dataDir`.
[x] ``persistGraphChange moves renamed directory tree before writing`` in the same file renames the Directory `docs` to `archive`, calls `persistGraphChange`, and asserts the old directory is absent and `.amb` plus nested `readme.txt` exist under the new directory.
[x] ``persistGraphChange rejects destination artifact conflict`` in the same file pre-creates the destination file, expects `persistGraphChange` to return Error containing `already exists`, and leaves both source and destination files on disk.
[x] ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs` holds `withLock` on a descendant File, keeps Directory `withLock` outside, and lets a sibling File `withLock` enter before the Directory enters after release.
[x] ``an absent node proceeds and the same thread is rejected`` in the same file runs `absentProceeds` then `holdRejectsSibling`, which asserts absent-id `withLock` proceeds, same-thread second `withLock` returns `Error NodeLockRejection.HolderHoldsLock`, the sibling action stays uncalled, and `entryCount` stays 1 until release.

### Copy-from

[x] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` wraps `writeDocumentCore` in `NodeLockPool.withLockResult graph documentRootId`.
[x] `NodeLockPool.withLockResult` maps `Error NodeLockRejection.HolderHoldsLock` to `Error "holder already holds a lock"` in `src/Server/NodeLockPool.fs`.
[x] `NodeLockPool.enqueue` returns `Error NodeLockRejection.HolderHoldsLock` when any pool entry shares `Environment.CurrentManagedThreadId` with the new request.
[x] `NodeLockPool.withLock` on a `nodeId` absent from `graph.nodes` runs the action with no queue entry and returns `Ok` immediately.

### implement

[ ] [13 — Lock the persist path moves](plan/node-move-disk/issues/13-lock-the-persist-path-moves/13-lock-the-persist-path-moves-ticket.md) claims `persistGraphChangeWith` holds `NodeLockPool.withLock` around each coalesced move before `File.Move` or `Directory.Move`; the code calls `executePathMoves` with no lock wrapper.
[ ] The ticket claims the lock call is `NodeLockPool.withLockResult` on the post graph and `move.nodeId`; `DocumentPersistChange.fs` imports and calls neither `withLock` nor `withLockResult`.
[ ] The ticket claims each coalesced move runs one at a time with lock release before the next move; `executePathMoves` already folds moves in order but without locks.
[x] The ticket claims on Error the function returns that Error and runs no later move; `executePathMoves` already short-circuits the fold on Error.
[x] The ticket claims call sites of `persistGraphChange` and `persistGraphOps` outside their definitions are only in `tests/Server.Tests`; `src/` has no callers, and `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`, `DocumentPersistChangeTests.fs`, and `DocumentOpPersistenceTests.fs` call them.
[ ] The four output tests ``persist path move waits while the moved file is held``, ``persist path move waits while a file under the moved directory is held``, ``a path move on a thread that already holds a lock leaves the artifact``, and ``a sibling file lock lets a file path move finish`` are absent from `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
[x] When a path move would take `withLockResult`, the persist path does not already hold a pool lock: `persistGraphChangeWith` runs `executePathMoves` before `writeDocumentsSoft`, and only `writeDocument` inside the write phase calls `withLockResult`.
[x] ``persist path move waits while the moved file is held`` will call `NodeLockPool.withLock` on the moved File inside its action, call `DocumentPersistChange.persistGraphChange` from outside that hold for a File rename on `graphWithNestedDocs`, assert the old artifact path remains during the hold, then assert the new path exists after release.
[x] ``persist path move waits while a file under the moved directory is held`` will hold `withLock` on the nested File under `docs`, call `persistGraphChange` for a Directory rename on `graphWithNestedDocs`, assert the old directory path remains during the hold, then assert the directory new path exists after release.
[x] ``a path move on a thread that already holds a lock leaves the artifact`` will nest `withLock` on a sibling File in its action on the calling thread, call `persistGraphChange` for another File rename, expect `Error "holder already holds a lock"`, and assert the source artifact path is unchanged with pool `entryCount` 1 on the first hold.
[x] ``a sibling file lock lets a file path move finish`` will hold `withLock` on a sibling File on another thread, call `persistGraphChange` for the other File rename during that hold, and assert the renamed artifact exists at the new path before the sibling lock releases.
[x] `graphWithNestedDocs` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` is a `private` function returning `Graph * NodeId * NodeId * NodeId * NodeId` for Workspace `home`, Directory `docs`, File `readme.txt`, and a body Normal node.
[x] `tests/Server.Tests/DocumentPersistenceFixtures.fs` exposes `graphWithNestedDocs` with the same nested shape for other Server test modules.

## Inputs

### Working

#### Path moves

[x] `persistGraphChangeWith` in `src/Server/DocumentPersistChange.fs` plans with `DocumentPathMove.planPathMovesBetweenGraphs` and moves with `executePathMoves`.
[x] `executePathMoves` coalesces with `DocumentPathMove.coalescePathMoves` and then calls `executePathMove`.
[x] `executePathMove` moves a directory with `Directory.Move` and a file with `File.Move` when the source path exists.
[x] `persistGraphChange` and `persistGraphOps` call `persistGraphChangeWith`.
[x] Call sites of `persistGraphChange` and `persistGraphOps` outside their definitions are in `tests/Server.Tests`.
[x] `persistGraphChangeWith` holds `NodeLockPool.withLock` around each coalesced move, and the move runs inside that action.
[x] The moves run one at a time. The action returns before the next move takes a lock.
[x] On Error, `persistGraphChangeWith` returns that Error and runs no later move.
[x] `File.Move` and `Directory.Move` stay the relocation.

#### Lock

[x] The call is `NodeLockPool.withLockResult`, which calls `NodeLockPool.withLock`.
[x] The graph argument is the post graph.
[x] The node id is `move.nodeId` on that coalesced move. A Directory move requests that Directory node. A File move requests that File node. A Workspace move requests that Workspace node.
[x] One request is that graph and that one node id. The caller passes no owner chain.
[x] A holder calls `withLock` once for that move. The action's return releases the request.
[x] A holder that already holds a pool lock and requests another gets `Error NodeLockRejection.HolderHoldsLock` immediately. `withLockResult` turns that into Error `holder already holds a lock`. The first entry stays. The move stays uncalled.
[x] The same thread is `Environment.CurrentManagedThreadId`.
[x] An absent node conflicts with nothing. `withLock` on an id absent from the passed graph runs the action and queues no entry, so that request blocks nothing.
[x] There is one graph. The pool stores no graph. Conflict checks read the graph passed with that request.

#### Open question — lock key

[x] **Node lock** names the moved node. This ticket uses `move.nodeId`.
[x] The strictly enclosing artifact starts at the parent and excludes the node. For a File Node under a Directory Node, that parent is the Directory Node. `GraphQuery.enclosingWorkspace` returns the Workspace id. Path class uses that Workspace id. **Node lock** does not pass either id to `withLock`.
[x] Those three ids differ for a File Node owned by a Directory Node owned by a Workspace Node.
[x] ``a sibling file lock lets a file path move finish`` is the reading of `move.nodeId`. A strictly enclosing Directory key, or an `enclosingWorkspace` key, waits on that sibling instead.
[x] A resolution that names the strictly enclosing artifact or `enclosingWorkspace` replaces `move.nodeId` before the build, and replaces that sibling test.

#### Copy-from

[x] `DocumentPersistWrite.writeDocument` calls `NodeLockPool.withLockResult` around the write.
[x] ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs` is the wait shape.
[x] ``an absent node proceeds and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` is the absent-node shape and the `HolderHoldsLock` shape.
[x] ``persistGraphChange moves renamed file artifact before writing`` and ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` are the move seam. `graphWithNestedDocs` is the graph builder.

#### Match sites

[x] This ticket adds no union case.
[x] This ticket changes no signature of `persistGraphChange`, `persistGraphOps`, `executePathMoves`, or `withLock`.
[x] This ticket adds no `sameWorkspaceMoves`, `crossWorkspaceMoves`, `gitMove`, `moveAcrossWorkspaces`, `repositoryRoot`, or `gitPath`.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Disk relocate** and §3 **Node lock**. §8 question 1 names the strictly enclosing artifact for where a File moves. §3 **Same-workspace relocate** names `GraphQuery.enclosingWorkspace` for path class.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] `src/Server/NodeLockPool.fs` stays as it is.
[x] `src/Shared/DocumentPathMove.fs` stays as it is.
[x] `src/Shared/GraphQuery.fs` stays as it is.
[x] `src/Server/WorkspaceGit.fs` and `src/Server/DocumentPersistPath.fs` stay as they are.
[x] `src/Server/DocumentPersistWrite.fs` stays as it is.
[x] `src/Server/ParseThread.fs` stays as it is.
[x] `src/Server/Core/PersistThread.fs` stays as it is.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[x] `src/Shared/ChangeAmendment.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `src/Server/DocumentPersistChange.fs` so `persistGraphChangeWith` runs each coalesced move inside `NodeLockPool.withLockResult` on the post graph and `move.nodeId`.
3. [x] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the four tests in Outputs.

## Outputs

[x] `src/Server/DocumentPersistChange.fs` — `persistGraphChangeWith` holds `withLock` around each coalesced path move.
[x] Distinct source files: 1. `src/Server/DocumentPersistChange.fs`.
[x] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the four tests below.
[x] ``persist path move waits while the moved file is held`` — `withLock` on the File is inside its action. `persistGraphChange` of that File rename stays outside, and the old path still exists. After release, the new path exists and the old path is absent.
[x] ``persist path move waits while a file under the moved directory is held`` — `withLock` on the File under the Directory is inside its action. `persistGraphChange` of that Directory rename stays outside, and the old directory still exists. After release, the Directory's new path exists.
[x] ``a path move on a thread that already holds a lock leaves the artifact`` — `withLock` on the sibling File is inside its action on the calling thread. `persistGraphChange` of the other File rename returns Error `holder already holds a lock`. The old path still exists. The first entry stays.
[x] ``a sibling file lock lets a file path move finish`` — another thread's `withLock` on the sibling File is inside its action. `persistGraphChange` of the other File rename returns during that hold, and the new path exists before the sibling releases.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/13-lock-the-persist-path-moves/13-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit adds `sameWorkspaceMoves`, `crossWorkspaceMoves`, `gitMove`, `moveAcrossWorkspaces`, `repositoryRoot`, or `gitPath`.
[ ] Stop when the lock key is the strictly enclosing artifact or `enclosingWorkspace` rather than `move.nodeId`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``persist path move waits while the moved file is held``
  - `tests/Server.Tests`: ``persist path move waits while a file under the moved directory is held``
  - `tests/Server.Tests`: ``a path move on a thread that already holds a lock leaves the artifact``
  - `tests/Server.Tests`: ``a sibling file lock lets a file path move finish``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/issues/13-lock-the-persist-path-moves/13-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
