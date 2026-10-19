# 45 — SetDocumentState leaves; NodeUnpersisted replaces it

**Status:** `defined`
**Type:** coding
**Blocked by:** 50 — Upload emits NodeUploading; 53 — Arrival and reconcile emit NodeReceived; 54 — Parse and import stop the Parsed Set; 56 — Remove ParseThread markUnparsed; 46 — Record the persist version; 47 — Undo and redo set Unpersisted; 55 — Stored replay sets Unpersisted; 48 — Browser applies Node events; 57 — Upload Actor posts Node notices
**Trackers:** architecture, project spec

## Context

`Op.SetDocumentState` is an `Op` on a `CoreMsg` Change. Alan, 2026-10-19: `Notice.SetDocumentState` does not exist. `Notice.NodeUnpersisted of nodeId` replaces it. Only a holder of the notice function posts a Notice. Today's notice function is `CoreMailboxApi.addInMsg`. `Api.inferredNodeEvents` still matches `Op.SetDocumentState`. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) removes that function.

## What to build

`Op.SetDocumentState` leaves `CoreMsg` and the Op list. No `InMsg.SetDocumentState` case is added. No `EventBody.SetDocumentState` case is added. The Unpersisted mark after an ordinary edit is `InMsg.NodeUnpersisted of nodeId`. That case becomes `Notice.NodeUnpersisted`. The server's Change handler posts it through the notice function. Parsed comes from `NodeParsed`. `InMsg.MarkUnparsed` leaves `InMsg` and `CoreMailboxLoad.applyInMsg`. It does not become a `Notice`.

### 1. Contract

1. [ ] **Leave CoreMsg and the Op list** — Remove `Op.SetDocumentState` from the `Op` union in `src/Shared/History.fs`, from `Op.apply`, from Op undo invert, and from the JSON codec in `src/Shared/Serialization.fs`. Planners and handlers do not put `Op.SetDocumentState` on a `CoreMsg`.
2. [ ] **NodeUnpersisted** — Add `InMsg.NodeUnpersisted of nodeId` in `src/Server/Core/CoreMsg.fs`. `CoreMailboxLoad.applyInMsg` accepts that case and marks that node Unpersisted. At the Expand rename the case is `Notice.NodeUnpersisted`.
3. [ ] **Change handler** — After an ordinary edit, `CoreEventDispatch.postEvent` posts `InMsg.NodeUnpersisted of nodeId` through `CoreMailboxApi.addInMsg`.
4. [ ] **Remove MarkUnparsed** — Remove `InMsg.MarkUnparsed` from `InMsg` and from `CoreMailboxLoad.applyInMsg`.

### Acceptance Criteria

1. [ ] **Op test** — `SetDocumentState leaves the Op list`. `Op` has no `SetDocumentState`. The JSON codec in `src/Shared/Serialization.fs` does not encode or decode `SetDocumentState`. A `CoreMsg` Change does not carry that op.
2. [ ] **No SetDocumentState case** — `no SetDocumentState message case`. `InMsg` has no `SetDocumentState`. `EventBody` has no `SetDocumentState`.
3. [ ] **Ordinary edit test** — `ordinary edit posts NodeUnpersisted`. `CoreEventDispatch.postEvent` applies an ordinary edit and posts `InMsg.NodeUnpersisted` of the edited node id through `CoreMailboxApi.addInMsg`. `CoreMailboxLoad.applyInMsg` marks that node Unpersisted.
4. [ ] **Parsed test** — `parsed comes from NodeParsed`. `CoreMailboxLoad.applyInMsg` sets Parsed from `InMsg.NodeParsed`.
5. [ ] **MarkUnparsed test** — `MarkUnparsed leaves InMsg`. `InMsg` has no `MarkUnparsed`. `CoreMailboxLoad.applyInMsg` has no `MarkUnparsed` case.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

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
- [GLOSSARY](../../GLOSSARY.md)
