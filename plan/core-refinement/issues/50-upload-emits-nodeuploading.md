# 50 — Upload emits NodeUploading

**Status:** `coded`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `Notice.NodeUploading` handler in `CoreMailboxLoad.applyNotice`. The handler applies `Notice.NodeUploading` and logs it. Until the upload Actor exists, `Api.postEvents` posts `Notice.NodeUploading` for the upload Change. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) is that Actor. The Actor posts `Notice.NodeUploading` once it exists.

## What to build

Until the upload Actor exists, `Api.postEvents` posts `Notice.NodeUploading` for the upload Change. The upload Actor posts `Notice.NodeUploading` once it exists. That Actor is [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md).

### 1. Upload

1. [x] **Upload post** — `Api.postEvents` posts `Notice.NodeUploading` through `CoreMailboxApi.postNotice` when it runs the upload Change.

### Acceptance Criteria

1. [x] **Upload test** — `upload emits NodeUploading`. The test calls `Api.postEvents` for the upload Change. `Api.postEvents` posts `Notice.NodeUploading` through `CoreMailboxApi.postNotice`. The `Notice.NodeUploading` handler sets PullingFile and Unpersisted.

## See also

- [Graph change upload](../arch.md#graph-change-upload)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [GLOSSARY](../../../GLOSSARY.md)
