# 23 — Directory or File Load posts to the parse stack

**Status:** `coded`
**Actual:** 6h
**Type:** coding
**Blocked by:** [28 — Stop enqueueing nodes just marked Unparsed](28-stop-enqueueing-nodes-just-marked-unparsed.md)
**Trial step:** 2. Next: [21 — Git Load posts the Workspace](21-git-load-posts-workspace.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 2. Load on a Directory or a File posts that node to the parse stack and does nothing else. Directory Load and File Load still finish. The reconcile and parse functions stay. The caller changes.

Alan, 2026-10-08. Load queues its subject. A Directory queues that Directory. A File queues that File. Nodes inside one File share that File. The Browser does not run a client reconcile on Load.

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

1. [x] Directory caller — A Directory Load sends a core message whose subject is that Directory Node. The server Desk path pushes that Directory through mailbox Load. The browser does not call `startDirectoryReconcile`.
2. [x] File caller — A File Load sends a core message whose subject is that File Node. The server Desk path pushes that File. The browser does not call `parseFileOp`.
3. [x] Subject rule — Load's subject is a Workspace Node, a Directory Node, or a File Node. A selected node inside a File Node uses that owning File Node. Several selected nodes in one File Node yield one message. A selected child does not become its parent Workspace Node.
4. [x] Functions stay — `reconcileDirectory`, `reconcileWorkspace`, and `planParseFile` stay in their modules. This ticket does not delete them.
5. [x] No new route — The message uses mailbox Load on the existing `POST /ambit/load-save-command` wire. The desktop-mapped path is left to [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md). This ticket does not fix that timing.
6. [x] Flag stays off — Do not set `requeueOnUnparsed` to true.
7. [x] Test, directory — A Directory Load names that Directory id and does not post `POST /ambit/workspace/reconciliation/directory`. The Desk path pushes that id. The directory still reconciles when the parse thread pops it.
8. [x] Test, file — A File Load names that File id and does not post `POST /ambit/file/parse`. The Desk path pushes that id. The file still parses when the thread pops it.
9. [x] Test, selection — Two selected File Nodes produce two messages, one per File Node. Two nodes inside one File Node produce one message for that File Node.
10. [x] Git actor stays on a Workspace — Plain Load of a File Node or a Directory Node stays Desk when that Workspace has a git remote. Explicit git Load, and plain Load of a Workspace Node, may start the git actor. A web desk Load of a Workspace Node with no remote sends a core message whose subject is that Workspace Node.
11. [x] Push front end — Before an id is pushed onto the parse stack, [CoreMailboxLoad.fs](../../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush` marks that node Unparsed. Every push uses that front end. `requeueOnUnparsed` stays false, so the mark does not enqueue.
12. [x] Whole selection — The subject list is the whole selection: each Workspace, Directory, or File, and a node inside a File uses that File. Explicit git Load and the desktop-mapped path stay one focus message.

## Comments

- 2026-10-09 — The desktop-mapped path is left to [26 — Revisit WebDAV parse](26-revisit-webdav-parse.md). This ticket does not fix its timing.
- 2026-10-09 — A Command is a user operation and is not sent. The Load command sends a core message. Load's subject is a Workspace Node, a Directory Node, or a File Node. Nodes inside one File Node share that File Node.
- 2026-10-09 — Plain Load of a File or a Directory stays on the parse stack when the Workspace has a git remote. The git actor starts for a Workspace target, or for explicit git Load.
- 2026-10-09 — Alan rejected `POST /ambit/parse-stack`. Directory and File Load use `SubmitLoadSaveCommand` on `POST /ambit/load-save-command`. The server Desk path pushes a File or Directory through mailbox Load. The browser after-step does not reconcile or parse those targets. Decision: [0005 — No new POST endpoints](../../../doc/Decisions/0005-no-new-post-endpoints.md). Load stays on this wire until [24 — Load and Save on the events list](24-load-save-on-events.md).
- 2026-10-09 — The parse push marks its subject Unparsed before the push. That mark does not enqueue. The Load command's subject list is the whole selection.

## Time

- 2026-10-09 2h — Directory and File Load post to the parse stack (from chat)
- 2026-10-09 1h — Move that post onto the Load command wire (from chat)
- 2026-10-09 1h — Plain File or Directory Load stays on the parse stack when a remote exists (from chat)
- 2026-10-09 2h — Unparsed push front end, whole-selection subjects, mailbox split (from chat)
