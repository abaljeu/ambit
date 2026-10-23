# 53 — Arrival and reconcile emit NodeReceived

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `Notice.NodeReceived` handler in `CoreMailboxLoad.applyNotice`. The handler applies `Notice.NodeReceived` and logs it. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` directly for a changed File Node already on the server (pull request 263). Until the upload Actor exists, `Api.postEvents` posts `Notice.NodeReceived` for an incoming file. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) is that Actor. The Actor posts `Notice.NodeReceived` once it exists. A second Load repairs a file that never arrived.

## What to build

Until the upload Actor exists, `Api.postEvents` posts `Notice.NodeReceived` for an incoming file. The upload Actor posts `Notice.NodeReceived` once it exists. That Actor is [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md).

### 1. Arrival

1. [ ] **Arrival post** — `Api.postEvents` posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice` for an incoming file.

### Acceptance Criteria

1. [ ] **Reconcile test** — `reconcile emits NodeReceived`. The test calls `LazyLoadReconciliationServer.reconcileReceivedFiles`. It posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice` for a changed File Node. The `Notice.NodeReceived` handler sets Unparsed and clears PullingFile.
2. [ ] **Arrival test** — `incoming file emits NodeReceived`. The test calls `Api.postEvents` for the incoming-file Change. `Api.postEvents` posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice`. The `Notice.NodeReceived` handler sets Unparsed and clears PullingFile.

## See also

- [Graph change arrival](../arch.md#graph-change-arrival)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [GLOSSARY](../../../GLOSSARY.md)
