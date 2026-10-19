# 52 — Log stack pulls

**Status:** `defined`
**Type:** coding
**Blocked by:** None — parse pull can start. Persist pull is blocked by [51 — Stand the persist stack](51-stand-the-persist-stack.md)
**Trackers:** architecture, project spec

## Context

`CoreMailboxLoad` `LoadNode` sets Unparsed and pushes through `markUnparsedThenPush`. It does not post a Node message. The parse thread's stack puller posts `Notice.NodeParsing` when it pulls a node. NodeParsing means parse started. It does not set Unparsed and does not push. The persist thread posts `Notice.NodePersisting` when it pulls a node from the persist stack. That pull waits on [51 — Stand the persist stack](51-stand-the-persist-stack.md). The threads do not edit the graph. [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md) applies each message.

## What to build

The parse thread and the persist thread post the stack-pull messages as `Notice`. NodeParsing does not push. NodePersisting sets Unpersisted in the Core-loop handler, not in the thread.

### 1. Stack pull

1. [ ] **Parse pull** — Change the parse thread's stack puller to post `Notice.NodeParsing` when it pulls a node. NodeParsing means parse started. The puller does not set Unparsed, does not push, and does not edit the graph.
2. [ ] **Persist pull** — Change the persist thread's stack puller to post `Notice.NodePersisting` when it pulls a node. This item is blocked by [51 — Stand the persist stack](51-stand-the-persist-stack.md). The puller does not call `GraphMutate.setPersistState` and does not edit the graph.

### Acceptance Criteria

1. [ ] **Parse pull test** — `parse pull posts NodeParsing`. The parse thread posts `Notice.NodeParsing` when it pulls the node. The node is not pushed again. The thread does not edit the graph.
2. [ ] **Persist pull test** — `persist pull posts NodePersisting`. The persist thread posts `Notice.NodePersisting` when it pulls the node. It does not call `GraphMutate.setPersistState`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change stack pull](../arch.md#graph-change-stack-pull)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
