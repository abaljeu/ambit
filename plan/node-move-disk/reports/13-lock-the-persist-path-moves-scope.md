# 13 — Lock the persist path moves

## Signatures

[ ] `DocumentPathMove` in `src/Shared/DocumentPathMove.fs` is `{ nodeId: NodeId; oldPath: string; newPath: string }`.
[ ] `DocumentPathMove.planPathMovesBetweenGraphs` has type `Graph -> Graph -> DocumentPathMove list`.
[ ] `DocumentPathMove.coalescePathMoves` has type `Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] `DocumentPersistChange.persistGraphChangeWith` is `private` in `src/Server/DocumentPersistChange.fs` with type `(NodeId list -> Set<NodeId>) -> (NodeId list -> NodeId list) -> string -> Graph -> Graph -> Result<PersistGraphOk, string>`.
[ ] `DocumentPersistChange.executePathMoves` has type `string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>`.
[ ] `DocumentPersistChange.executePathMove` is `private` with type `(bool * string) -> (bool * string) -> Result<unit, string>`.
[ ] `DocumentPersistChange.persistGraphChange` has type `string -> Graph -> Graph -> Result<PersistGraphOk, string>`.
[ ] `DocumentPersistChange.persistGraphOps` has type `string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>`.
[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] `NodeLockPool.withLockResult` is `internal` with type `Graph -> NodeId -> (unit -> Result<'value, string>) -> Result<'value, string>`.
[ ] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>`.

## Match sites

[ ] `DocumentPersistChange.persistGraphChange` in `src/Server/DocumentPersistChange.fs` calls `persistGraphChangeWith` with `DocumentPartition.documentRootsAffectedByGraphChange` and `DocumentPersistPath.enumerateDocumentRoots`.
[ ] `DocumentPersistChange.persistGraphOps` in the same file calls `persistGraphChangeWith` with `DocumentOpImpact.documentRootsAffectedByOps` and `id`.
[ ] `DocumentPersistChange.persistGraphChangeWith` calls `DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph`, then `executePathMoves dataDir preGraph postGraph moves`, then `DocumentPersistWrite.writeDocumentsSoft dataDir postGraph`, then `DocumentPersistPath.stampExistingDocuments`.
[ ] `DocumentPersistChange.executePathMoves` pipes `moves` through `DocumentPathMove.coalescePathMoves preGraph`, folds with `resolveMovePaths` and `executePathMove`.
[ ] `DocumentPersistChange.executePathMove` calls `Directory.Move(oldFullPath, newFullPath)` when `oldIsDirectory` and `Directory.Exists oldFullPath`, and `File.Move(oldFullPath, newFullPath)` when not a directory and `File.Exists oldFullPath`.
[ ] `DocumentPathMove.planPathMovesBetweenGraphs` constructs `{ nodeId; oldPath; newPath }` records when `expandedPathForNodeId` differs between graphs.
[ ] `DocumentPersistChange.validatePathMoves` also calls `planPathMovesBetweenGraphs` and `coalescePathMoves` before disk checks; it does not call `executePathMove`.
[ ] `src/Server/DocumentPersistChange.fs` has no reference to `NodeLockPool` today.

## Existing tests

[ ] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` writes `graphWithNestedDocs`, renames the File with `Graph.setName`, calls `persistGraphChange`, and asserts the old file path is absent and the new file path exists under `dataDir`.
[ ] ``persistGraphChange moves renamed directory tree before writing`` in the same file renames the Directory `docs` to `archive`, calls `persistGraphChange`, and asserts the old directory is absent and `.amb` plus nested `readme.txt` exist under the new directory.
[ ] ``persistGraphChange rejects destination artifact conflict`` in the same file pre-creates the destination file, expects `persistGraphChange` to return Error containing `already exists`, and leaves both source and destination files on disk.
[ ] ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs` holds `withLock` on a descendant File, keeps Directory `withLock` outside, and lets a sibling File `withLock` enter before the Directory enters after release.
[ ] ``an absent node proceeds and the same thread is rejected`` in the same file runs `absentProceeds` then `holdRejectsSibling`, which asserts absent-id `withLock` proceeds, same-thread second `withLock` returns `Error NodeLockRejection.HolderHoldsLock`, the sibling action stays uncalled, and `entryCount` stays 1 until release.

## Copy-from

[ ] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` wraps `writeDocumentCore` in `NodeLockPool.withLockResult graph documentRootId`.
[ ] `NodeLockPool.withLockResult` maps `Error NodeLockRejection.HolderHoldsLock` to `Error "holder already holds a lock"` in `src/Server/NodeLockPool.fs`.
[ ] `NodeLockPool.enqueue` returns `Error NodeLockRejection.HolderHoldsLock` when any pool entry shares `Environment.CurrentManagedThreadId` with the new request.
[ ] `NodeLockPool.withLock` on a `nodeId` absent from `graph.nodes` runs the action with no queue entry and returns `Ok` immediately.

## implement

[ ] [13 — Lock the persist path moves](plan/node-move-disk/issues/13-lock-the-persist-path-moves.md) claims `persistGraphChangeWith` holds `NodeLockPool.withLock` around each coalesced move before `File.Move` or `Directory.Move`; the code calls `executePathMoves` with no lock wrapper.
[ ] The ticket claims the lock call is `NodeLockPool.withLockResult` on the post graph and `move.nodeId`; `DocumentPersistChange.fs` imports and calls neither `withLock` nor `withLockResult`.
[ ] The ticket claims each coalesced move runs one at a time with lock release before the next move; `executePathMoves` already folds moves in order but without locks.
[ ] The ticket claims on Error the function returns that Error and runs no later move; `executePathMoves` already short-circuits the fold on Error.
[ ] The ticket claims call sites of `persistGraphChange` and `persistGraphOps` outside their definitions are only in `tests/Server.Tests`; `src/` has no callers, and `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`, `DocumentPersistChangeTests.fs`, and `DocumentOpPersistenceTests.fs` call them.
[ ] The four output tests ``persist path move waits while the moved file is held``, ``persist path move waits while a file under the moved directory is held``, ``a path move on a thread that already holds a lock leaves the artifact``, and ``a sibling file lock lets a file path move finish`` are absent from `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`.
[ ] When a path move would take `withLockResult`, the persist path does not already hold a pool lock: `persistGraphChangeWith` runs `executePathMoves` before `writeDocumentsSoft`, and only `writeDocument` inside the write phase calls `withLockResult`.
[ ] ``persist path move waits while the moved file is held`` will call `NodeLockPool.withLock` on the moved File inside its action, call `DocumentPersistChange.persistGraphChange` from outside that hold for a File rename on `graphWithNestedDocs`, assert the old artifact path remains during the hold, then assert the new path exists after release.
[ ] ``persist path move waits while a file under the moved directory is held`` will hold `withLock` on the nested File under `docs`, call `persistGraphChange` for a Directory rename on `graphWithNestedDocs`, assert the old directory path remains during the hold, then assert the directory new path exists after release.
[ ] ``a path move on a thread that already holds a lock leaves the artifact`` will nest `withLock` on a sibling File in its action on the calling thread, call `persistGraphChange` for another File rename, expect `Error "holder already holds a lock"`, and assert the source artifact path is unchanged with pool `entryCount` 1 on the first hold.
[ ] ``a sibling file lock lets a file path move finish`` will hold `withLock` on a sibling File on another thread, call `persistGraphChange` for the other File rename during that hold, and assert the renamed artifact exists at the new path before the sibling lock releases.
[ ] `graphWithNestedDocs` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` is a `private` function returning `Graph * NodeId * NodeId * NodeId * NodeId` for Workspace `home`, Directory `docs`, File `readme.txt`, and a body Normal node.
[ ] `tests/Server.Tests/DocumentPersistenceFixtures.fs` exposes `graphWithNestedDocs` with the same nested shape for other Server test modules.
