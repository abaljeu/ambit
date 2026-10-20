# 73 — Add the NodeParsing and NodePersisting Notices

**Status:** `coded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Split from [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md). `Notice` has no `NodeParsing` or `NodePersisting` case yet. No producer posts them in this ticket. [52 — Log stack pulls](52-log-stack-pulls.md) has the threads post them.

## What to build

1. [x] **Cases** — Add `Notice.NodeParsing of nodeId` and `Notice.NodePersisting of nodeId`.
2. [x] **NodeParsing handler** — `CoreMailboxLoad.applyNotice` logs it. Unparsed is the gate for parsing, so NodeParsing does not set it. It leaves parse state and persist state unchanged and does not push.
3. [x] **NodePersisting handler** — `CoreMailboxLoad.applyNotice` sets the node Unpersisted, as the Unpersisted `Op.SetDocumentState` does today. It leaves parse state unchanged.

### Acceptance Criteria

1. [x] **Test** — `NodePersisting matches the Unpersisted Set`. From the same graph, `NodePersisting` and the Unpersisted `Op.SetDocumentState` give the same node state.
2. [x] **Test** — `NodeParsing changes nothing`. The node's parse and persist state are unchanged, and nothing is pushed.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

`Op.SetDocumentState` does not write `PersistState`. The Unpersisted write today is `GraphMutate.setPersistState` with `PersistState.Unpersisted`. `NodePersisting` uses that write. The acceptance test compares the node to that set from the same graph. No producer posts these Notices. No event-log append was added. The queue puller still logs the Notice on `noticeLog`.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket.

## See also

- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [Message shape](../cqrs-messages.md)
