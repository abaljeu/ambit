# 23 — Run the Load actor for Git and Neither — scope

Scope mode: implement. Ticket: [23 — Run the Load actor for Git and Neither](../issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md).

## 1. Signatures

### 1.1 — Shared types (`src/Shared/History.fs`)

[ ] `LoadSavePath`: `| Git | Desk` at lines 34–36.

[ ] `GithubTransportOperation`: `| Load | Save` in `src/Server/GithubTransportActor.fs` lines 6–8.

[ ] `Order.StartActor`: `| StartActor of ActorOrder` in `src/Shared/History.fs` line 120.

[ ] `Notice.ActorStopped`: `| ActorStopped of focusId: NodeId * result: ActorResult` in `src/Shared/History.fs` line 70.

[ ] `ActorOrder`: `{ actor: ActorFunction; actorNodeId: NodeId; actorName: string }` in `src/Shared/History.fs` lines 100–103.

[ ] `FunctionRun`: `{ getGraph: GraphCarrier; secret: Credential }` in `src/Shared/History.fs` lines 85–87.

[ ] `ActorFunction`: `FunctionRun -> ActorChanges -> Async<unit>` in `src/Shared/History.fs` line 89.

[ ] `ActorChanges`: record with `postChanges`, `actorStop`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, `postGraphOnly` in `src/Shared/History.fs` lines 91–98.

[ ] `EventBody.ActorStop`: `Notice.ActorStopped(focusId, result)` in `src/Shared/History.fs` lines 192–193 (client reads stops as `EventBody.ActorStop`, not `EventBody.Order`).

### 1.2 — `GithubTransportActor` (`src/Server/GithubTransportActor.fs`)

[ ] `loadActorFunction`: `dataDir: string -> dependencies: GithubTransportActorDependencies -> focusId: NodeId -> ActorFunction` at lines 372–373; body is `actorFunction (runLoadBody dataDir dependencies focusId)`.

[ ] `saveActorFunction`: `dataDir -> dependencies -> focusId -> ActorFunction` at lines 376–377.

[ ] `start`: `host: MailboxHost -> caller: Caller -> dataDir: string -> dependencies: GithubTransportActorDependencies -> operation: GithubTransportOperation -> request: ActorStart -> Async<Result<unit, string>>` at lines 387–406; matches `Load` to `loadActorFunction`, then `CoreMailboxApi.coreMailboxStartActorFromFunction`.

[ ] `runLoad`: `dependencies -> dataDir -> changes: CoreChangesDoor -> workspaceId -> workspaceRoot -> Async<Result<unit, string>>` at lines 153–165.

[ ] `finishLoadOn`: private, `{ changes; dataDir; workspaceId } -> label -> changedPaths -> async` at lines 99–112; calls `LazyLoadReconciliationServer.reconcileFromChanges`, then `changes.loadNode workspaceId`.

[ ] `pullTracked`: private, `git: GithubTransportGit -> locked: LockedWorkspace -> Result<LoadPullHeads, string>` at lines 81–83.

[ ] `runLoadChoice`: private, `dependencies -> root -> WorkspaceTarget -> (NodeId * LoadSubject) option -> Async<Result<unit, string>>` at lines 177–184.

[ ] `finishActor`: private, `changes: ActorChanges -> work: Async<Result<unit, string>> -> Async<unit>` at lines 191–199; ends with `changes.actorStop (actorOutcome step)`.

[ ] `actorFunction`: private, `(changes -> Async<Result<unit, string>>) -> ActorFunction` at lines 368–369: `fun _ changes -> finishActor changes (body changes)`.

### 1.3 — `PathPick` (`src/Shared/PathPick.fs`)

[ ] `plainLoadStaysDesk`: private, `LoadPathChoice -> bool` at lines 87–94; true for `Plain` Load when subject is `Directory`, `File`, or `None` (Workspace is false).

[ ] `resolveCommand`: `LoadPathChoice -> (unit -> Result<bool, string>) -> Result<LoadSavePath, string>` at lines 98–105.

[ ] `effectiveLoadPath`: `LoadPathChoice -> LoadSavePath -> LoadSavePath` at lines 108–115.

