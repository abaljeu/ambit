# 44 — Apply document state from Node events

**Status:** `defined`
**Type:** coding
**Blocked by:** 43 — Expand Node events beside the Set output; 36 — Add the persist column
**Trackers:** architecture, project spec

## Context

Graph change expand posts `InMsg.NodeUploading` or `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside the current state write, and logs NodeParsed beside that write. Those posts do not set state. `InMsg.NodeParsed` is the renamed `InMsg.ParseFinished` from [43 — Expand Node events beside the Set output](43-expand-file-events-beside-the-set-output.md). The same handler sets Parsed. Graph change apply adds the handlers in `CoreMailboxLoad.applyInMsg`. Each handler applies the message and logs it. Tests call the handlers. Producers stay on their current writes. [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md), [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md), and [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) switch the producers. [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md) removes `ParseThread` `markUnparsed`.

## What to build

Add handlers for `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` in `CoreMailboxLoad.applyInMsg`. Each handler applies the effect in [Document-state events](../spec.md) and logs the message. Each handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. The stored word for Parsed stays `current`. The column stores Parsed or Unparsed only. PullingFile is not written there. The NodeParsing handler does not push.

### 1. Apply

1. [ ] **Core-loop upload handler** — Add an `InMsg.NodeUploading` handler in `CoreMailboxLoad.applyInMsg`. It receives `InMsg.NodeUploading`, applies it, and logs it. It sets PullingFile in memory and persist state Unpersisted. It does not insert the node. It does not write PullingFile to `document_state`. It writes `persist_state` through `DatabaseProjection`.
2. [ ] **Core-loop receive handler** — Add an `InMsg.NodeReceived` handler in `CoreMailboxLoad.applyInMsg`. It receives `InMsg.NodeReceived`, applies it, and logs it. It calls `GraphMutate.setParseState` with Unparsed and `GraphMutate.setPersistState` with Unpersisted, and writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. `Node.withParseState` clears PullingFile. The code field is `noServerFile` [o] obsolescent until [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md).
3. [ ] **Core-loop parsing handler** — Add an `InMsg.NodeParsing` handler in `CoreMailboxLoad.applyInMsg`. It receives `InMsg.NodeParsing`, applies it, and logs it. NodeParsing means parse started. The handler does not set Unparsed and does not push. It leaves parse state and persist state unchanged.
4. [ ] **Core-loop persisting handler** — Add an `InMsg.NodePersisting` handler in `CoreMailboxLoad.applyInMsg`. It receives `InMsg.NodePersisting`, applies it, and logs it. It calls `GraphMutate.setPersistState` with Unpersisted, leaves parse state unchanged, and writes the resulting `document_state` and `persist_state` through `DatabaseProjection`.
5. [ ] **Core-loop persisted handler** — Add an `InMsg.NodePersisted` handler in `CoreMailboxLoad.applyInMsg`. It receives `InMsg.NodePersisted`, applies it, and logs it. It calls `GraphMutate.setPersistState` with the Persisted version, and writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. [46 — Record the persist version](46-record-the-persist-version.md) adds the version check.

### Acceptance Criteria

1. [ ] **Upload test** — `upload NodeUploading creates a PullingFile node`. The handler receives `InMsg.NodeUploading`, applies it, and logs it. The node is PullingFile and Unpersisted. `document_state` does not store PullingFile.
2. [ ] **Receive test** — `NodeReceived applies Unparsed`. The handler receives `InMsg.NodeReceived`, applies it, and logs it. The File Node is Unparsed and Unpersisted. PullingFile is clear.
3. [ ] **Parse test** — `NodeParsing does not push`. The handler receives `InMsg.NodeParsing`. Parse state is unchanged. The node is not pushed. Persist state is unchanged.
4. [ ] **Persist pull test** — `NodePersisting applies Unpersisted`. The handler receives NodePersisting. The node is Unpersisted. Parse state is unchanged.
5. [ ] **Persisted test** — `NodePersisted writes the Persisted version`. The handler receives NodePersisted. `persist_state` is that version. The stored word for Parsed in `document_state` stays `current`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change apply](../arch.md#graph-change-apply)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [36 — Add the persist column](36-add-the-persist-column.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md)
- [GLOSSARY](../../GLOSSARY.md)
