# 48 — Browser applies Node events

**Status:** `defined`
**Type:** coding
**Blocked by:** 43 — Expand Node events beside the Set output
**Trackers:** architecture, project spec

## Context

Graph change expand posts `InMsg.NodeUploading` and `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg`, and emits NodeParsing, NodeParsed, NodePersisting, and NodePersisted. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeUploading` and `InMsg.NodeReceived` and logs them. The Browser applies the messages the handler logged. The server writer is today's `Op.apply` of `Op.SetDocumentState` and `GraphMutate.setPersistState`, or the Core-loop handlers from Graph change apply.

## What to build

The Browser event applier updates the resident Browser Graph from the messages `CoreMailboxLoad.applyInMsg` logged. The browser updates only that graph. Display reads from it.

### 1. Browser apply

1. [ ] **Browser event applier** — Extend `SyncLogic.foldProjectedEvents` to receive every log message, including `InMsg.NodeUploading` and `InMsg.NodeReceived` that `CoreMailboxLoad.applyInMsg` logged, and apply the effect in [Document-state events](../spec.md) for each Node message to the resident Browser Graph. There is no filter. Display reads that graph. NodeParsing means parse started and does not set Unparsed. NodePersisting sets Unpersisted. NodeUploading sets PullingFile in memory.

### Acceptance Criteria

1. [ ] **Client test** — `client applies Node events for parse state and persist state`. The resident Browser Graph has the parse state and persist state from those messages, including `InMsg.NodeUploading` and `InMsg.NodeReceived`. Display reads that graph. The client receives every log message.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change browser apply](../arch.md#graph-change-browser)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [43 — Expand Node events beside the Set output](43-expand-file-events-beside-the-set-output.md)
- [GLOSSARY](../../GLOSSARY.md)
