# 53 — Arrival and reconcile emit NodeReceived

**Status:** `coded`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `Notice.NodeReceived` handler in `CoreMailboxLoad.applyNotice`. The handler applies `Notice.NodeReceived` and logs it. `Notice.NodeReceived` fires once per node, when the server first holds that node's bytes. Two places post it. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts it after a git pull, only for a file the server never held. `WorkspaceUploadLock` posts it from the WebDAV PUT, under the Workspace lock, only while the File node is still PullingFile. A later pull or PUT of a file already received marks it Unparsed and posts no `Notice.NodeReceived`. `Api.postEvents` posts no notice. It decodes the client's events and places them as one `Order.Change` with origin `Edit`. [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) moves the upload post into the upload Actor once that Actor exists. A second Load repairs a file that never arrived.

## What to build

The PUT and the git pull reconcile post `Notice.NodeReceived` once, at first receipt. `Api.postEvents` posts no notice. The upload Actor takes over the upload post once it exists. That Actor is [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md).

### 1. Arrival

1. [x] **Arrival post** — `WorkspaceUploadLock` posts `Notice.NodeReceived` from the WebDAV PUT, under the Workspace lock, only while the File node is PullingFile. A later PUT posts `Notice.NodeUnparsed`. `Api.postEvents` posts no notice. (Corrected 2026-11-10: this step had said `Api.postEvents` posts the notice. The code never did.)

### Acceptance Criteria

1. [x] **Reconcile tests** — ``git pull of a new file posts NodeReceived once`` and ``git pull of an already received file posts no NodeReceived`` in `tests/Server.Tests/NodeEventExpandTests.fs`. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` only for a file the server never held. The `Notice.NodeReceived` handler sets Unparsed and Persisted and clears PullingFile.
2. [x] **Arrival tests** — ``one upload posts NodeReceived once`` and ``a second upload posts no NodeReceived and marks Unparsed`` in `tests/Server.Tests/GithubTransportLoadPullTests.fs`. ``NodeReceived applies Unparsed`` in `tests/Server.Tests/CoreLoopNoticeTests.fs`: the `Notice.NodeReceived` handler sets Unparsed and Persisted and clears PullingFile. (Corrected 2026-11-10: the earlier `incoming file emits NodeReceived` test through `Api.postEvents` did not exist.)

## See also

- [Graph change arrival](../arch.md#graph-change-arrival)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md)
- [GLOSSARY](../../../GLOSSARY.md)
