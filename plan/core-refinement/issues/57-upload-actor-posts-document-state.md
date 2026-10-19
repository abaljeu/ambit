# 57 — Upload Actor posts document state

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events; 45 — SetDocumentState leaves CoreMsg
**Trackers:** architecture, project spec

## Context

`Api.postEvents` is the general client Change endpoint. It posts `InMsg.NodeUploading` and `InMsg.NodeReceived` by recognizing upload and arrival from the Ops in the Change. That inferred path is the design weakness on [Graph change](../arch.md#graph-change-change). Alan, 2026-10-19: upload runs as an Actor with an `InMsg` handle, so the Actor posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState`.

## What to build

Upload runs as an Actor. The Actor has an `InMsg` handle. It posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState`.

### 1. Upload Actor

1. [ ] **Actor** — Upload runs as an Actor with an `InMsg` handle.
2. [ ] **Posts** — The Actor posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState` through that handle.
3. [ ] **Client Change** — `Api.postEvents` does not infer `InMsg.NodeUploading`, `InMsg.NodeReceived`, or `InMsg.SetDocumentState` from the Ops of a client Change.

### Acceptance Criteria

1. [ ] **Actor test** — `upload Actor posts document state`. The upload Actor posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState`. `Api.postEvents` does not infer those three from the Ops of a client Change.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change upload Actor](../arch.md#graph-change-upload-actor)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [45 — SetDocumentState leaves CoreMsg](45-remove-document-state-set-ops.md)
- [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md)
- [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md)
- [GLOSSARY](../../GLOSSARY.md)