### 1.4 — Mailbox and pool (`src/Server/Core`)

[ ] `CoreMailboxApi.startLoadSaveCommand`: `host -> caller -> path: LoadSavePath -> peerName: PeerActorName -> request: LoadSaveCommandRequest -> Async<Result<unit, string>>` at `CoreMailboxApi.fs` lines 385–398.

[ ] `CoreMailboxApi.coreMailboxStartActorFromFunction`: `host -> caller -> placement: ActorOrderFromStart.FunctionActorPlacement -> Async<Result<EventId, string>>` at lines 347–353; pipes `ActorOrderFromStart.orderFromPlacement` into `MailboxHost.postActorOrder`.

[ ] `CoreMailboxApi.mbLoadNode` (load door): `host -> caller -> subject: NodeId -> Async<Result<EventId, string>>` at lines 401–410; order `Order.Load subject`.

[ ] `CoreMailboxApi.actorStop`: `host -> caller -> result: ActorResult -> Async<Result<unit, string>>` at lines 412–418.

[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand`: internal, mailbox message handler at `CoreMailboxLoad.fs` lines 34–56; calls `CoreMailboxActors.dispatchActorStartResult` with `DeskLoadPush.runLoadSave`.

[ ] `DeskLoadPush.runLoadSave`: `LoadSaveContext -> (NodeId -> Result<unit, string>) -> LoadSaveCommandRequest -> LoadSavePath -> Result<Credential option, string>` at `DeskLoadPush.fs` lines 24–38.

[ ] `CoreActorPool.startLoadSaveCommand`: `_run -> path -> _choice -> _request -> Result<Credential option, string>` at `CoreActorPool.fs` lines 242–251; both `Desk` and `Git` return `Ok None`.

[ ] `LazyLoadReconciliationServer.reconcileFromChanges`: `changes: ActorChanges -> dataDir: string -> workspaceLabel: string -> changedPaths: LazyLoadReconciliation.ChangedPath list -> Async<Result<unit, string>>` at `LazyLoadReconciliationServer.fs` lines 389–399.

[ ] `CoreDoor.actorStop`: `door -> result -> Async<Result<unit, string>>` at `CoreDoor.fs` lines 51–52.

[ ] `CoreDoor.loadNode`: `door -> subject -> Async<Result<EventId, string>>` at `CoreDoor.fs` lines 54–55.

[ ] `CoreDoor.actorChanges`: `CoreChangesDoor -> ActorChanges` at `CoreDoor.fs` lines 61–83.

### 1.5 — Pool launch before `Order.StartActor`

[ ] `MailboxHost.postActorOrder`: `host -> caller -> order: ActorOrder -> Async<Result<EventId, string>>` at `MailboxHost.fs` lines 104–118; wraps `Order.StartActor order` in `postPlacedOrder`.

[ ] `CoreMailboxLoop.placeOrder` matches `Order.StartActor order` to `replyActorOrder` at `CoreMailboxLoop.fs` lines 280–281.

[ ] `CoreMailboxLoop.scheduleActorOrder`: `context -> caller -> order -> Result<Credential, string>` at lines 201–215; calls `context.pool.startFunction (functionStartOf context order)` then `context.pool.schedule`.

[ ] `CoreMailboxLoop.replyActorOrder` comment at line 238: `/// Pool first. StartActor is posted only after the affirmation.`

[ ] `CoreMailboxLoop.replyAffirmedStart`: posts `EventBody.Order(Order.StartActor order)` only after `scheduleActorOrder` returns `Ok secret` at lines 217–236.

[ ] `CoreActorPool.startFunction` / `runStartFunction` / `putFunctionStart`: affirm via `admitFunctionStart` then mint credential at `CoreActorPool.fs` lines 208–240.

[ ] `logDeskOrder` does not exist in `CoreMailboxLoad.fs` (PR 589 removal is in place).

## 2. Match sites

### 2.1 — Load vs Save at `GithubTransportActor.start`

[ ] `src/Server/GithubTransportActor.fs` `start` lines 389–393: `GithubTransportOperation.Load` → `loadActorFunction`; `Save` → `saveActorFunction`.

[ ] `src/Server/RouteRegistration.fs` `/ambit/load-save-command` lines 345–361: `LoadSavePath.Desk` → `CoreMailboxApi.startLoadSaveCommand`; else → `GithubTransportActor.start` with `githubOperation request.operation`.

### 2.2 — Path pick: Git vs Desk vs Neither

[ ] `LoadSaveRouting.resolvePath` (`src/Server/LoadSaveRouting.fs` lines 21–32) builds `LoadPathChoice.ofRequest` and calls `PathPick.resolveCommand` with `WorkspaceGit.remoteExists`.

[ ] `PathPick.resolveCommand` lines 102–105: when `plainLoadStaysDesk choice` → `Ok LoadSavePath.Desk` without calling `remoteExists` (Neither plain Directory/File today stays Desk).

[ ] `PathPick.resolve` lines 76–77: `Plain` pre-pick uses `remoteExists` → `choose` → Git when remote exists (Workspace plain Load path).

[ ] `DeskLoadPush.runLoadSave` lines 31–33: `effectiveLoadPath` can coerce a chosen Git path back to Desk for plain Directory/File Load.

[ ] `DeskLoadPush.subjectToLoad` lines 16–21: only `LoadSavePath.Desk` + `Load` mailbox-loads the focus when it is a load subject.

[ ] Neither (plain Load of Directory/File with remote, no desk mapping): today `resolveCommand` → Desk; `RouteRegistration` → `startLoadSaveCommand`; no `GithubTransportActor.start`.

### 2.3 — Load actor body: Git pull vs subject load

[ ] `runLoadBody` lines 357–366: `withWorkspace` → `runLoadChoice` with `LoadSubject.enclosingSubject graph focusId`.

[ ] `runLoadChoice` lines 178–184: `Workspace` → `runLoadOn` (pull via `pullTracked`); `Directory`/`File` → `loadFocus` (`loadNode` only); `None` → `missingSubject`.

[ ] `runLoadOn` lines 145–148: success path → `applyPulledChanges` → `finishLoadOn` (reconcile + `loadNode` on workspace id).

[ ] `loadFocus` lines 167–171: `changes.loadNode focusId` only (no `reconcileFromChanges`).

### 2.4 — Who posts `Order.StartActor`

[ ] Git-path Load/Save: `GithubTransportActor.start` → `coreMailboxStartActorFromFunction` → `MailboxHost.postActorOrder` → mailbox `placeOrder` → `replyAffirmedStart` (after pool `startFunction`).

[ ] Desk-path Load/Save: `startLoadSaveCommand` → `dispatchStartLoadSaveCommand` → `dispatchActorStartResult` with `DeskLoadPush.runLoadSave` → `CoreActorPool.startLoadSaveCommand` returns `Ok None` → `prepareActorStart` line 71 `Ok (None, _)` → **no** `Order.StartActor`, no pool actor.

[ ] Named desk tests expect no `Order.StartActor` for desk plain load-save (`LoadSaveCommandTests.fs` line 1058).

### 2.5 — Who posts stop / `Notice.ActorStopped`

[ ] Load/Save Git actor: `finishActor` → `changes.actorStop` → `CoreMailboxApi.actorStop` with `Authority "Actor"` → `CoreMailboxActors.dispatchActorStop` → `CoreEventDispatch.actorStop` stores `EventBody.ActorStop` (= `Notice.ActorStopped`) at `History.fs` lines 192–193.

[ ] Desk Load: `dispatchLoad` → parse push only; no `actorStop`.

[ ] Client Cancel live set: `Order.StartActor` via `SyncLogic` / `ActorLive`; cleared when `Notice.ActorStopped` arrives on poll (`GithubTransportActorTests.fs` ``git Load tells the client the actor completed`` pattern).

## 3. Existing tests

### 3.1 — Ticket-named tests

[ ] ``git Load of a Directory clears Cancel after the actor stops`` in `tests/Server.Tests/LoadSaveCommandTests.fs` line 625: explicit `LoadSavePrePick.Git` on a Directory; asserts `LoadSavePath.Git`, `Order.StartActor` with `actorName = "github-load"` and `actorNodeId = dirId`, `ActorSucceeded` stop, Cancel visible after command then cleared after poll.

[ ] ``Desk Load of a Directory does not leave a cancelable actor`` in `tests/Server.Tests/ParseThreadLoadTests.fs` line 208: explicit `LoadSavePrePick.Desk`; `startLoadSaveCommand` with `LoadSavePath.Desk`; asserts focus id not in `ActorLive` set after history.

### 3.2 — `GithubTransportActorTests.fs` (Load / Cancel / actor-stop)

[ ] ``git Load tells the client the actor completed`` (line 169): `GithubTransportActor.start` Load; Cancel after start, cleared after `waitForStop` and poll.

[ ] ``git Load gates pull from subnode Focus`` (line 241): subnode focus resolves to Workspace subject; expects commit+pull steps and actor stop.

[ ] ``git Load posts that Workspace id and does not reconcile`` (line 276): workspace-focused Load; parse push expectations.

[ ] ``git Load ends when the pull fails and posts nothing`` (line 319): failed pull → `ActorFailed`, no extra reconcile posts.

[ ] ``Peer Actor row is live only while git work runs`` (line 586): pool live set during work.

### 3.3 — `LoadSaveCommandTests.fs` (routing / desk / actors)

[ ] ``every Git choice starts the Server Peer Actor`` (line 433): routed Git path starts actor on focus.

[ ] ``plain Load of a non-Workspace returns Desk when a remote exists`` (line 465): plain Load on default harness focus (non-workspace subject) → `LoadSavePath.Desk`, `command = None`, route harness actor not started.

[ ] ``plain Load of a File or Directory stays Desk when a remote exists`` (line 739): plain Load on file and directory → Desk, no `hasActorStart`.

[ ] ``Desk load-save runs no Actor and posts no StartActor`` (line 1011): desk path; empty pool live; no `StartActor` or `ActorStop` in history.

[ ] ``routed Git Load pulls the tracked branch`` (line 519): workspace plain Load with git worktree → Git path, actor stop, parse change.

### 3.4 — `ParseThreadLoadTests.fs` (mailbox Load / desk)

[ ] ``mailbox Load of a Directory pushes onto the Parse stack`` (line 174): direct `mbLoadNode`, not load-save command.

[ ] ``Desk Load command pushes a Directory and a File`` (line 308): explicit desk command pushes parse stack; no actor cancel assertion.

### 3.5 — `PathPickTests.fs` (plain stays Desk)

[ ] ``plain Load of a Directory, File, or non-subject stays Desk`` (line 58): documents current `plainLoadStaysDesk` behavior.

## 4. Copy-from

### 4.1 — Git Load actor already posts start and stop

[ ] Copy chain: `GithubTransportActor.start` → `coreMailboxStartActorFromFunction` → `MailboxHost.postActorOrder` → `CoreMailboxLoop.replyActorOrder` → pool `startFunction` then `Order.StartActor`.

[ ] Actor body: `actorFunction` wraps `finishActor`; Git workspace path uses `runLoadOn` → `pullTracked` → `finishLoadOn` → `reconcileFromChanges` → `loadNode`.

[ ] Stop: `finishActor` lines 196–198 always calls `changes.actorStop` after the body `Async`.

### 4.2 — Pool affirms before `Order.StartActor`

[ ] Same as ``coreMailboxStartActorFromFunction places StartActor from the selector`` in `tests/Server.Tests/ActorOrderFromStartTests.fs` line 55: one stored `Order.StartActor` after placement; no `StartActorDeprecated`.

[ ] Refusal: `scheduleActorOrder` / `startFunction` error → `replyActorOrder` replies `Error err` with no `replyAffirmedStart` (no StartActor row).

### 4.3 — PR 589 / desk stub actor

[ ] `CoreMailboxLoad.logDeskOrder` is gone; desk Load uses `DeskLoadPush.runLoadSave` + `dispatchLoad` only (`CoreMailboxLoad.fs` lines 34–55).

[ ] `EventJson.stubActor` remains for wire decode only (`src/Shared/EventJson.fs` line 285); desk path does not post `StartActor` with empty name in current code.

### 4.4 — Cancel-clear pattern for tests

[ ] ``git Load of a Directory clears Cancel after the actor stops`` uses `routeMatrixHarness`, `Api.postLoadSaveCommand`, `waitForStop`, `ActorLive.offersCancel`, and poll via `SyncLogic.applyServerTail` (lines 623–687).

[ ] ``git Load tells the client the actor completed`` in `GithubTransportActorTests.fs` is the lower-level copy for direct `GithubTransportActor.start`.

## 5. Implement claims

### 5.1 — Ticket one-job and Alan shape

[ ] Claim: plain Neither Load (Directory/File, no desktop mapping) runs `loadActorFunction`, reconciles, posts `Order.StartActor` after pool affirm, then stop so Cancel clears. Current: `plainLoadStaysDesk` forces Desk; desk path posts no actor and no stop; gap is routing plus actor body Neither arm.

[ ] Claim (Alan shape): after `( Git → pull | Neither → do_nothing )`, always `reconcile x`. Current: reconcile runs only on workspace pull path inside `finishLoadOn`; Directory/File Git Load uses `loadFocus` only with no `reconcileFromChanges`; gap in `runLoadChoice` / post-arm reconcile.

### 5.2 — Ticket Process checkboxes

[ ] Claim: Git Load and Neither Load both call `GithubTransportActor.start` with `loadActorFunction`. Current: only `LoadSavePath.Git` route branch calls `start`; Neither uses Desk branch. Gap: `PathPick` / routing (and possibly drop `effectiveLoadPath` desk coercion for Neither).

[ ] Claim: Git arm keeps pull in `runLoad`. Current: matches for `LoadSubject.Workspace` via `runLoadOn` / `pullTracked`. No gap for workspace Git pull.

[ ] Claim: Neither arm is an empty step inside the actor. Current: no Neither arm; desk path runs mailbox Load outside the actor. Gap: `runLoadChoice` or successor needs `async { return Ok () }` (or equivalent) before reconcile.

[ ] Claim: after the arm, actor reconciles; Git pull keeps `finishLoadOn`. Current: `finishLoadOn` only after pull. Gap: Neither needs reconcile with empty `changedPaths` (still `finishLoadOn` or shared post-step).

[ ] Claim: actor posts `Order.StartActor` at start and stop through `finishActor`. Current: true for Git-path `GithubTransportActor.start` only. Gap: extend to Neither once routed to Git transport.

[ ] Claim: plain Neither Load reaches `GithubTransportActor.start`; `plainLoadStaysDesk` currently blocks that. Current: lines 87–94 keep Directory/File/None on Desk. Gap: change `plainLoadStaysDesk` / `resolveCommand` / `effectiveLoadPath` so Neither resolves to Git while explicit Desk pre-pick stays Desk.

[ ] Claim: leave Desktop_mapped unused. Current: no WebDAV arm in `GithubTransportActor`; desk command path is mailbox push. No gap for this ticket.

[ ] Claim: leave `saveActorFunction` unchanged. Current: `saveActorFunction` separate from `loadActorFunction`. No gap.

### 5.3 — Ticket Inputs (Working)

[ ] `GithubTransportActor.start` matches Load/Save to `loadActorFunction` / `saveActorFunction`. Current: matches at lines 389–393. No gap.

[ ] `runLoadChoice` pulls for `LoadSubject.Workspace` and `loadFocus` for Directory/File. Current: matches lines 178–183. Gap only if Alan shape requires reconcile after Directory/File arms.

[ ] Desk arm of `RouteRegistration.registerStateRoutes` calls `startLoadSaveCommand`. Current: `path = LoadSavePath.Desk` branch lines 345–353. No gap.

[ ] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for Desk. Current: lines 249–251 return `Ok None` for both Desk and Git (Git path bypasses this via `GithubTransportActor.start`). Desk behavior matches ticket.

## 6. Test seams

### 6.1 — ``git Load of a Directory clears Cancel after the actor stops``

[ ] Seam: `Api.postLoadSaveCommand` with production-style router (`LoadSaveRouting.resolvePath` + `GithubTransportActor.start` for Git path).

[ ] Observes: `LoadSavePath.Git`, `Order.StartActor` on directory id, `EventBody.ActorStop` / `Notice.ActorStopped`, `ActorLive.offersCancel` before poll and false after poll.

[ ] Likely stable after Neither work if explicit Git Directory Load behavior unchanged; add reconcile assertions only if Alan shape extends reconcile to Directory Git Load.

### 6.2 — ``Desk Load of a Directory does not leave a cancelable actor``

[ ] Seam: `CoreMailboxApi.startLoadSaveCommand` with `LoadSavePrePick.Desk` and `LoadSavePath.Desk` (explicit desk, not Neither).

[ ] Observes: `ActorLive.applyEvents` live set excludes directory id.

[ ] Stays valid when Neither moves to Git actor; no assertion change for this test name.

### 6.3 — Neither case (plain Load Directory/File with remote)

[ ] Primary seam today: ``plain Load of a File or Directory stays Desk when a remote exists`` (`LoadSaveCommandTests.fs` line 739) via `Api.postLoadSaveCommand` + `productionRouter`.

[ ] Assertion change: expect `LoadSavePath.Git` (or Git transport) instead of Desk; expect `response.command = Some _` when Git actor runs; expect `hasStartedActor` / `hasActorStop` for focus id; expect Cancel lifecycle like ``git Load of a Directory clears Cancel after the actor stops`` (poll clears `ActorLive.offersCancel`).

[ ] `PathPickTests.fs` ``plain Load of a Directory, File, or non-subject stays Desk`` must invert or split: Neither routes to Git for plain Load when remote exists.

[ ] ``plain Load of a non-Workspace returns Desk when a remote exists`` may need rename or split: harness focus may be Neither vs Workspace.

## 7. File facts

### 7.1 — `src/Server/GithubTransportActor.fs`

[ ] Change required: yes. Edit `runLoadChoice` / `runLoadBody` (and possibly add a Neither finish path) so Neither runs empty step then `finishLoadOn` or shared reconcile+`loadNode`; keep workspace Git pull on `runLoadOn`.

### 7.2 — `src/Shared/PathPick.fs`

[ ] Change required: yes. Edit `plainLoadStaysDesk`, `resolveCommand`, and/or `effectiveLoadPath` so plain Neither Load (Directory/File with remote, not explicit Desk) resolves to `LoadSavePath.Git` for the transport actor.

### 7.3 — `src/Server/RouteRegistration.fs`

[ ] Change required: optional. Existing `if path = LoadSavePath.Desk` vs `GithubTransportActor.start` split (lines 345–361) already routes Git path to the Load actor; change only if a new path enum or branch is introduced (ticket names only Desk and Git).

### 7.4 — `src/Server/Core/CoreMailboxApi.fs`

[ ] Change required: no for signatures. `startLoadSaveCommand` and `coreMailboxStartActorFromFunction` already match ticket; call sites change via routing only.

### 7.5 — `src/Server/Core/CoreMailboxLoad.fs`

[ ] Change required: no. `dispatchStartLoadSaveCommand` stays desk-only; do not restore `logDeskOrder`.

### 7.6 — `tests/Server.Tests/GithubTransportActorTests.fs`

[ ] Change required: likely yes if unit-level Neither Load is added (direct `GithubTransportActor.start` with Directory focus, pull not called, reconcile+stop).

### 7.7 — `tests/Server.Tests/LoadSaveCommandTests.fs`

[ ] Change required: yes. Update ``plain Load of a File or Directory stays Desk when a remote exists`` and related plain-Load routing tests; keep ``git Load of a Directory clears Cancel after the actor stops`` as Git explicit control.

### 7.8 — `tests/Server.Tests/ParseThreadLoadTests.fs`

[ ] Change required: no for ``Desk Load of a Directory does not leave a cancelable actor`` unless desk semantics move; optional new threaded test if plain Neither Load is exercised through full host.

### 7.9 — `tests/Shared.Tests/PathPickTests.fs`

[ ] Change required: yes when `PathPick` changes (ticket Outputs list does not name this file; implementer should update paired PathPick tests).
