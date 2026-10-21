# 78 — Persist thread moves a renamed node's file

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
**Trackers:** architecture
**Priority:** low

## Context

The persist thread takes a node id from the persist request stack, reads the live graph once, and calls `DocumentPersistChange.persistNode` to write that node's document at the path the live graph gives it.

A rename, or a move under another parent, changes that path. The file on disk still sits at the old path. The existing move code in [DocumentPersistChange.fs](../../../src/Server/DocumentPersistChange.fs) (`executePathMoves`) and [DocumentPathMove.fs](../../../src/Shared/DocumentPathMove.fs) moves a file or folder from an old path to a new one inside the work-tree gate.

The persist thread is the only writer of document files, so it knows the path of each document it wrote.

## What to build

1. [ ] **Written paths** — The persist thread keeps a private `writtenPaths: Map<NodeId, string>`, the path each document root has on disk. It fills the map from the loaded graph when the thread starts, using `NodeDesktopPath.expandedPathForNodeId`.
2. [ ] **Move, then write** — On take, the thread computes the node's path from the live graph. When `writtenPaths` holds a different path, it moves the file or folder from the recorded path to the new one with the existing move code, inside the same work-tree gate, then writes the document.
3. [ ] **Record** — After a successful move and write, the thread sets the node's entry to the new path. A folder move also updates the entries of every document under that folder.
4. [ ] **New node** — A node with no entry is written at its live path and then recorded.
5. [ ] **Failure** — A failed move posts `Notice.PersistFailed nodeId message`, writes nothing for that node, and keeps the recorded path.

### Acceptance Criteria

1. [ ] **Test** — `rename moves the file`. Rename a File node, push it, take it: the file is at the new path and the old path is empty.
2. [ ] **Test** — `move under another parent moves the file`. Same, for a change of parent Directory.
3. [ ] **Test** — `directory rename moves its documents`. Rename a Directory node: its folder and the files under it move, and a later edit to a child writes at the new path.
4. [ ] **Test** — `failed move posts PersistFailed`. The destination is taken: the thread posts `PersistFailed` and the file stays at the old path.
5. [ ] **Still working** — Upload, import, parse, Load, undo and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [72 — Push parsed nodes to the persist stack](72-push-parsed-nodes-to-the-persist-stack.md)
