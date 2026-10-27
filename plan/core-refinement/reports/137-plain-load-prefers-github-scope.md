# 137 — Plain Load prefers the GitHub mapping — scope report

Ticket: [137 — Plain Load prefers the GitHub mapping](../issues/137-plain-load-prefers-github.md).

## Slice

[ ] When a workspace has a GitHub remote, a plain Load on Workspace, Directory, or File resolves through `remoteExists` to Git; without a remote, plain Load resolves to Desk; explicit Desk or Git pre-picks stay unchanged; remove `plainLoadStaysDesk`, `effectiveLoadPath`, and Desk coercion in `DeskLoadPush`; extend the Git Load actor so Directory and File subjects follow Load semantics (pull only for Workspace; mark Unparsed, reconcile or parse the immediate selection only).

## Signatures

### Current type and function signatures this change uses

[ ] `LoadSavePath` — `Git | Desk` — `src/Shared/PathPick.fs` lines 4–6.

[ ] `LoadSavePrePick` — `Plain | Git | Desk` — `src/Shared/PathPick.fs` lines 9–12.

[ ] `LoadSaveOperation` — `Load | Save` — `src/Shared/PathPick.fs` lines 15–17.

[ ] `LoadSubject` — `Workspace | Directory | File` — `src/Shared/PathPick.fs` lines 21–24.

[ ] `LoadSubjectRejection` — `NotLoadSubject | NotFound` — `src/Shared/PathPick.fs` lines 27–29.

[ ] `LoadSubject.ofId` — `Graph -> NodeId -> Result<LoadSubject, LoadSubjectRejection>` — `src/Shared/PathPick.fs` lines 36–47.

[ ] `LoadPathChoice` — record `{ operation; prePick; subject }` — `src/Shared/PathPick.fs` lines 50–54.

[ ] `PathPick.choose` — `bool -> LoadSavePath` — `src/Shared/PathPick.fs` lines 59–63.

[ ] `PathPick.resolve` — `LoadSavePrePick -> (unit -> Result<bool, string>) -> Result<LoadSavePath, string>` — `src/Shared/PathPick.fs` lines 65–73.

[ ] `PathPick.subjectOf` — `Graph -> NodeId -> LoadSubject option` — `src/Shared/PathPick.fs` lines 75–78.

[ ] `PathPick.plainLoadStaysDesk` — private `LoadPathChoice -> bool` — `src/Shared/PathPick.fs` lines 81–88 (remove).

[ ] `PathPick.resolveCommand` — `LoadPathChoice -> (unit -> Result<bool, string>) -> Result<LoadSavePath, string>` — `src/Shared/PathPick.fs` lines 92–99.

[ ] `PathPick.effectiveLoadPath` — `LoadPathChoice -> LoadSavePath -> LoadSavePath` — `src/Shared/PathPick.fs` lines 102–109 (remove).

[ ] `LoadPathChoice.ofRequest` — `LoadSaveCommandRequest -> Graph -> LoadPathChoice` — `src/Shared/LoadSaveCommand.fs` lines 17–23.

[ ] `LoadSaveCommandRequest` — record — `src/Shared/LoadSaveCommand.fs` lines 3–7.

[ ] `LoadSaveRouting.resolvePath` — `string -> State -> LoadSaveCommandRequest -> Result<LoadSavePath, string>` — `src/Server/LoadSaveRouting.fs` lines 16–32.

[ ] `WorkspaceGit.remoteExists` — `string -> Result<bool, string>` — `src/Server/WorkspaceGit.fs` line 117.

[ ] `DeskLoadPush.runLoadSave` — `LoadSaveContext -> (NodeId -> Result<unit, string>) -> LoadSaveCommandRequest -> LoadSavePath -> Result<Credential option, string>` — `src/Server/Core/DeskLoadPush.fs` lines 24–38.

[ ] `DeskLoadPush.subjectToLoad` — private — `src/Server/Core/DeskLoadPush.fs` lines 8–21.

[ ] `CoreActorPool.startLoadSaveCommand` — `LoadSaveContext -> LoadSavePath -> LoadPathChoice -> LoadSaveCommandRequest -> Result<Credential option, string>` — `src/Server/Core/CoreActorPool.fs` lines 269–283.

