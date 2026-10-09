# 21 — Git Load informs Core

**Type:** coding
**Status:** defined
**Blocked by:** None — can start immediately
**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md)

## Context

A person runs git Load on a Workspace. The Server Actor pulls the tracked branch. Today that Actor then reconciles the whole Workspace on the request. The Browser reconciles the same Workspace again after the response. On the life Workspace those two walks used 3.61 CPU-minutes. Azure Free's quota is 3 minutes. The app stopped. The proxy returned 502. The app returned 403 "web app is stopped". Cancel never cleared.

The server walk is [Github transport actor](../../../src/Server/GithubTransportActor.fs) `runLoad` (`continueLoad` at the production dependencies, about line 216). That calls [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) `reconcileWorkspace` inline in the Actor. The Browser walk is [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md). This ticket leaves that Browser after-step in place.

Alan, 2026-10-08: the only reconcile is the Parse thread. The Server Actor posts work to that thread. The Browser was never instructed to reconcile. The Desktop app did reconcile, and that path was disabled in favor of webdav-only uploading.

Binding clauses: [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile** (the Parse thread owns it; a Workspace Node uses the same reconcile) and §3 step 2 Migrate item 1 **Workspace lock handoff**. [github-transport architecture](../arch.md) story path 17 **Inform Core after files land**: after files land, inform Core, and Core works through the changes. [parse-thread architecture](../../parse-thread/arch.md) §2 Module map item 1 **Directory reconcile**.

Home is this Project. The caller is the git Load Actor. [parse-thread](../../parse-thread/project.md) owns the reconcile body. [core-refinement](../../core-refinement/project.md) owns the inform doors and the workspace lock, and already has [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) and [07 — Git changed list sets Unparsed](../../core-refinement/issues/07-git-changed-list-unparsed.md). This ticket is the caller shift on git Load.

Alan, 2026-10-09. The Azure App Service plan limits CPU to no more than 60% over a short window, and to no more than 5% averaged over a day. Today's use is about 1%.

Alan, 2026-10-08. The Load command queues exactly its target for the Parse thread, and nothing more.

1. Load on a Workspace queues that Workspace Node only.
2. Load on a Directory queues that Directory Node only.
3. Load on a File queues that File Node only.
4. When the selection holds more than one node, Load queues each selected item. One queue item per selected item.

git pull stays the whole tracked branch. The parse queue is the Load target list. A Directory Load does not also queue its Workspace. A File Load does not also queue its Directory or its Workspace.

## Current Load targets

Read from this tree.

1. **Selection range** — [UpdateHelpers.fs](../../../src/Client/UpdateHelpers.fs) `selectedLoadTargetIds` (line 92) returns every child id in the selection range. [UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) `loadOpFor` (line 133) and `deskLoadOp` (line 105) refuse that list when [ResidentProjection.fs](../../../src/Shared/ResidentProjection.fs) `selectionSpansMultipleWorkspaces` (line 418) is true. A multi-node selection inside one Workspace passes that guard. Tests: [LoadCaptureTests.fs](../../../tests/Shared.Tests/LoadCaptureTests.fs) lines 186 and 194.
2. **One focus on the request** — `loadSaveCommandRequest` ([UpdateHelpers.fs](../../../src/Client/UpdateHelpers.fs) line 61) sets `ActorStart.focusId` to the focused node, or to the zoom root when the selection is empty. [LoadSaveCommand.fs](../../../src/Shared/LoadSaveCommand.fs) line 8 has operation, pre-pick, and `start`. It has no selected-id list. `ActorStart.graphIds` ([CommandRequest.fs](../../../src/Shared/CommandRequest.fs) line 107) is the Included expand of Zoom, not the selection.
3. **Server uses that one focus** — [GithubTransportActor.fs](../../../src/Server/GithubTransportActor.fs) `workspaceFromFocus` (line 54) and `runLoad` (line 120) take `focusId`. `runLoad` calls `continueLoad` for the Workspace label, then `parseFocusFile` (line 92) when that focus is a File. A Directory focus still reconciles the whole Workspace. `queueLoadRequest` ([UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 14) parks one `QueuedLoad`. [App.fs](../../../src/Client/App.fs) line 101 replays `deskLoadOp`, which again uses the focus.
4. **Kinds the command can start** — [CommandEntry.fs](../../../src/Shared/CommandEntry.fs) `contextualTarget` (line 82) returns `ReconcileWorkspace`, `ReconcileDirectory`, or `ParseFile`. [WorkspaceUpload.fs](../../../src/Shared/WorkspaceUpload.fs) `plan` (line 76) maps those three. `loadAvailable` ([UpdateWorkspaceLoad.fs](../../../src/Client/UpdateWorkspaceLoad.fs) line 158) is true for those three, and for the Workspaces container when Desktop push is available. That container path is `CreateWorkspaceFromFolder` ([UpdateWorkspaceSync.fs](../../../src/Client/UpdateWorkspaceSync.fs) `focusIsWorkspaces`, line 165). It is not a Workspace, Directory, or File target.
5. **Fetch uses the range** — `tryStartLoadFetch` ([UpdateHelpers.fs](../../../src/Client/UpdateHelpers.fs) line 112) builds one fetch target per id in the selection. That fetch is not a parse-queue item.
6. **Enqueue after MarkUnparsed** — [ParseThread.fs](../../../src/Server/ParseThread.fs) `afterPost` (line 46) marks the named ids Unparsed and pushes those ids. A separate draft PR from another agent disables that push. Requeue stays off for this ticket. Field testing of this ticket runs without that push. Turning the push back on is a later ticket.

## What to build

A person runs Load on a Workspace, a Directory, a File, or several of those in one Workspace. git Load still pulls the tracked branch. The command then marks each Load target Unparsed and pushes that target onto the Parse stack, and finishes. It pushes nothing else. The Parse thread works those items in small slices, with a yield or a sleep between slices. A throw in the Actor becomes ActorStop reason `ActorFailed`, and the process stays up. A throw in one Parse item is logged, and the Parse thread continues.

Point at [core-refinement architecture](../../core-refinement/arch.md) §10 **Core loop** and **Parse thread** for State, Interface, and Uses. Reuse `InMsg` `MarkUnparsed` and the existing parse-stack push. [Route registration](../../../src/Server/RouteRegistration.fs) already passes those two functions into [Parse thread](../../../src/Server/ParseThread.fs). `CoreMailbox.load` accepts a File Node only. A Workspace Node and a Directory Node use the same parse push. This ticket adds no InMsg case and no HTTP route.

The workspace lock in §6 is a later step. This ticket uses today's pull, then queues the Load targets.

### 1. Expand

Stand the inform path, the Actor failure catch, the paced Parse loop, and the `.git` / symlink skip beside today's `continueLoad` call. The production Actor still calls `reconcileWorkspace` until Migrate.

1. [ ] Inform path — The inform path marks each Load target Unparsed through the existing `InMsg` `MarkUnparsed` private add, then pushes that target on the existing parse stack. A Workspace target pushes that Workspace Node only. A Directory target pushes that Directory Node only. A File target pushes that File Node only. A selection pushes one item per selected node and no other node. The path does not call `reconcileWorkspace`. It does not wait for the Parse thread to finish.
2. [ ] Workspace test — Load on a Workspace Node records `MarkUnparsed` and one parse push for that node. No child Directory and no File is in that push.
3. [ ] Directory test — Load on a Directory Node records `MarkUnparsed` and one parse push for that Directory Node. The enclosing Workspace Node is not in that push.
4. [ ] File test — Load on a File Node records `MarkUnparsed` and one parse push for that File Node. `planParseFile` does not run on the Actor. The enclosing Directory Node and Workspace Node are not in that push.
5. [ ] Multi-selection test — A selection of two nodes in one Workspace records two queue items, one per selected node, and no third item. `reconcileWorkspace` is not called.
6. [ ] Actor failure — [Github transport actor](../../../src/Server/GithubTransportActor.fs) `actorFn` catches an exception from the body. The Actor ends with ActorStop reason `ActorFailed` and that message. Today `actorFn` has no try/with, and [Core actor pool](../../../src/Server/Core/CoreActorPool.fs) starts the body with `Async.Start` (about line 308), so a throw can kill the process.
7. [ ] Actor failure test — A test throws from the git dependency. `actorStop` receives `ActorFailed` with the message. The test process stays up.
8. [ ] Parse item failure — [Parse thread](../../../src/Server/ParseThread.fs) `loop` (about line 105) wraps each stack item. One failure is logged, in the same style as `reportPost`, and the loop takes the next item.
9. [ ] Parse item failure test — A test feeds a failing item and then a good item. The log records the failure. The good item runs.
10. [ ] Short-window cap — The Parse thread paces each queued target in small slices and yields or sleeps between slices. Each short window stays under 60% CPU. No slice burst comes near that quota. The Azure App Service plan limit is no more than 60% CPU over a short window.
11. [ ] Daily budget — The same work respects a 5% CPU average over a day. Today's use is about 1%. The design spends that budget on changed work.
12. [ ] Unchanged target — When nothing changed, the Parse thread does not run a full reconcile of that target.
13. [ ] Pace test — A test reconciles a Workspace of several directories and records a yield or a sleep between slices. A test of an unchanged target records no full walk. The test does not sample Azure CPU. The 60% window and the 5% day are the budgets those mechanisms meet. The test runs with the push after `MarkUnparsed` still off.
14. [ ] Requeue stays off — This ticket does not restore the parse push that follows `InMsg` `MarkUnparsed`. Field testing runs while that push stays disabled.
15. [ ] Skip `.git` and symlinks — [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) `diskDirectories` skips a child named `.git` and a symlink directory. [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) `emptyDirectoryFileRels` already skips a child named `.git`. It also skips a symlink directory. File discovery already skips a `.git` path (`DocumentPersistPath.shouldSkipDiscoveryRel`).
16. [ ] Skip test — A `.git` directory and a symlink directory are not planned as Directory Nodes, and `emptyDirectoryFileRels` does not walk them.

### 2. Migrate

git Load calls the inform path after pull and finishes. The Browser after-step stays. Ticket 22 decides that after-step.

1. [ ] Actor informs — After pull, the Actor marks each Load target Unparsed, pushes those targets, and finishes. The Actor body does not call `reconcileWorkspace`. It does not call `parseFocusFile`.
2. [ ] Browser stays — `gitLoadAfterOp` still starts directory reconcile or focused-file parse, as [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md) records. This ticket does not edit that client path.
3. [ ] Migrate test — A git Load whose focus is a Directory records `MarkUnparsed` and a parse push for that Directory Node only, then ActorStop success, with no `reconcileWorkspace` call. The Workspace, Directory, and File tests from Expand still pass on the Actor path. A client test still sees `gitLoadAfterOp` start `ContinueDirectoryReconcile` or `parseFocusedFile`.

### 3. Contract

The Actor's `continueLoad` wiring to `reconcileWorkspace` is gone. The paced Parse loop is the only reconcile the Actor starts, and it starts that reconcile for the Load targets only. The HTTP route stays for the Browser. The push after `MarkUnparsed` stays off.

1. [ ] Actor wiring — `GithubTransportActor.productionDependencies` has no `reconcileWorkspace` call. The `continueLoad` dependency that only existed to call that function is gone.
2. [ ] Route stays — `POST /ambit/workspace/reconciliation/directory` still calls `reconcileWorkspace` when the path is empty ([Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) about line 345). Ticket 22 owns that caller.
3. [ ] Paced loop only — The Parse thread consumer yields or sleeps between slices of a workspace reconcile. A tight loop that walks a whole workspace with no yield and no sleep is gone.
4. [ ] Contract test — A test shows the Actor production wiring has no `reconcileWorkspace` reference, and the directory route still answers. A second test shows the Parse consumer yields or sleeps between workspace slices.

## Scope questions

1. **Browser walk and the 60% cap** — This ticket's 60% and 5% acceptance bind the Parse thread. The Browser still posts an unpaced reconcile until ticket 22. That post can still fill a short window. Recommended: do not treat the Browser post as a failure of this ticket, and do not delete it here to meet the cap.

## Out of scope

1. **Browser after-step** — [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md). Alan's rule is there: a Browser Load must not trigger a client reconcile function. This ticket does not edit that client path.
2. **Workspace lock** — [core-refinement architecture](../../core-refinement/arch.md) §3 step 4 **§6 locks catch-up** and §6 **Workspace lock**. This ticket does not take that lock.
3. **Git changed list** — [07 — Git changed list sets Unparsed](../../core-refinement/issues/07-git-changed-list-unparsed.md). Both Unparsed approaches stay. This ticket queues the Load targets. It does not mark the git changed list.
4. **Workspaces container** — Focus on the Workspaces node still plans `CreateWorkspaceFromFolder` when Desktop push is available. That path is not a Workspace, Directory, or File queue target.
5. **Azure CPU sampling** — CI does not read the App Service CPU meter. The pace test locks slices, the yield or sleep, and the unchanged skip.
6. **Other Actors** — The try/with is this Actor's body. A pool-wide catch around every `actorFn` is a later change.
7. **Requeue after MarkUnparsed** — The push that follows `InMsg` `MarkUnparsed` stays disabled. Field testing of this ticket runs without it. A later ticket turns that push back on.

## See also

- [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile** and §3 step 2 Migrate item 1 **Workspace lock handoff**
- [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)

## Comments

- 2026-10-09 — Alan. Azure App Service CPU is no more than 60% over a short window, and no more than 5% averaged over a day. Today's use is about 1%. The Parse thread paces a reconcile. A full reconcile does not run when nothing changed.
- 2026-10-08 — Alan. Load queues exactly its target for the Parse thread. A Workspace queues that Workspace Node. A Directory queues that Directory Node. A File queues that File Node. A multi-node selection queues one item per selected node.
- 2026-10-09 — Alan. Requeue after `MarkUnparsed` stays off. This ticket is field-tested without that push. Turning the push back on is a later ticket.
- 2026-10-09 — Alan. Load from a Browser must not trigger any client reconcile function. The call chain and the removal live on [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md).
