# 53 — Arrival and reconcile emit NodeReceived

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `InMsg.NodeReceived` handler in `CoreMailboxLoad.applyInMsg`. The handler applies `InMsg.NodeReceived` and logs it. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` directly for a changed File Node already on the server (pull request 263). Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeReceived` for an incoming file. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) is that Actor. The Actor posts `InMsg.NodeReceived` once it exists. A second Load repairs a file that never arrived.

## What to build

Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeReceived` for an incoming file. The upload Actor posts `InMsg.NodeReceived` once it exists. That Actor is [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md).

### 1. Arrival

1. [ ] **Arrival post** — `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for an incoming file.

### Acceptance Criteria

1. [ ] **Reconcile test** — `reconcile emits NodeReceived`. The test calls `LazyLoadReconciliationServer.reconcileReceivedFiles`. It posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for a changed File Node. The `InMsg.NodeReceived` handler sets Unparsed and clears PullingFile.
2. [ ] **Arrival test** — `incoming file emits NodeReceived`. The test calls `Api.postEvents` for the incoming-file Change. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg`. The `InMsg.NodeReceived` handler sets Unparsed and clears PullingFile.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change arrival](../arch.md#graph-change-arrival)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [GLOSSARY](../../GLOSSARY.md)
