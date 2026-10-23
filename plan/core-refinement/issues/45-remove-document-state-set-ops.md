# 45 — SetDocumentState leaves; NodeUnpersisted replaces it

**Status:** `defined`
**Type:** coding
**Blocked by:** 50 — Upload emits NodeUploading; 53 — Arrival and reconcile emit NodeReceived; 54 — Parse and import stop the Parsed Set; 56 — Remove ParseThread markUnparsed; 46 — Record the persist version; 47 — Undo and redo set Unpersisted; 55 — Stored replay sets Unpersisted; 48 — Browser applies Node events; 57 — Upload Actor posts Node notices
**Trackers:** architecture, project spec

## Context

`Op.SetDocumentState` is an `Op` on a `CoreMsg` Change. Alan, 2026-10-19: `Notice.SetDocumentState` does not exist. `Notice.NodeUnpersisted of nodeId` replaces it. Only a holder of the notice function posts a Notice. Today's notice function is `CoreMailboxApi.postNotice`. `Api.inferredNodeEvents` still matches `Op.SetDocumentState`. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) removes that function.

## What to build

`Op.SetDocumentState` leaves `CoreMsg` and the Op list. No `Notice.SetDocumentState` case is added. No `EventBody.SetDocumentState` case is added. The Unpersisted mark after an ordinary edit is `Notice.NodeUnpersisted of nodeId`. The server's Change handler posts it through the notice function. Parsed comes from `NodeParsed`. `Notice.MarkUnparsed` leaves `Notice` and `CoreMailboxLoad.applyNotice`.

### 1. Contract

1. [ ] **Leave CoreMsg and the Op list** — Remove `Op.SetDocumentState` from the `Op` union in `src/Shared/History.fs`, from `Op.apply`, from Op undo invert, and from the JSON codec in `src/Shared/Serialization.fs`. Planners and handlers do not put `Op.SetDocumentState` on a `CoreMsg`.
2. [ ] **NodeUnpersisted** — Add `Notice.NodeUnpersisted of nodeId` in `src/Server/Core/CoreMsg.fs`. `CoreMailboxLoad.applyNotice` accepts that case and marks that node Unpersisted. At the Expand rename the case is `Notice.NodeUnpersisted`.
3. [ ] **Change handler** — After an ordinary edit, `CoreEventDispatch.postEvent` posts `Notice.NodeUnpersisted of nodeId` through `CoreMailboxApi.postNotice`.
4. [ ] **Remove MarkUnparsed** — Remove `Notice.MarkUnparsed` from `Notice` and from `CoreMailboxLoad.applyNotice`.

### Acceptance Criteria

1. [ ] **Op test** — `SetDocumentState leaves the Op list`. `Op` has no `SetDocumentState`. The JSON codec in `src/Shared/Serialization.fs` does not encode or decode `SetDocumentState`. A `CoreMsg` Change does not carry that op.
2. [ ] **No SetDocumentState case** — `no SetDocumentState message case`. `Notice` has no `SetDocumentState`. `EventBody` has no `SetDocumentState`.
3. [ ] **Ordinary edit test** — `ordinary edit posts NodeUnpersisted`. `CoreEventDispatch.postEvent` applies an ordinary edit and posts `Notice.NodeUnpersisted` of the edited node id through `CoreMailboxApi.postNotice`. `CoreMailboxLoad.applyNotice` marks that node Unpersisted.
4. [ ] **Parsed test** — `parsed comes from NodeParsed`. `CoreMailboxLoad.applyNotice` sets Parsed from `Notice.NodeParsed`.
5. [ ] **MarkUnparsed test** — `MarkUnparsed leaves Notice`. `Notice` has no `MarkUnparsed`. `CoreMailboxLoad.applyNotice` has no `MarkUnparsed` case.

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
