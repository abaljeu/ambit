# 142 — Reconcile holds its own Notice post

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] None.

## 3. Look at

### Owner

[ ] [LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs) owns `WorkspaceReconcile` and `reconcileReceivedFiles`

### Source

[ ] [LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs) `WorkspaceReconcile`, `postNotices`, `postContainerUnparsed`, `postCurrentUnparsed`, `postBeforeChange`, `postAfterChange`, `postReconcileReport`, `reconcileReceivedFiles`
[ ] [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) `finishLoadPull` and the `WorkspaceReconcile` it builds from the Actor's `changes`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postNotice`, read only

### Notice posts

[ ] `WorkspaceReconcile` is `{ handle: CoreChangesDoor; dataDir; workspaceLabel }`
[ ] The private post helpers take a `MailboxHost` and call `CoreMailboxApi.postNotice host` for `NodeUnparsed`, `NodeParsing`, and `NodeParsed`
[ ] `reconcileReceivedFiles` posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice workspace.handle.host`
[ ] `reconcileReceivedFiles` is the only public entry; `GithubTransportActor.finishLoadPull` is its only source caller
[ ] `GithubTransportActor` builds `WorkspaceReconcile` with `handle = changes`, the Actor's `CoreChangesDoor`

### Tests that reach it

[ ] [NodeEventExpandTests.fs](tests/Server.Tests/NodeEventExpandTests.fs) calls `GithubTransportActor.finishLoadPull`
[ ] [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs), [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs), [GithubTransportLoadPullTests.fs](tests/Server.Tests/GithubTransportLoadPullTests.fs)

## 4. Context

### Decisions

[ ] Alan, 2026-10-29: the `CoreQueue` post is private and granted to approved holders. Reconcile is an approved holder of the Notice post.
[ ] This ticket blocks [115 — The CoreQueue post is private](115-the-corequeue-post-is-private.md)

## 5. Changes

### Change

[ ] One change: reconcile posts its Notices through a Notice post it is given, not through `handle.host`

### Rules

[ ] No reconcile helper reads `host` from a `CoreChangesDoor` to post a Notice
[ ] The Notices posted and their order are unchanged

## 6. Tests

### Kept

[ ] Keep the tests in `LazyLoadReconciliationServerTests.fs`, `GithubTransportActorTests.fs`, and `GithubTransportLoadPullTests.fs`
[ ] Filter: `LazyLoadReconciliation`
[ ] Filter: `GithubTransport`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
