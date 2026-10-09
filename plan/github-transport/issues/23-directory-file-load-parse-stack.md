# 23 — Directory or File Load posts to the parse stack

**Status:** `defined`
**Type:** coding
**Blocked by:** Pull request [220 — Stop enqueueing nodes just marked Unparsed](https://github.com/abaljeu/ambit/pull/220) (trial step 1). That pull request has no local ticket. The re-enable ticket is on [parse-thread](../../parse-thread/project.md) and is not this sequence.
**Trial step:** 2. Next: [21 — Git Load posts the Workspace](21-git-load-posts-workspace.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 2. Load on a Directory or a File posts that node to the parse stack and does nothing else. Directory Load and File Load still finish. The reconcile and parse functions stay. The caller changes.

Alan, 2026-10-08. Load queues its target. A Directory queues that Directory. A File queues that File. A multi-selection queues one item per selected node. The Browser does not run a client reconcile on Load.

`requeueOnUnparsed` stays false. That flag stops the child requeue inside directory reconcile. It does not stop Load from posting the node the person named.

## Current state

The desk path is the caller this ticket changes.

1. **Plan** — [WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) `plan` (line 76) returns `ParseServerDisk` at line 94 for a File and `ReconcileServerDisk` at line 100 for a Directory.
2. **Desk op** — [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) `deskLoadOp` (line 105) calls that plan, then `runDeskLoadAction` (line 71).
3. **Directory caller** — `ReconcileServerDisk` at line 88 calls `startDirectoryReconcile` (line 31). That returns `Effect.ContinueDirectoryReconcile`. [App.fs](../../../src/Client/App.fs) line 166 posts `POST /ambit/workspace/reconciliation/directory` (line 174).
4. **Directory route** — [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs) `registerDirectoryRoute` (line 338) calls `reconcileWorkspace` (line 361) when the id is a Workspace, and `reconcileDirectory` (line 366) when the id is a Directory. Those calls run inside the HTTP request.
5. **File caller** — `ParseServerDisk` at line 93 calls `parseFileOp` ([UpdateImport.fs](../../../src/Client/UpdateImport.fs) line 75). [App.fs](../../../src/Client/App.fs) posts `POST /ambit/file/parse` (line 221).
6. **File route** — [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 417 maps that route to [Api.postParseFile](../../../src/Server/Api.fs) (line 397). The parse runs inside the HTTP request.

## What to build

A Directory Load or a File Load posts that node to the parse stack. It does not call the reconcile or parse functions itself. Those functions stay for the parse thread and for the other callers.

1. [ ] Directory caller — `ReconcileServerDisk` posts that Directory id to the parse stack. It does not call `startDirectoryReconcile`.
2. [ ] File caller — `ParseServerDisk` posts that File id to the parse stack. It does not call `parseFileOp`.
3. [ ] One item per node — A multi-selection posts one item for each selected Directory or File. It does not post a parent Workspace in their place.
4. [ ] Functions stay — `reconcileDirectory`, `reconcileWorkspace`, and `planParseFile` stay in their modules. This ticket does not delete them.
5. [ ] No new route — The post uses the parse thread push. Do not add a route. Desktop push still uses the directory and file routes until [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md).
6. [ ] Flag stays off — Do not set `requeueOnUnparsed` to true.
7. [ ] Test, directory — A Directory Load posts that Directory id and does not post `POST /ambit/workspace/reconciliation/directory`. The directory still reconciles when the parse thread pops that id.
8. [ ] Test, file — A File Load posts that File id and does not post `POST /ambit/file/parse`. The file still parses when the parse thread pops that id.
9. [ ] Test, selection — Two selected Files produce two posts, one per File.
