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

## What to build

A person runs git Load. The Actor pulls, tells Core the Workspace Node is Unparsed, pushes that node onto the Parse stack, and finishes. The Actor does not walk the Workspace. The Parse thread reconciles that Workspace in small slices, with a yield or a sleep between slices. A throw in the Actor becomes ActorStop reason `ActorFailed`, and the process stays up. A throw in one Parse item is logged, and the Parse thread continues.

Point at [core-refinement architecture](../../core-refinement/arch.md) §10 **Core loop** and **Parse thread** for State, Interface, and Uses. Reuse `InMsg` `MarkUnparsed` and the existing parse-stack push. [Route registration](../../../src/Server/RouteRegistration.fs) already passes those two functions into [Parse thread](../../../src/Server/ParseThread.fs). `CoreMailbox.load` accepts a File Node only. The Workspace Node uses the parse push. This ticket adds no InMsg case and no HTTP route.

The workspace lock in §6 is a later step. This ticket uses today's pull, then informs Core.

### 1. Expand

Stand the inform path, the Actor failure catch, the paced Parse loop, and the `.git` / symlink skip beside today's `continueLoad` call. The production Actor still calls `reconcileWorkspace` until Migrate.

1. [ ] Inform path — The inform path marks the Workspace Node Unparsed through the existing `InMsg` `MarkUnparsed` private add, then pushes that node on the existing parse stack. It does not call `reconcileWorkspace`. It does not wait for the Parse thread to finish.
2. [ ] Inform test — A test runs the inform path for a Workspace Node. The mailbox receives `MarkUnparsed` for that node. The parse stack receives that node. `reconcileWorkspace` is not called.
3. [ ] Actor failure — [Github transport actor](../../../src/Server/GithubTransportActor.fs) `actorFn` catches an exception from the body. The Actor ends with ActorStop reason `ActorFailed` and that message. Today `actorFn` has no try/with, and [Core actor pool](../../../src/Server/Core/CoreActorPool.fs) starts the body with `Async.Start` (about line 308), so a throw can kill the process.
4. [ ] Actor failure test — A test throws from the git dependency. `actorStop` receives `ActorFailed` with the message. The test process stays up.
5. [ ] Parse item failure — [Parse thread](../../../src/Server/ParseThread.fs) `loop` (about line 105) wraps each stack item. One failure is logged, in the same style as `reportPost`, and the loop takes the next item.
6. [ ] Parse item failure test — A test feeds a failing item and then a good item. The log records the failure. The good item runs.
7. [ ] Short-window cap — The Parse thread paces a whole-workspace reconcile in small slices and yields or sleeps between slices. Each short window stays under 60% CPU. No slice burst comes near that quota. The Azure App Service plan limit is no more than 60% CPU over a short window.
8. [ ] Daily budget — The same reconcile respects a 5% CPU average over a day. Today's use is about 1%. The design spends that budget on changed work.
9. [ ] Unchanged workspace — When nothing changed, the Parse thread does not run a full workspace reconcile.
10. [ ] Pace test — A test reconciles a workspace of several directories and records a yield or a sleep between slices. A test of an unchanged workspace records no full directory walk. The test does not sample Azure CPU. The 60% window and the 5% day are the budgets those mechanisms meet.
11. [ ] Skip `.git` and symlinks — [Directory reconcile](../../../src/Shared/dotnet/DirectoryReconcile.fs) `diskDirectories` skips a child named `.git` and a symlink directory. [Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) `emptyDirectoryFileRels` already skips a child named `.git`. It also skips a symlink directory. File discovery already skips a `.git` path (`DocumentPersistPath.shouldSkipDiscoveryRel`).
12. [ ] Skip test — A `.git` directory and a symlink directory are not planned as Directory Nodes, and `emptyDirectoryFileRels` does not walk them.

### 2. Migrate

git Load calls the inform path after pull and finishes. The Browser after-step stays. Ticket 22 decides that after-step.

1. [ ] Actor informs — After pull, the Actor marks the Workspace Node Unparsed, pushes it, and finishes. The Actor body does not call `reconcileWorkspace`.
2. [ ] Browser stays — `gitLoadAfterOp` still starts directory reconcile or focused-file parse, as [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md) records. This ticket does not edit that client path.
3. [ ] Migrate test — A git Load test with a changed pull records `MarkUnparsed` and a parse push for the Workspace Node, then ActorStop success, with no `reconcileWorkspace` call. A client test still sees `gitLoadAfterOp` start `ContinueDirectoryReconcile` or `parseFocusedFile`.

### 3. Contract

The Actor's `continueLoad` wiring to `reconcileWorkspace` is gone. The paced Parse loop is the only whole-workspace reconcile the Actor starts. The HTTP route stays for the Browser.

1. [ ] Actor wiring — `GithubTransportActor.productionDependencies` has no `reconcileWorkspace` call. The `continueLoad` dependency that only existed to call that function is gone.
2. [ ] Route stays — `POST /ambit/workspace/reconciliation/directory` still calls `reconcileWorkspace` when the path is empty ([Lazy load reconciliation server](../../../src/Server/LazyLoadReconciliationServer.fs) about line 345). Ticket 22 owns that caller.
3. [ ] Paced loop only — The Parse thread consumer yields or sleeps between slices of a workspace reconcile. A tight loop that walks a whole workspace with no yield and no sleep is gone.
4. [ ] Contract test — A test shows the Actor production wiring has no `reconcileWorkspace` reference, and the directory route still answers. A second test shows the Parse consumer yields or sleeps between workspace slices.

## Scope questions

1. **Focused-file parse on the Actor** — `runLoad` still calls `parseFocusFile`, which runs `planParseFile` inline for a focused File. Recommended: leave that call in this ticket. The CPU incident is the whole-workspace walk. A later ticket can move that File onto the parse stack.
2. **Browser walk and the 60% cap** — This ticket's 60% and 5% acceptance bind the Parse thread. The Browser still posts an unpaced whole-workspace reconcile until ticket 22. That post can still fill a short window. Recommended: do not treat the Browser post as a failure of this ticket, and do not delete it here to meet the cap.

## Out of scope

1. **Browser after-step** — [22 — Browser Load paths that reconcile or parse](22-browser-load-reconcile-paths.md). `gitLoadAfterOp`, `ContinueDirectoryReconcile`, and `POST /ambit/workspace/reconciliation/directory` stay up.
2. **Workspace lock** — [core-refinement architecture](../../core-refinement/arch.md) §3 step 4 **§6 locks catch-up** and §6 **Workspace lock**. This ticket does not take that lock.
3. **Git changed list** — [07 — Git changed list sets Unparsed](../../core-refinement/issues/07-git-changed-list-unparsed.md). Both Unparsed approaches stay. This ticket pushes the Workspace Node.
4. **Azure CPU sampling** — CI does not read the App Service CPU meter. The pace test locks slices, the yield or sleep, and the unchanged skip.
5. **Other Actors** — The try/with is this Actor's body. A pool-wide catch around every `actorFn` is a later change.

## See also

- [core-refinement architecture](../../core-refinement/arch.md) §5 item 10 **Directory reconcile** and §3 step 2 Migrate item 1 **Workspace lock handoff**
- [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)

## Comments

- 2026-10-09 — Alan. Azure App Service CPU is no more than 60% over a short window, and no more than 5% averaged over a day. Today's use is about 1%. The Parse thread paces a whole-workspace reconcile. A full reconcile does not run when nothing changed.
