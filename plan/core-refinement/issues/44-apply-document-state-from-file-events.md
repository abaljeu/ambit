# 44 — Apply document state from Node events

**Status:** `defined`
**Type:** coding
**Blocked by:** 43 — Expand Node events beside the Set output; 36 — Add the persist column
**Trackers:** architecture, project spec

## Context

Graph change expand posts `Notice.NodeUploading` or `Notice.NodeReceived` through `CoreMailboxApi.postNotice` beside the current state write. It logs `Notice.NodeParsed` beside that write. Those posts do not set state. `Notice.NodeParsed` is the renamed `Notice.ParseFinished` from 43 — Expand Node events beside the Set output. The same handler sets Parsed.

Producers stay on their current writes. These tickets switch the producers:

- 50 — Upload emits NodeUploading
- 53 — Arrival and reconcile emit NodeReceived
- 54 — Parse and import stop the Parsed Set

56 — Remove ParseThread markUnparsed removes `ParseThread` `markUnparsed`.

## What to build

1. [ ] **Core-loop upload handler** — Graph change apply adds the handlers in `CoreMailboxLoad.applyNotice`. Tests call the handlers. Each handler receives its message, applies the effect Document-state events names, and logs the message. Each handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. The `document_state` column stores Parsed or Unparsed only. The stored word for Parsed stays `current`. PullingFile is not written there. The `Notice.NodeUploading` handler sets PullingFile in memory and persist state Unpersisted. It does not insert the node.
2. [ ] **Core-loop receive handler** — The `Notice.NodeReceived` handler calls `GraphMutate.setParseState` with Unparsed and `GraphMutate.setPersistState` with Unpersisted. `Node.withParseState` clears PullingFile. The code field is `pullingFile`.

The `Notice.NodePersisted` handler already exists as the `Notice.SnapshotDone` handler, which sets Persisted. [73 — Add the NodeParsing and NodePersisting Notices](73-add-the-nodeparsing-and-nodepersisting-notices.md) adds the other two handlers.

### Acceptance Criteria

1. [ ] **Upload test** — `NodeUploading applies PullingFile`. The node is PullingFile and Unpersisted. `document_state` does not store PullingFile.
2. [ ] **Receive test** — `NodeReceived applies Unparsed`. The File Node is Unparsed and Unpersisted. PullingFile is clear.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change expand](../arch.md#graph-change-expand)
- [Graph change apply](../arch.md#graph-change-apply)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [36 — Add the persist column](36-add-the-persist-column.md)
- [43 — Expand Node events beside the Set output](43-expand-file-events-beside-the-set-output.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [73 — Add the NodeParsing and NodePersisting Notices](73-add-the-nodeparsing-and-nodepersisting-notices.md)
- [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md)
- [GLOSSARY](../../../GLOSSARY.md)
