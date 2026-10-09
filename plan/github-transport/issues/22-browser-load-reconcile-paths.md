# 22 — Browser Load paths that reconcile or parse

**Type:** grilling
**Status:** needs-info
**Blocked by:** None — research is in this file; Alan's answers are the open part
**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md)

## Context

A person runs Load in the Browser. Several client paths then ask the Server to reconcile a directory or parse a file on the request. git Load does this after the Actor returns. Desk Load on the web does this instead of a Desktop upload. The life Workspace incident ran two whole-workspace reconciles at once. [21 — Git Load informs Core](21-git-load-informs-core.md) stops the Actor's inline walk and leaves every Browser path in place.

Alan, 2026-10-08: the only reconcile is the Parse thread. The Server Actor posts work to that thread. The Browser was never instructed to reconcile. The Desktop app did, and that was disabled in favor of webdav-only uploading.

Binding clauses: [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile** and §3 step 2 Migrate item 1 **Workspace lock handoff**. [github-transport architecture](../arch.md) story path 17 **Inform Core after files land**. [parse-thread architecture](../../parse-thread/arch.md) §2 Module map item 1 **Directory reconcile**.

Home is this Project. The paths below are the Load after-steps this Project and the older Upload work left in the Browser. [parse-thread](../../parse-thread/project.md) owns the reconcile operation. [core-refinement](../../core-refinement/project.md) owns mailbox Load of a File Node. This ticket asks which Browser callers stay, which needs move onto the Parse thread, and which go.

Classifications in the research are proposals. They are not decisions.

1. **stays** — The path does not run Directory reconcile or file parse, and it remains.
2. **remains** — The person still needs the effect. The work moves to the Parse thread. The inline HTTP walk goes.
3. **goes** — The path is dropped. The Parse thread is not given a replacement for it.

## Research

Read from this tree. Line numbers are the current files.

| Path | Location | Proposed classification |
| --- | --- | --- |
| `gitLoadAfterOp` | [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 56 | goes |
| `startDirectoryReconcile` | [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 31 | goes |
| `ContinueDirectoryReconcile` | [ViewModelSync.fs](../../../src/Shared/ViewModelSync.fs) line 109; handler [App.fs](../../../src/Client/App.fs) line 166 | goes |
| `ReconcileServerDisk` | [WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 13 | remains |
| `ParseServerDisk` | [WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 15 | remains |
| `parseFocusedFile` | [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 46 | goes |
| `queueLoadRequest` | [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 14 | stays |
| `reconcileWorkspaceAck` | [UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 53 | stays |
| `DesktopPush` | [WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 11 | goes |
| `startWorkspacePush` | [UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 177 | goes |
| Added-paths route | [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs) line 383 | goes |

### 1. gitLoadAfterOp

[UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 56. After git Load, a focused File calls `parseFocusedFile`. Any other Focus calls `startDirectoryReconcile` for the Workspace root.

Caller: [LoadSaveCommandClient.fs](../../../src/Client/LoadSaveCommandClient.fs) `continueGitLoad` (line 76) from `applyResponse` when the path is Git and the operation is Load (line 38).

Server: no route of its own. It emits `ContinueDirectoryReconcile` or `ContinueParseFile`.

History: commit `2a23835a`, 2026-09-27, "fixing github to work" (Alan). That is the web git Load era, the same day as [13 — Run git Load and Save through the Server Actor](13-actor-runs-git-load-save.md). It is not the July 2026 Desktop upload era.

Proposed **goes**. The Actor informs Core. The Parse thread reconciles. The Browser was not instructed to start a second walk. Fetch+Poll stays so the outline updates. This call is one of the two walks in the life Workspace incident. Ticket 21 leaves it in place until this decision.

### 2. startDirectoryReconcile

[UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 31. It sets Parsing and emits `Effect.ContinueDirectoryReconcile`.

Callers: `gitLoadAfterOp` (line 67) and `runDeskLoadAction` for `ReconcileServerDisk` (line 92).

Server: none until the effect handler posts.

History: the function landed in `2a23835a` (2026-09-27). The effect it emits is older (item 3).

Proposed **goes**. It only starts the inline HTTP reconcile. A caller that remains informs Core by Unparsed plus a parse push, and does not call this function.

### 3. ContinueDirectoryReconcile

Declared at [ViewModelSync.fs](../../../src/Shared/ViewModelSync.fs) line 109. Handled at [App.fs](../../../src/Client/App.fs) line 166. The handler posts `POST /ambit/workspace/reconciliation/directory`.

Emitters: `startDirectoryReconcile` (item 2) and `completeWorkspacePush` when no single file is selected ([UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 265).

Server: [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs) `registerDirectoryRoute` (line 345). An empty path calls `reconcileWorkspace` (line 282). A non-empty path calls `reconcileDirectory`. Both run inline on the HTTP request.

History: the handler is commit `d118060e`, 2026-08-08, "fix slow Load responsiveness" (Alan). The route string first appears in `fcee49e7`, 2026-07-22, "refactoring, eliminating git mentions", during Upload. Ticket 14 reused it: `3cde2784`, 2026-09-27, "Ticket 14: route Load-Save by pre-pick (#141)".

Proposed **goes**. This is the inline whole-workspace reconcile. Binding arch gives that walk to the Parse thread.

### 4. ReconcileServerDisk

[WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 13. `WorkspaceUpload.plan` returns it (line 100) for a Directory or Workspace focus when Desktop push is not available.

Caller: `deskLoadOp` → `runDeskLoadAction` (line 88). `deskLoadOp` runs from `continueDesk` on Desk Load ([LoadSaveCommandClient.fs](../../../src/Client/LoadSaveCommandClient.fs) line 72) and from `RunQueuedRequest QueuedLoad` ([App.fs](../../../src/Client/App.fs) line 101).

Server: `POST /ambit/workspace/reconciliation/directory`, the same route as item 3.

History: commit `50d37983`, 2026-07-22, "upload clarification and sync status" (Alan). Upload era, before github-transport. The Desktop gate around it is `781735e5`, 2026-07-25.

Proposed **remains**. A person who Loads a Directory or a Workspace on the web still needs disk brought into the graph. [core-refinement architecture](../../core-refinement/arch.md) §5 item 8 marks that Directory Node Unparsed. The Parse thread reconciles. The HTTP walk goes.

### 5. ParseServerDisk

[WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 15. `plan` returns it (line 94) for a File focus when Desktop push is not available.

Callers: `runDeskLoadAction` (line 93) via `parseFileOp`, and `parseFocusedFile` (item 6) which passes this action.

Server: `Effect.ContinueParseFile` → [App.fs](../../../src/Client/App.fs) `runParseFile` (line 214) → `POST /ambit/file/parse` → [Api.fs](../../../src/Server/Api.fs) `postParseFile` (line 397) → `planParseFile` inline on the request. [Route registration](../../../src/Server/RouteRegistration.fs) maps that route (line 417). This is not the parse stack. `CoreMailbox.load` pushes a File Node and has no production caller outside tests.

History: same Upload commit `50d37983`, 2026-07-22.

Proposed **remains**. Web Load of a File still needs that file parsed. The existing door is mailbox Load, which pushes the File Node. The inline `planParseFile` on the request goes.

### 6. parseFocusedFile

[UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 46. It calls `parseFileOp` with `ParseServerDisk`.

Caller: `gitLoadAfterOp` only (line 58), when the contextual target is a File.

Server: `POST /ambit/file/parse`, as item 5.

History: introduced in `2a23835a`, 2026-09-27, with `gitLoadAfterOp`. Touched again in `1e9a2121`, 2026-09-29, "Ticket 20 — State axes on special nodes".

Proposed **goes**. It exists so the Browser parses a focused File after git Load. The Actor already has its own inline `parseFocusFile`. The Parse thread is the parse. The Browser was not instructed to start one.

### 7. queueLoadRequest

[UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 14. It queues `QueuedLoad` until sync is idle.

Callers: `runDeskLoadAction` when creating a Workspace from a folder cannot start (line 87), and when `ReconcileServerDisk` or `ParseServerDisk` cannot start (line 98). git Load does not use it. `gitLoadAfterOp` reports the blocked detail instead (line 68).

Later: [App.fs](../../../src/Client/App.fs) line 101 runs `deskLoadOp` for `QueuedLoad`.

Server: none until the replayed desk Load.

History: commit `d1d46d87`, 2026-08-08, "refactor" (Alan).

Proposed **stays**. It parks desk Load. It does not reconcile. Whether the replayed action remains is question 2 below. That question owns the replay. This function is not a separate decision.

### 8. reconcileWorkspaceAck

[UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 53. It applies a Change ack to client sync state.

Callers: `applyAndPostSync` (line 103) and `completeUploadStructurePost` (line 330).

Server: `POST /{file}/changes`. It does not call the reconciliation routes.

History: commit `a1711eed`, 2026-08-16, "undo change mass implementation" (Alan). The name says reconcile. The work is Sync.

Proposed **stays**. Glossary says Sync is not reconcile. This function confirms a posted Change.

### 9. DesktopPush

[WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) line 11. `plan` returns it (lines 92 and 98) when Desktop push caps and a local mapping both exist.

Caller: `runDeskLoadAction` (line 76) → `startWorkspacePush` or `queueWorkspacePush`.

Server: `/_desktop/workspace-push` (item 10), then either `POST /ambit/file/parse` or `POST /ambit/workspace/reconciliation/directory`.

History: `50d37983`, 2026-07-22, Upload era. Desktop, not the web git Load path.

Proposed **goes**. Alan said the Desktop app reconciled before that was disabled for webdav-only uploading. WebDAV Upload and Download stay.

### 10. startWorkspacePush

[UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) line 177. It emits `ContinueWorkspaceStubsThenPush`.

Callers: `runDeskLoadAction` (line 80) and [App.fs](../../../src/Client/App.fs) line 103 for a queued workspace push.

Server: [DeskLoadSaveEffectClient.fs](../../../src/Client/DeskLoadSaveEffectClient.fs) line 79 posts `/_desktop/workspace-push`. On success, `completeWorkspacePush` (line 232) parses one file or emits `ContinueDirectoryReconcile`. The receive side [GitGateway.fs](../../../src/Server/GitGateway.fs) `completeWorkspacePush` (line 153) calls `reconcileChangedPaths` inline after a workspace push pack (`handlePackPost`, line 373).

History: commit `92a76f80`, 2026-07-25, "fixing upload bug" (Alan). Desktop upload era.

Proposed **goes**, with `DesktopPush` and with the server reconcile after that receive. WebDAV stays.

### 11. Added-paths route

`POST /ambit/workspace/reconciliation/added` at [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs) line 383. It calls `reconcileAddedPaths`.

Client: [UpdateCodec.fs](../../../src/Client/UpdateCodec.fs) `encodeReconciliationAddedRequest` (line 66) has no caller in this tree.

History: the route string is in `2cebe763`, 2026-07-25, "fix changes hanging the system". The encoder arrived with `dc4a222d`, 2026-07-23, "CONVERT UPLOAD to build graph clientside", and `52a9301a`, 2026-07-22, "fixing incremental upload issues". Upload era.

Proposed **goes**. Nothing in the Browser posts it. Deleting the route waits until the directory route's callers are decided, so a build does not remove a door this grilling still names.

### 12. Related server walk

Not a Browser path. [GithubTransportActor.fs](../../../src/Server/GithubTransportActor.fs) production `continueLoad` (line 216) calls `reconcileWorkspace`. Commit `cdc12c42`, 2026-09-27, "Run git Load and Save through the Server Peer Actor", introduced that call. `2a23835a` the same day pointed it at `reconcileWorkspace` and added `gitLoadAfterOp`. Those two calls are the life Workspace pair. [21 — Git Load informs Core](21-git-load-informs-core.md) owns the Actor call.

## Grilling

One round. Each question is one decision. This ticket does not decide them.

❓ **Q1** - **Git Load after-step**: After git Load, `gitLoadAfterOp` parses a focused File or posts directory reconcile for the Workspace root. That post is the Browser half of the life Workspace incident. Does this after-step go?

➡️ It goes. The Actor informs Core. The Parse thread reconciles. The Browser keeps Fetch+Poll.

❓ **Q2** - **Web Directory Load**: Web Load of a Directory or a Workspace plans `ReconcileServerDisk` and posts `/ambit/workspace/reconciliation/directory`. Does that HTTP walk go, with the need remaining as Unparsed plus a parse-stack push?

➡️ The need remains. The HTTP walk goes. Web Load informs Core the same way git Load does.

❓ **Q3** - **Web File Load**: Web Load of a File plans `ParseServerDisk` and posts `/ambit/file/parse`, which runs `planParseFile` on the request. Mailbox Load already pushes a File Node. Does the inline parse go, with the need remaining on that push?

➡️ The need remains. The inline parse goes. Web File Load uses mailbox Load.

❓ **Q4** - **Desktop push**: `DesktopPush` and `startWorkspacePush` post `/_desktop/workspace-push`, then parse a file or reconcile a directory. `GitGateway.completeWorkspacePush` reconciles again after receive. Do these paths go?

➡️ They go, including the reconcile after receive. WebDAV Upload and Download stay.

❓ **Q5** - **Change ack**: `reconcileWorkspaceAck` applies the ack from `POST /{file}/changes`. It does not scan disk. Does it stay?

➡️ It stays. It is Sync.

❓ **Q6** - **Added route**: `POST /ambit/workspace/reconciliation/added` has an encoder and no caller. Does that route go?

➡️ It goes once no caller remains. This grilling does not delete it.

## See also

- [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile**
- [03 — Workspace Load after incoming files](../../parse-thread/issues/03-workspace-load-after-incoming-files.md)

## Comments

- 2026-10-09 — Research written from this tree. Status stays `needs-info` until Alan answers the grilling.
