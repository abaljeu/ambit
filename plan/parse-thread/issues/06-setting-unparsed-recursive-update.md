# 06 — Setting Unparsed, recursive update

**Type:** coding
**Status:** coded
Actual: 35m
**Blocked by:** None — can start immediately

## Context

A person has a Directory Node, and disk is newer than a File Node in that Body. Directory reconcile runs on the parse thread and can already append a missing File Node. Setting that newer File Node Unparsed is a recursive update. This ticket does not block [05 — Directory reconcile](05-directory-reconcile.md), and 05 — Directory reconcile does not block this ticket.

## What to build

When disk is newer, the parse thread sets that File Node Unparsed. The call is an InMsg through the mailbox private function. The core loop applies that InMsg. The parse thread does not edit the graph axes. Op.SetDocumentState is not the writer. This ticket adds no Op.

The queue and the private function stay [core-refinement architecture](plan/core-refinement/arch.md) §10 Core loop.

### 1. InMsg for Unparsed

The parse thread adds an InMsg through the mailbox private function. The core loop applies that InMsg and sets the File Node Unparsed.

1. [x] markUnparsedOps — The call that set Unparsed is an InMsg through the mailbox private function. It is not Op.SetDocumentState.
2. [x] planFromFiles — The call that appended the Unparsed ops is that same InMsg. Reconcile ops do not carry the Unparsed write.

### 2. Disk-newer and push

These leaves moved from [05 — Directory reconcile](05-directory-reconcile.md).

1. [x] Disk-newer — After Directory reconcile, a disk-newer file has its File Node Unparsed.
2. [x] Unparsed — The parse thread sets that disk-newer File Node Unparsed by an InMsg. The core loop applies the InMsg.
3. [x] Push — Directory reconcile names that File Node for push.

## See also

[Parse thread architecture](plan/parse-thread/arch.md)

[core-refinement architecture](plan/core-refinement/arch.md)

## Time

- 2026-10-07 20m — disk-newer File Node set Unparsed through InMsg (from chat)
- 2026-10-07 15m — document InMsg MarkUnparsed on the architecture and in current docs (from chat)

## Comments

- 2026-10-07 — Alan. MarkUnparsed is an accepted InMsg case. Document it on [core-refinement architecture](../../core-refinement/arch.md) §10 Core loop and under [doc/current](../../../doc/current/).
