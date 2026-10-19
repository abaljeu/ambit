# 57 — Upload Actor posts Node notices

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events; 54 — Parse and import stop the Parsed Set
**Trackers:** architecture, project spec

## Context

`Api.postEvents` is the client Change endpoint. `Api.inferredNodeEvents` reads `Op.SetDocumentState` on that Change and posts `Notice.NodeUploading`, `Notice.NodeReceived`, and `Notice.NodeParsed` through `CoreMailboxApi.postNotice`. Alan, 2026-10-19: client upload becomes an Actor. The core gives the Actor the notice function. The Actor posts `NodeUploading` and `NodeReceived`. It does not post `SetDocumentState`. Only a holder of the notice function posts a Notice. Today's notice function is `CoreMailboxApi.postNotice`. [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) moves the import `NodeParsed` post to `ParseThread.finishParse` before `inferredNodeEvents` is removed.

## What to build

Client upload runs as an Actor. The core gives the Actor the notice function. The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. Those cases become `Notice.NodeUploading` and `Notice.NodeReceived`. The Actor does not post `SetDocumentState`. `Api.postEvents` stops inferring node events from client ops. Remove `inferredNodeEvents`.

### 1. Upload Actor

1. [ ] **Actor** — Client upload runs as an Actor. The core gives the Actor `CoreMailboxApi.postNotice`.
2. [ ] **Posts** — The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. It does not post `SetDocumentState`.
3. [ ] **Stop inference** — Remove `inferredNodeEvents`. `Api.postEvents` does not post `Notice.NodeUploading`, `Notice.NodeReceived`, or `Notice.NodeParsed` from the ops of a client Change.

### Acceptance Criteria

1. [ ] **Actor test** — `upload Actor posts Node notices`. The upload Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through `CoreMailboxApi.postNotice`. The Actor does not post `SetDocumentState`.
2. [ ] **Inference test** — `postEvents does not infer node events`. `Api` has no `inferredNodeEvents`. A client Change does not gain `NodeUploading`, `NodeReceived`, or `NodeParsed` from its ops.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change upload Actor](../arch.md#graph-change-upload-actor)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [GLOSSARY](../../../GLOSSARY.md)
