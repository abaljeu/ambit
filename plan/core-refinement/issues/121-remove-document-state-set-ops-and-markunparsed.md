# 121 — SetDocumentState and MarkUnparsed leave

**Status:** `defined`
**Type:** coding
**Blocked by:** 50 — Upload emits NodeUploading; 53 — Arrival and reconcile emit NodeReceived; 54 — Parse and import stop the Parsed Set; 56 — Remove ParseThread markUnparsed; 100 — Core loop gates Persisted on the written version; 101 — Client sync applies the persist version rule; 47 — Undo and redo set Unpersisted; 55 — Stored replay sets Unpersisted; 48 — Browser applies Node events; 57 — Upload Actor posts Node notices
**Trackers:** architecture, project spec

> Moved from 45 on 2026-10-24, unchanged except for the NodeUnpersisted items. Alan, 2026-10-24: `Notice.NodeUnpersisted` is not added. [45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed](45-remove-document-state-set-ops.md) now holds the persist-stop decision. The Unpersisted mark after an edit is the `Notice.NodePersisting` that `CoreEventDispatch.pushChangeSpecials` logs.

## Context

`Op.SetDocumentState` is an `Op` on a `CoreMsg` Change. Alan, 2026-10-19: `Notice.SetDocumentState` does not exist. Only a holder of the notice function posts a Notice. Today's notice function is `CoreMailboxApi.postNotice`. `Api.inferredNodeEvents` still matches `Op.SetDocumentState`. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) removes that function.

## What to build

`Op.SetDocumentState` leaves `CoreMsg` and the Op list. No `Notice.SetDocumentState` case is added. No `EventBody.SetDocumentState` case is added. Parsed comes from `NodeParsed`. `Notice.MarkUnparsed` leaves `Notice` and `CoreMailboxLoad.applyNotice`.

### 1. Contract

1. [ ] **Leave CoreMsg and the Op list** — Remove `Op.SetDocumentState` from the `Op` union in `src/Shared/History.fs`, from `Op.apply`, from Op undo invert, and from the JSON codec in `src/Shared/Serialization.fs`. Planners and handlers do not put `Op.SetDocumentState` on a `CoreMsg`.
2. [ ] **Remove MarkUnparsed** — Remove `Notice.MarkUnparsed` from `Notice` and from `CoreMailboxLoad.applyNotice`.

### Acceptance Criteria

1. [ ] **Op test** — `SetDocumentState leaves the Op list`. `Op` has no `SetDocumentState`. The JSON codec in `src/Shared/Serialization.fs` does not encode or decode `SetDocumentState`. A `CoreMsg` Change does not carry that op.
2. [ ] **No SetDocumentState case** — `no SetDocumentState message case`. `Notice` has no `SetDocumentState`. `EventBody` has no `SetDocumentState`.
3. [ ] **Parsed test** — `parsed comes from NodeParsed`. `CoreMailboxLoad.applyNotice` sets Parsed from `Notice.NodeParsed`.
4. [ ] **MarkUnparsed test** — `MarkUnparsed leaves Notice`. `Notice` has no `MarkUnparsed`. `CoreMailboxLoad.applyNotice` has no `MarkUnparsed` case.

## See also

- [Graph change contract](../arch.md#graph-change-contract)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [56 — Remove ParseThread markUnparsed](56-remove-parsethread-markunparsed.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [47 — Undo and redo set Unpersisted](47-undo-and-redo-set-unpersisted.md)
- [55 — Stored replay sets Unpersisted](55-stored-replay-sets-unpersisted.md)
- [48 — Browser applies Node events](48-browser-applies-node-events.md)
- [GLOSSARY](../../../GLOSSARY.md)
