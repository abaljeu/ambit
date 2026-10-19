# 45 — SetDocumentState leaves CoreMsg

**Status:** `defined`
**Type:** coding
**Blocked by:** 50 — Upload emits NodeUploading; 53 — Arrival and reconcile emit NodeReceived; 54 — Parse and import stop the Parsed Set; 56 — Remove ParseThread markUnparsed; 46 — Record the persist version; 47 — Undo and redo set Unpersisted; 55 — Stored replay sets Unpersisted; 48 — Browser applies Node events
**Trackers:** architecture, project spec

## Context

Planners and handlers write `SetDocumentState` as a document-state Op on `CoreMsg`. Alan, 2026-10-19: `SetDocumentState` leaves `CoreMsg`. It is added as `InMsg.SetDocumentState` and as an `EventBody` item on the event log. `InMsg.MarkUnparsed` leaves `InMsg` and `CoreMailboxLoad.applyInMsg`.

## What to build

`SetDocumentState` leaves the Op path on `CoreMsg`. Add `InMsg.SetDocumentState`. Add `EventBody.SetDocumentState`. Remove `InMsg.MarkUnparsed`. `Op.SetPersistState` and `Op.SetParseState` are not ops.

### 1. Contract

1. [ ] **InMsg case** — Add `InMsg.SetDocumentState` to `InMsg`. `CoreMailboxLoad.applyInMsg` accepts that case.
2. [ ] **EventBody item** — Add `EventBody.SetDocumentState` in `src/Shared/History.fs`. The event log accepts that item. The JSON codec in `src/Shared/Serialization.fs` encodes and decodes it.
3. [ ] **Leave CoreMsg** — `Op.SetDocumentState` leaves the `Op` union in `src/Shared/History.fs`, `Op.apply`, `Op` undo invert, and the JSON codec in `src/Shared/Serialization.fs`. Planners and handlers do not write `SetDocumentState` as a document-state Op on `CoreMsg`.
4. [ ] **Remove MarkUnparsed** — Remove `InMsg.MarkUnparsed` from `InMsg` and from `CoreMailboxLoad.applyInMsg`.
5. [ ] **No persist or parse Set op** — `Op.SetPersistState` and `Op.SetParseState` are not ops.

### Acceptance Criteria

1. [ ] **Codec test** — `InMsg.SetDocumentState` decodes. An `EventBody.SetDocumentState` item decodes. `Op` has no `SetDocumentState`. `InMsg` has no `MarkUnparsed`.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change contract](../arch.md#graph-change-contract)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [57 — Upload Actor posts document state](57-upload-actor-posts-document-state.md)
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
