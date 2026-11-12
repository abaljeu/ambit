# Node move and disk

Stage: chart
Summary: Moving a File Node or Directory Node in the Graph moves its linked file or directory on disk to match. A conflict causes amendment to disambiguate before the change lands.
Updated: 2026-11-10

**Part of:** [[plan/roadmap/epics/chapters/move-nodes-with-files.md]] on [[plan/roadmap/epics/robust-outliner.md]]

## Decision

A node move or a disk operation amends a conflict to disambiguate before the change lands.

A conflict is one of these:

- The destination path already exists.
- The source and the destination disagree.

The operation does not overwrite.

The operation does not merge.

## Notes

- 2026-10-29 — Opened by Alan for bugs in moving File Nodes and Directory Nodes and their linked disk elements. Not a Load or git question.
- 2026-11-08 — Alan set the conflict rule in Decision. Overlap: [78 — Persist thread moves a renamed node's file](../core-refinement/issues/78-persist-thread-moves-a-renamed-node-file.md).

## Issues

- [03 — Research: directory-level persist gate for moves](issues/03-research-directory-level-persist-gate/03-research-directory-level-persist-gate-ticket.md)
- [04 — Expand the node enclosure lock](issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md)
- [05 — Relocate in the same workspace](issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md)
- [06 — Keep a nested node whose derived path differs](issues/06-keep-a-nested-node-whose-derived-path-differs/06-keep-a-nested-node-whose-derived-path-differs-ticket.md)
- [07 — Move an owned node across workspaces](issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md)
- [08 — Amend a change conflict before the graph is stored](issues/08-amend-a-change-conflict-before-the-graph-is-stored/08-amend-a-change-conflict-before-the-graph-is-stored-ticket.md)
- [09 — Relocate under the node lock](issues/09-relocate-under-the-node-enclosure-lock/09-relocate-under-the-node-enclosure-lock-ticket.md)
- [10 — Reject an unparsed persist request](issues/10-reject-an-unparsed-persist-request/10-reject-an-unparsed-persist-request-ticket.md)
- [11 — Expand the node lock pool](issues/11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md)
- [12 — Compare locks on the live graph](issues/12-compare-locks-on-the-live-graph/12-compare-locks-on-the-live-graph-ticket.md) — 11 has passed review, so this ticket is unblocked. The pool stores no graph. Conflict checks read the live graph passed with the request. An absent node conflicts with nothing. `HolderHoldsLock` stays.
- [13 — Lock the persist path moves](issues/13-lock-the-persist-path-moves/13-lock-the-persist-path-moves-ticket.md) — `persistGraphChangeWith` holds `NodeLockPool.withLock` around each coalesced `File.Move` and `Directory.Move`. 11 — Expand the node lock pool, 12 — Compare locks on the live graph, and 31 — Migrate onto the workspace lock have passed review.