[ ] `CoreMailboxLoad.dispatchLoad` — private mailbox Load — `src/Server/Core/CoreMailboxLoad.fs` lines 22–32.

[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` — `src/Server/Core/CoreMailboxLoad.fs` lines 34–55.

[ ] `CoreMailboxApi.mbLoadNode` — `MailboxHost -> Caller -> NodeId -> Async<Result<EventId, string>>` — `src/Server/Core/CoreMailboxApi.fs` lines 297–309.

[ ] `CoreDoor.loadNode` — `CoreChangesDoor -> NodeId -> Async<Result<unit, string>>` — `src/Server/Core/CoreDoor.fs` lines 48–49.

[ ] `GithubTransportActor.runLoad` — `dependencies -> dataDir -> CoreChangesDoor -> NodeId -> string -> Async<Result<unit, string>>` — `src/Server/GithubTransportActor.fs` lines 150–173.

[ ] `GithubTransportActor.finishLoadPull` — internal — `src/Server/GithubTransportActor.fs` lines 130–146.

[ ] `GithubTransportActor.runBody` / `runOperation` — `src/Server/GithubTransportActor.fs` lines 175–221.

[ ] `LazyLoadReconciliationServer.reconcileDirectory` — `WorkspaceReconcile -> string -> Async<Result<Failure list, string>>` — `src/Server/LazyLoadReconciliationServer.fs` lines 471–475.

[ ] `LazyLoadReconciliationServer.reconcileWorkspace` — `WorkspaceReconcile -> Async<...>` — `src/Server/LazyLoadReconciliationServer.fs` lines 478–481.

[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` — `WorkspaceReconcile -> ChangedPath list -> Async<...>` — `src/Server/LazyLoadReconciliationServer.fs` lines 403–434.

[ ] `CoreMailboxFilesAxis.markUnparsedThenPush` — `PersistHandlers -> (NodeId -> unit) -> NodeId -> Result<unit, string>` — `src/Server/Core/CoreMailboxFilesAxis.fs` lines 20–30.

[ ] `DocumentPersistWrite.planParseFile` — `string -> Graph -> NodeId -> string option -> Result<Op list, string>` — `src/Server/DocumentPersistWrite.fs` lines 120–133.

[ ] `ParseThread.parseOne` / `alreadyParsed` — `src/Server/ParseThread.fs` lines 52–56, 120–133.

[ ] `DocumentPartition.artifactDirectoryRelative` / `directoryDiskRelative` — `src/Shared/DocumentPartition.fs` lines 194–224.

[ ] `Api.postLoadSaveCommand` — `LoadSaveCommandRouter -> CoreChangesDoor -> string -> Async<IResult>` — `src/Server/Api.fs` lines 318–353.

## Match sites

### Each constructor, match, and caller, with file and function

[ ] `PathPick.plainLoadStaysDesk` — matches `LoadSubject.Workspace` vs `Directory | File | None` — `src/Shared/PathPick.fs` lines 84–88.

[ ] `PathPick.resolveCommand` — calls `plainLoadStaysDesk` then forces `Ok Desk` — `src/Shared/PathPick.fs` lines 96–99.

[ ] `PathPick.effectiveLoadPath` — calls `plainLoadStaysDesk` then forces `Desk` — `src/Shared/PathPick.fs` lines 106–107.

[ ] `PathPick.resolve` — `LoadSavePrePick.Plain` branch calls `remoteExists () |> Result.map choose` — `src/Shared/PathPick.fs` lines 70–71.

[ ] `LoadSaveRouting.resolvePath` — builds `LoadPathChoice.ofRequest` then `PathPick.resolveCommand` with `WorkspaceGit.remoteExists` — `src/Server/LoadSaveRouting.fs` lines 21–32.

[ ] `DeskLoadPush.runLoadSave` — `LoadPathChoice.ofRequest`, `PathPick.effectiveLoadPath`, `CoreActorPool.startLoadSaveCommand`, optional `load subject` — `src/Server/Core/DeskLoadPush.fs` lines 31–38.

[ ] `DeskLoadPush.subjectToLoad` — only `LoadSavePath.Desk` + `LoadSaveOperation.Load` + `LoadSubject.ofId` Ok — `src/Server/Core/DeskLoadPush.fs` lines 16–20.

[ ] `CoreMailboxLoad.dispatchStartLoadSaveCommand` — passes `DeskLoadPush.runLoadSave` and `dispatchLoad` — `src/Server/Core/CoreMailboxLoad.fs` lines 48–54.

[ ] `CoreMailboxLoad.dispatchLoad` — `isLoadSubject` then `context.parsePush subject` — `src/Server/Core/CoreMailboxLoad.fs` lines 30–32.

[ ] `CoreMailboxLoop.placeOrder` — `Order.Load subject` → `CoreMailboxLoad.replyLoad` — `src/Server/Core/CoreMailboxLoop.fs` lines 173–175.

[ ] `CoreMailboxLoop.makeMailBox` — `parsePush = markUnparsedThenPush` front end — `src/Server/Core/CoreMailboxLoop.fs` lines 324–328.

[ ] `CoreActorPool.startLoadSaveCommand` — `Desk` → `Ok None`; `Git` → `startPeerActor` — `src/Server/Core/CoreActorPool.fs` lines 276–283.

[ ] `RouteRegistration` — `LoadSaveRouting.resolvePath` + `CoreMailboxApi.startLoadSaveCommand` + `GithubTransportActor.peerName` — `src/Server/RouteRegistration.fs` lines 363–374.

[ ] `Api.postLoadSaveCommand` — `resolvePath` then `startCommand`; Desk returns path Desk without command events; Git returns path Git with `universalResponseFromHandle` — `src/Server/Api.fs` lines 331–352.

[ ] `GithubTransportActor.runBody` — `workspaceFromFocus dataDir graph input.focusId` then `runOperation` — `src/Server/GithubTransportActor.fs` lines 204–217.

[ ] `GithubTransportActor.runLoad` — `pullTracked`, `changedAfterPull`, `finishLoadPull` with `workspaceId` only — `src/Server/GithubTransportActor.fs` lines 157–172.

[ ] `GithubTransportActor.finishLoadPull` — `reconcileReceivedFiles` then `CoreDoor.loadNode workspace.handle workspaceId` — `src/Server/GithubTransportActor.fs` lines 136–145.

[ ] `LazyLoadReconciliationServer.registerDirectoryRoute` — HTTP POST `/ambit/workspace/reconciliation/directory` calls `reconcileWorkspace` or `reconcileDirectory` — `src/Server/LazyLoadReconciliationServer.fs` lines 530–559.

[ ] `ParseThread.parseOne` — `Special File` → `parseFile`; `Directory | Workspace` → `parseDirectory` — `src/Server/ParseThread.fs` lines 127–131.

[ ] `ParseThread.parseFile` — `DocumentPersistWrite.planParseFile` — `src/Server/ParseThread.fs` lines 85–90.

[ ] `ParseThread.parseDirectory` — `DirectoryReconcile.planDirectoryReconcile` with `DocumentPersistPath.discoverImmediateMembers` — `src/Server/ParseThread.fs` lines 104–117.

[ ] `LoadSaveCommandClient.applyResponse` — `Desk` → `continueDesk`; `Git` → `commandDone` — `src/Client/LoadSaveCommandClient.fs` lines 31–38.

[ ] `UpdateWorkspaceLoad.runDeskLoadAction` — `WorkspaceUploadAction.DesktopPush` for desk-mapped load (local folder) — `src/Client/UpdateWorkspaceLoad.fs` lines 45–53.

[ ] `WorkspaceFileSync` / `WorkspaceLocalInventory.listForPush` — desk push reads mapped local folder — `src/Shared/dotnet/WorkspaceFileSync.fs` lines 306, 416; `src/Shared/dotnet/WorkspaceLocalInventory.fs` line 128.

## Existing tests

### Each named test that already exists, its file, and what it checks

[ ] ``choose returns Git when a remote exists`` — `tests/Shared.Tests/PathPickTests.fs` lines 21–22 — `PathPick.choose true` is Git.

[ ] ``choose returns Desk when no remote exists`` — `tests/Shared.Tests/PathPickTests.fs` lines 25–26 — `PathPick.choose false` is Desk.

[ ] ``Plain resolves to Git when a remote exists`` — `tests/Shared.Tests/PathPickTests.fs` lines 29–33 — `PathPick.resolve Plain` with remote true (no subject in this API).

[ ] ``Plain resolves to Desk when no remote exists`` — `tests/Shared.Tests/PathPickTests.fs` lines 36–40.

[ ] ``explicit pre-pick bypasses the remote chooser`` — `tests/Shared.Tests/PathPickTests.fs` lines 45–55 — Git and Desk pre-picks skip `remoteExists`.

[ ] ``plain Load of a Directory, File, or non-subject stays Desk`` — `tests/Shared.Tests/PathPickTests.fs` lines 58–65 — **`PathPick.resolveCommand` Plain Load forces Desk** for Directory, File, and non-subject via helper `staysDesk` (lines 11–18).

[ ] ``plain Load of a Workspace uses the remote`` — `tests/Shared.Tests/PathPickTests.fs` lines 68–76 — Plain Load + Workspace subject + remote → Git.

[ ] ``explicit git Load of a File stays Git`` — `tests/Shared.Tests/PathPickTests.fs` lines 79–87.

[ ] ``plain Save of a Directory still asks the remote`` — `tests/Shared.Tests/PathPickTests.fs` lines 90–98 — Save is not subject to `plainLoadStaysDesk`.

[ ] ``effective path forces Desk for plain File Load`` — `tests/Shared.Tests/PathPickTests.fs` lines 101–109 — **`PathPick.effectiveLoadPath` coerces Git → Desk** for plain File Load.

[ ] ``plain Load of a non-Workspace returns Desk when a remote exists`` — `tests/Server.Tests/LoadSaveCommandTests.fs` lines 431–451 — API routing harness with `subject = Some Directory` in `routeMatrixRouter` (lines 368–375); expects Desk path and no peer start.

[ ] ``plain Load of a File or Directory stays Desk when a remote exists`` — `tests/Server.Tests/LoadSaveCommandTests.fs` lines 609–653 — production router + git harness; **expects Desk** for plain Load on file and directory foci; asserts no actor start on those ids.

[ ] ``Load on a file parses that file`` — `tests/Server.Tests/LoadSaveCommandTests.fs` lines 710–746 — plain Load on file with **no git remote**; expects **Desk** path and local disk parse under `dataDir`.

[ ] ``routed Git Load pulls the tracked branch`` — `tests/Server.Tests/LoadSaveCommandTests.fs` lines 480–513 — plain Load on **workspace** focus with git harness.

[ ] ``plain Load of a File or Directory ignores a Git path`` — `tests/Server.Tests/ParseThreadLoadTests.fs` lines 317–352 — passes `LoadSavePath.Git` into `startLoadSaveCommand` with Plain pre-pick; **`effectiveLoadPath` + Desk `subjectToLoad`** pushes dir and file onto parse stack (not git actor).

[ ] ``plain Load of a Workspace still requires the git actor`` — `tests/Server.Tests/ParseThreadLoadTests.fs` lines 355–384 — Plain Workspace + Git path → peer registration error, empty parse push.

[ ] ``explicit git Load of a File still requires the git actor`` — `tests/Server.Tests/ParseThreadLoadTests.fs` lines 387–414.

[ ] ``mailbox Load of File node pushes onto Parse stack`` — `tests/Server.Tests/ParseThreadLoadTests.fs` lines 104–124 — `mbLoadNode` seam.

[ ] ``mailbox Load of a Directory pushes onto the Parse stack`` — `tests/Server.Tests/ParseThreadLoadTests.fs` lines 174–193.

[ ] ``git Load pull marks a received file and its directory Unparsed`` — `tests/Server.Tests/GithubTransportLoadPullTests.fs` lines 76+ — `GithubTransportActor.runLoad` workspace pull + reconcile received files.

[ ] ``remoteExists reports any configured remote`` — `tests/Server.Tests/WorkspaceGitTests.fs` line 355+ — `WorkspaceGit.remoteExists`.

[ ] ``desktop-mapped Load and git Load after-step use the Parse stack`` — `tests/Server.Tests/DeskLoadParseStackTests.fs` lines 79–139 — client desk path uses DesktopPush then desk LoadSave; git Load uses Plain/Git without client reconcile hop.

## Copy-from

### The helper that already does the same job, with file and function

[ ] Plain remote pick without Desk override — `PathPick.resolve` with `LoadSavePrePick.Plain` and `choose` — `src/Shared/PathPick.fs` lines 65–73 (becomes the model for `resolveCommand` after removing `plainLoadStaysDesk`).

[ ] Workspace git Load after pull — `GithubTransportActor.finishLoadPull` — `reconcileReceivedFiles` + `CoreDoor.loadNode` on workspace id — `src/Server/GithubTransportActor.fs` lines 130–146.

[ ] Desk mailbox Load (mark Unparsed + parse push) — `CoreMailboxLoad.dispatchLoad` → `context.parsePush` — `src/Server/Core/CoreMailboxLoad.fs` lines 22–32; front end `CoreMailboxFilesAxis.markUnparsedThenPush` — `src/Server/Core/CoreMailboxFilesAxis.fs` lines 20–30.

[ ] Directory / workspace one-level disk reconcile (git worktree under `dataDir`, not desktop map) — `LazyLoadReconciliationServer.reconcileDirectory` / `reconcileWorkspace` — `src/Server/LazyLoadReconciliationServer.fs` lines 471–481; discovery via `DocumentPersistPath.discoverImmediateMembers` — `src/Server/LazyLoadReconciliationServer.fs` lines 186–210.

[ ] Parse-thread directory reconcile for a loaded node — `ParseThread.parseDirectory` → `DirectoryReconcile.planDirectoryReconcile` — `src/Server/ParseThread.fs` lines 98–117.

[ ] Single-file parse from server artifact tree — `DocumentPersistWrite.planParseFile` — `src/Server/DocumentPersistWrite.fs` lines 120–133 (reads `dataDir` worktree via `preparedParseText`, not `WorkspaceLocalInventory.listForPush`).

[ ] Directory relative path from graph node — `DocumentPartition.artifactDirectoryRelative` / `directoryDiskRelative` — `src/Shared/DocumentPartition.fs` lines 194–224 (for wiring `reconcileDirectory` when focus is a Directory).

## implement

### Each ticket claim that differs from the code

[ ] Ticket: plain Load on Directory, File, or non-subject uses `remoteExists` (Git when remote, Desk when not). Code: `PathPick.plainLoadStaysDesk` forces Desk for Plain Load on Directory, File, and non-subject — `src/Shared/PathPick.fs` lines 81–88, 96–97.

[ ] Ticket: remove `effectiveLoadPath`. Code: `DeskLoadPush.runLoadSave` coerces Git to Desk for those choices — `src/Server/Core/DeskLoadPush.fs` line 32; definition — `src/Shared/PathPick.fs` lines 102–109.

[ ] Ticket: explicit Desk and Git pre-picks unchanged. Code: already true in `PathPick.resolve` — `src/Shared/PathPick.fs` lines 72–73; `resolveCommand` only overrides Plain Load subjects — lines 96–99.

[ ] Ticket: Workspace git Load — pull, workspace Unparsed, parse/reconcile that folder only. Code: `GithubTransportActor.runLoad` pulls and `finishLoadPull` reconciles pull deltas then `loadNode workspaceId` — `src/Server/GithubTransportActor.fs` lines 157–145; does not use `input.focusId` as Load subject (always workspace id from enclosing workspace) — lines 54–65, 209–217.

[ ] Ticket: Directory git Load — no pull; Unparsed; reconcile; NodeParsed. Code: no git-actor branch for Directory focus; desk-only immediate push via `DeskLoadPush` when path coerced to Desk — `src/Server/Core/DeskLoadPush.fs` lines 16–20; HTTP `reconcileDirectory` exists but is not invoked from `GithubTransportActor` — `src/Server/LazyLoadReconciliationServer.fs` lines 471–475 vs `GithubTransportActor.fs` lines 184–190.

[ ] Ticket: File git Load — mark Unparsed and parse regardless of Parsed state; only immediate selection. Code: `ParseThread.parseOne` skips when `alreadyParsed` — `src/Server/ParseThread.fs` lines 52–56, 125; git actor never calls `loadNode` on file focus — `GithubTransportActor.finishLoadPull` only loads workspace — line 144.

[ ] Ticket: NodeParsing before parse changes the node. Code: `ParseThread` loop does not post `Notice.NodeParsing` (no match in `src/Server/ParseThread.fs`); handler is no-op — `src/Server/Core/CoreMailboxLoop.fs` line 109; comment documents intent — `src/Server/Core/CoreMailboxFilesAxis.fs` lines 41–42.

[ ] Ticket: NodeParsed only after real parse. Code: `ParseThread.afterPost` calls `finishParse` (`Notice.NodeParsed`) after successful op post — `src/Server/ParseThread.fs` lines 64–77, 197–200.

[ ] Ticket: do not construct `Op.SetDocumentState`. Code: reconciliation planners may emit it; `LazyLoadReconciliationServer.receivedFileIds` matches planner output — `src/Server/LazyLoadReconciliationServer.fs` lines 379–383 (consumers, not new ticket construction).

[ ] Ticket: plain Load on File in GitHub workspace avoids local-folder read. Code: Plain Load on File with remote resolves Desk; client `UpdateWorkspaceLoad` can run `DesktopPush` using `WorkspaceLocalInventory.listForPush` — `src/Client/UpdateWorkspaceLoad.fs` lines 45–53; `src/Shared/dotnet/WorkspaceLocalInventory.fs` line 128.

[ ] Git-path gap (implement, not greenfield): `GithubTransportActor.runOperation` / `runLoad` must branch on `LoadSubject.ofId graph input.focusId` — today only `GithubTransportOperation.Load` → workspace pull path — `src/Server/GithubTransportActor.fs` lines 183–190, 150–173; File/Directory need `CoreDoor.loadNode` on `input.focusId` (and pull omitted), reusing desk semantics without `effectiveLoadPath`.

### How each named new test observes the change at a public seam

[ ] **Plain Load on Directory with GitHub remote → Git** — assert `LoadSaveCommandResponse.path = Git` from `Api.postLoadSaveCommand` / `LoadSaveRouting.resolvePath` (same seam as ``plain Load of a File or Directory stays Desk when a remote exists`` — `tests/Server.Tests/LoadSaveCommandTests.fs` lines 628–644); update `PathPick.resolveCommand` unit cases in `tests/Shared.Tests/PathPickTests.fs` lines 58–65.

[ ] **Plain Load on File with GitHub remote → Git** — same HTTP response path seam; invert ``effective path forces Desk for plain File Load`` — `tests/Shared.Tests/PathPickTests.fs` lines 101–109.

[ ] **Plain Load on Directory without GitHub remote → Desk** — `PathPick.resolveCommand` with `remoteExists () = Ok false` and `LoadSubject.Directory`; integration test with harness without `git remote` (mirror ``Load on a file parses that file`` desk expectation — `tests/Server.Tests/LoadSaveCommandTests.fs` line 734).

[ ] **Plain Load on File in GitHub workspace — Unparsed + parse, no local-folder read** — git harness + `postLoadSaveCommand` with Plain Load on file focus: response path Git; assert parse stack or graph content from worktree file (not from a separate mapped root); assert no `LoadSavePath.Desk` and no client `WorkspaceUploadAction.DesktopPush` on that command (seams: `LoadSaveCommandClient.applyResponse`, `CoreMailboxApi.mbLoadNode` / event history, graph `documentState` and child text); may require new git-actor test alongside `GithubTransportLoadPullTests` pattern — `tests/Server.Tests/GithubTransportLoadPullTests.fs` lines 88–94.
