# Node move and disk

Stage: chart
Summary: Moving a File Node or Directory Node in the Graph moves its linked file or directory on disk to match. A conflict causes amendment to disambiguate before the change lands.
Updated: 2026-11-09

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

- [01 — Moving never-parsed File Nodes under a new Directory Node leaves the files behind](issues/01-move-unparsed-files-under-new-directory.md)
- [03 — Research: directory-level persist gate for moves](issues/03-research-directory-level-persist-gate.md)
