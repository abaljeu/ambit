# 45 — Remove document-state Set ops

**Status:** `defined`
**Type:** coding
**Blocked by:** 50 — Upload emits NodeUploading; 53 — Arrival and reconcile emit NodeReceived; 54 — Parse and import stop the Parsed Set; 56 — Remove ParseThread markUnparsed; 46 — Record the persist version; 47 — Undo and redo set Unpersisted; 55 — Stored replay sets Unpersisted; 48 — Browser applies Node events
**Trackers:** architecture, project spec

## Context

Graph change upload, Graph change arrival, and Graph change parsed producers have stopped their direct state writes. Graph change remove markUnparsed has removed `ParseThread` `markUnparsed`. Graph change persist version records the version. Graph change undo and Graph change stored replay mark Unpersisted. Graph change browser apply updates the resident Graph. `Op.SetDocumentState` is still in the `Op` union. Graph change contract removes `Op.SetDocumentState` and `InMsg.MarkUnparsed`. `InMsg.NodeParsed` is the renamed `InMsg.ParseFinished` from [43 — Expand Node events beside the Set output](43-expand-file-events-beside-the-set-output.md). `Op.SetText` and the other content ops stay.

## What to build

`Op.SetDocumentState` leaves. `InMsg.MarkUnparsed` leaves. `InMsg.NodeParsed` stays. NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted remain the document-state messages. `Op.NewSpecialNode` stays.

### 1. Contract

1. [ ] **Remove SetDocumentState** — `Op.SetDocumentState` leaves the `Op` union in `src/Shared/History.fs`, `Op.apply`, `Op` undo invert, and the JSON codec in `src/Shared/Serialization.fs`.
2. [ ] **No replacement Set** — `Op.SetPersistState` and `Op.SetParseState` are not ops. No Set op writes parse state, persist state, or PullingFile.
3. [ ] **Remove MarkUnparsed** — Remove `InMsg.MarkUnparsed` from `InMsg` and from `CoreMailboxLoad.applyInMsg`. [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md) already removed `ParseThread` `markUnparsed`. `CoreMailboxLoad.markUnparsedThenPush` sets Unparsed and pushes.

### Acceptance Criteria

1. [ ] **Codec test** — `SetDocumentState does not decode`. `InMsg` has no `MarkUnparsed`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change contract](../arch.md#graph-change-contract)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
- [55 — Stored replay sets Unpersisted](55-stored-replay-sets-unpersisted.md)
- [48 — Browser applies Node events](48-browser-applies-node-events.md)
- [GLOSSARY](../../GLOSSARY.md)
