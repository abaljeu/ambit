# 50 — Upload emits NodeUploading

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `InMsg.NodeUploading` handler in `CoreMailboxLoad.applyInMsg`. The handler applies `InMsg.NodeUploading` and logs it. Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeUploading` for the upload Change. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) is that Actor. The Actor posts `InMsg.NodeUploading` once it exists.

## What to build

Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeUploading` for the upload Change. The upload Actor posts `InMsg.NodeUploading` once it exists. That Actor is [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md).

### 1. Upload

1. [ ] **Upload post** — `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` when it runs the upload Change.

### Acceptance Criteria

1. [ ] **Upload test** — `upload emits NodeUploading`. The test calls `Api.postEvents` for the upload Change. `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg`. The `InMsg.NodeUploading` handler sets PullingFile and Unpersisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change upload](../arch.md#graph-change-upload)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [GLOSSARY](../../GLOSSARY.md)
