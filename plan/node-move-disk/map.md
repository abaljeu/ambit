# Node move and disk

## 1. Problem

- 1 — Stale disk path. A Graph change can give a File Node or a Directory Node a new derived path while its File or its Directory stays at the old path.
- 2 — File Nodes under a new Directory Node.
- 3 — Normal Node. A Normal Node can have a Directory Node as an Owned descendant through other Normal Nodes. A move of that Normal Node can change the derived paths of the File Nodes and Directory Nodes under it.
- 4 — Write at the new path.
  - Accept validates the destination with `DocumentPersistChange.validatePathMoves` in `FileAgent.validatePostChange` and `DbAgent.validatePostChange`.
  - `PersistThread.runRequest` calls `DocumentPersistChange.persistNode`.
  - `persistNode` writes the artifact at the new path and leaves the old file.
  - `DocumentPersistChange.persistGraphChange` and `DocumentPersistChange.persistGraphOps` call `executePathMoves`.
  - `executePathMoves` folds `executeCoalescedMove`. That takes `NodeLockPool.withLockResult` on the post graph and `move.nodeId`, then calls `resolveMovePaths` and `executePathMove`. `executePathMove` relocates with `File.Move` and `Directory.Move`.
  - Tests call `persistGraphChange` and `persistGraphOps`.
  - The live accept path and the persist thread call `validatePathMoves` and `persistNode`.

## 2. Solution

The Server compares each derived path before the Graph change with the derived path after the Graph change. The comparison applies to the changes in Scope.

- 1 — Different derived path. The Server relocates that File or that Directory with `git mv`.
- 2 — Same derived path. The Server leaves that File or that Directory at that path.

One Graph change can relocate a Directory and a File or Directory nested under that Directory. The comparison applies to each of them. Each File and each Directory ends at its derived path after the change.

A change conflict causes amendment to disambiguate before the change lands. The existing process is [ChangeAmendment](src/Shared/ChangeAmendment.fs). That rule is Decision in [Node move and disk](project.md).

## 3. Scope

This Project covers:

- 1 — File Node rename
- 2 — `Op.Replace` that places a File Node under a different Node
- 3 — Directory Node move
- 4 — Normal Node move that changes a derived path of an Owned File Node or Directory Node, including through other Normal Nodes
- 5 — Overlapping and nested derived paths in one Graph change

Workspace rename is disabled, so Workspace relocation is outside this Project.
