# 07 — Directory Unparsed during reconcile

**Type:** coding
**Status:** coded
Actual: 1h
**Blocked by:** [05 — Directory reconcile](05-directory-reconcile.md), [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md)

## Context

A person sets a Workspace Node or a Directory Node Unparsed. Directory reconcile then runs on that node. Reconcile notes File Nodes in that directory that need reparsing. Those File Nodes stay on [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md). This ticket marks Directory Nodes there that need reparse. [05 — Directory reconcile](05-directory-reconcile.md) owns the scan this ticket extends.

## What to build

When Directory reconcile finds a directory under the reconciled Directory Node or Workspace Node that needs reparse, it marks that Directory Node Unparsed. Needs reparse means the directory is disk-newer than its Directory Node, or the directory is a disk member the Graph lacks. The disk-newer compare is the same compare already used for a File Node. A missing disk directory gets a Directory Node, and that new node is Unparsed. A new special node already starts Unparsed. This ticket does not add a third compare. It does not mark every child Directory Node. It does not mark ancestor nodes.

The mark is an InMsg through the mailbox private function. The core loop applies that InMsg. The parse thread does not edit the graph axes. Op.SetDocumentState is not the writer. Reconcile ops do not carry the Unparsed write. This ticket adds no Op. The InMsg is the one [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md) adds for a File Node. This ticket does not add a second message type.

The queue and the private function stay [core-refinement architecture](plan/core-refinement/arch.md) §10 Core loop. Directory reconcile names that Directory Node for push when the Parse stack exists.

### 1. Directory Node Unparsed

Directory reconcile names each child Directory Node that needs reparse. The parse thread sets that Directory Node Unparsed by the same InMsg as a File Node.

1. [x] Disk-newer Directory Node — After Directory reconcile, a disk-newer directory under the reconciled Directory Node or Workspace Node has its Directory Node Unparsed.
2. [x] Missing directory — A directory that is a disk member the Graph lacks has a Directory Node, and that Directory Node is Unparsed.
3. [x] Same InMsg — The Unparsed write is the InMsg from [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md). It is not Op.SetDocumentState. Reconcile ops do not carry the Unparsed write.
4. [x] No axis edit — The parse thread does not edit the graph axes. The core loop applies the InMsg.
5. [x] File path stays — A disk-newer File Node stays on [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md). This ticket does not move that acceptance.

### 2. Push

Directory reconcile names that Directory Node for push when the Parse stack exists. The push rule matches the File Node push on [06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md).

1. [x] Push — Directory reconcile names that Directory Node for push.

## See also

[Parse thread map](plan/parse-thread/map.md)

[Parse thread architecture](plan/parse-thread/arch.md)

[06 — Setting Unparsed, recursive update](06-setting-unparsed-recursive-update.md)

## Time

- 2026-10-07 1h — Directory Node Unparsed and push during Directory reconcile (from chat)
