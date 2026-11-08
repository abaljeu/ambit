# 30 — GitHub save path: missing workspace upward scan and workspace upload

Scope for [30 — GitHub save path: missing workspace upward scan and workspace upload](../issues/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md) on branch `origin/staging` at `fcc01cd0`. Mode: implement.

## Signatures

[ ] `type GithubTransportOperation = Load | Save` in `src/Server/GithubTransportActor.fs`.
[ ] `type GithubTransportGit` records `withWorkTreeGate`, `pullTracked`, `commitAll`, and `pushTracked` in `src/Server/GithubTransportActor.fs`.
[ ] `type GithubTransportActorDependencies = { git: GithubTransportGit }` in `src/Server/GithubTransportActor.fs`.
[ ] `let private workspaceFromFocus dataDir graph focusId` returns `Result<NodeId * string, string>` in `src/Server/GithubTransportActor.fs`.
[ ] `let private pullTracked git workspaceRoot` returns `Result<LoadPullHeads, string>` in `src/Server/GithubTransportActor.fs`.
[ ] `let private saveTracked git workspaceRoot` returns `Result<string, string>` in `src/Server/GithubTransportActor.fs`.
[ ] `let private finishLoadOn changes dataDir workspaceLabel workspaceId changedPaths` returns `Async<Result<unit, string>>` in `src/Server/GithubTransportActor.fs`.
[ ] `let internal finishLoadPull workspace workspaceId changedPaths` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runLoadOn dependencies dataDir changes workspaceId workspaceRoot` in `src/Server/GithubTransportActor.fs`.
[ ] `let internal runLoad dependencies dataDir changes workspaceId workspaceRoot` in `src/Server/GithubTransportActor.fs`.
[ ] `let private loadFocus changes focusId` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runLoadChoice dependencies dataDir changes choice workspaceId root` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runLoadStep dataDir changes graph focusId workspaceId dependencies root` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runSaveStep workspaceId dependencies root` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runBody dataDir operation dependencies focusId changes` in `src/Server/GithubTransportActor.fs`.
[ ] `let private runActorFunction dataDir operation dependencies focusId : ActorFunction` in `src/Server/GithubTransportActor.fs`.
[ ] `let loadActorFunction dataDir dependencies focusId` in `src/Server/GithubTransportActor.fs`.
[ ] `let saveActorFunction dataDir dependencies focusId` in `src/Server/GithubTransportActor.fs`.
[ ] `let start host caller dataDir dependencies operation request` in `src/Server/GithubTransportActor.fs`.
[ ] `let enclosingWorkspace graph nodeId : NodeId option` in `src/Shared/GraphQuery.fs`.
[ ] `let enclosing graph predicate nodeId : NodeId option` in `src/Shared/GraphQuery.fs`.
[ ] `let enclosingSubject graph nodeId : (NodeId * LoadSubject) option` in `src/Shared/PathPick.fs` module `LoadSubject`.
[ ] `let pullTrackedBranch workspaceRoot tracked` in `src/Server/WorkspaceGit.fs`.
[ ] `let pullTracked workspaceRoot` in `src/Server/WorkspaceGit.fs`.
[ ] `let commitTracked workspaceRoot baseMsg clientHint` in `src/Server/WorkspaceGit.fs`.
[ ] `let pushTrackedBranch workspaceRoot tracked commitOutput` in `src/Server/WorkspaceGit.fs`.
[ ] `let saveTracked workspaceRoot baseMsg clientHint` in `src/Server/WorkspaceGit.fs`.
[ ] `let completeRemoteWins workspaceRoot output` in `src/Server/WorkspaceGit.fs`.
[ ] `and ActorChanges` holds `postChanges`, `actorStop`, `graphAtEvent`, `loadNode`, `postNotice`, and `postGraphOnly` in `src/Shared/History.fs`.
[ ] `let loadNode door subject` in `src/Server/Core/CoreDoor.fs`.
[ ] `let mbLoadNode host caller subject` in `src/Server/Core/CoreMailboxApi.fs`.
[ ] `let persistNode dataDir graph nodeId` in `src/Server/DocumentPersistChange.fs`.
[ ] `type LoadSaveCommandRouter` in `src/Server/LoadSaveRouting.fs`.
[ ] `let resolvePath dataDir state request` in `LoadSaveRouting` in `src/Server/LoadSaveRouting.fs`.
[ ] `type ActorStart` and `type ActorOrder` in `src/Shared/History.fs`.
[ ] `Order.StartActorDeprecated of ActorStart` and `Order.StartActor of ActorOrder` in `src/Shared/History.fs`.

## Match sites

[ ] `GithubTransportOperation.Load` and `Save` in `peerName` and `transportActorName` in `src/Server/GithubTransportActor.fs` module `GithubTransportActor`.
[ ] `workspaceFromFocus` calls `GraphQuery.enclosingWorkspace` and `DocumentPersistPath.workspaceRootFor` in `GithubTransportActor.workspaceFromFocus`.
[ ] `pullTracked` calls `readLoadCommit`, `git.commitAll` with `"gambol: git Load"`, and `pullAndReadHeads` in `GithubTransportActor.pullTracked`.
[ ] `saveTracked` calls `git.commitAll` with `"gambol: git Save"` then `git.pushTracked` in `GithubTransportActor.saveTracked`.
[ ] `finishLoadOn` calls `LazyLoadReconciliationServer.reconcileFromChanges` then `changes.loadNode workspaceId` in `GithubTransportActor.finishLoadOn`.
[ ] `finishLoadPull` calls `CoreDoor.actorChanges workspace.handle` in `GithubTransportActor.finishLoadPull`.
[ ] `runLoadOn` calls `pullTracked` then `finishLoadOn` in `GithubTransportActor.runLoadOn`.
[ ] `runLoad` wraps `changes` with `CoreDoor.actorChanges` in `GithubTransportActor.runLoad`.
[ ] `runLoadStep` calls `LoadSubject.enclosingSubject` then `runLoadChoice` in `GithubTransportActor.runLoadStep`.
[ ] `runSaveStep` calls only `saveTracked dependencies.git root` in `GithubTransportActor.runSaveStep`.
[ ] `runBody` matches `GithubTransportOperation.Load` to `runLoadStep` and `Save` to `runSaveStep` in `GithubTransportActor.runBody`.
[ ] `runBody` calls `workspaceFromFocus` before that match in `GithubTransportActor.runBody`.
[ ] `runActorFunction` calls `runBody` then `changes.actorStop` in `GithubTransportActor.runActorFunction`.
[ ] `loadActorFunction` and `saveActorFunction` call `runActorFunction` with `Load` or `Save` in `GithubTransportActor`.
[ ] `start` matches `operation` to `loadActorFunction` or `saveActorFunction` in `GithubTransportActor.start`.
[ ] `workspaceGit` maps `pullTracked` to `WorkspaceGit.pullTrackedBranch` in `GithubTransportActor`.
[ ] `LoadSubject.enclosingSubject` uses `GraphQuery.enclosing` with `isSubjectNode` in `src/Shared/PathPick.fs`.
[ ] `RouteRegistration` maps `LoadSavePath.Desk` to `CoreMailboxApi.startLoadSaveCommand` and Git to `GithubTransportActor.start` in `src/Server/RouteRegistration.fs` handler for `/ambit/load-save-command`.
[ ] `LoadSaveRouting.resolvePath` uses `LoadPathChoice.ofRequest` and `PathPick.resolveCommand` in `src/Server/LoadSaveRouting.fs`.
[ ] `CoreActorPool.startLoadSaveCommand` starts desk or peer actor from `LoadSavePath` in `src/Server/Core/CoreActorPool.fs`.
[ ] `CoreDoor.actorChanges` exposes `loadNode` from `CoreDoor.loadNode` in `src/Server/Core/CoreDoor.fs`.

## Existing tests

[ ] ``enclosingWorkspace resolves named workspace and canonical roots`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosingWorkspace` from Workspace, Directory, File, root, trash, and workspaces container nodes.
[ ] ``pullTracked fast forwards the current tracked branch`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.pullTracked`.
[ ] ``pullTracked takes the remote file when both sides edited`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks remote-wins pull.
[ ] ``saveTracked commits then pushes while honoring gitignore`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks commit then push on the tracked branch.
[ ] ``saveTracked rejects a non fast forward push`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks push rejection.
[ ] ``git Load gates pull from subnode Focus`` in `tests/Server.Tests/GithubTransportActorTests.fs` checks Load commit, pull, and gate order from a subnode focus.
[ ] ``git Load posts that Workspace id and does not reconcile`` in `tests/Server.Tests/GithubTransportActorTests.fs` checks `loadNode` runs for the Workspace id after pull.
[ ] ``git Save gates commit then releases before push`` in `tests/Server.Tests/GithubTransportActorTests.fs` checks Save calls commit then push with gate release between them.
[ ] ``Load and Save return the same conflict reject shape`` in `tests/Server.Tests/GithubTransportActorTests.fs` checks Save fails on commit conflict without calling pull.
[ ] ``routed Git Save commits then pushes through Peer Actor`` in `tests/Server.Tests/LoadSaveCommandTests.fs` checks end-to-end git Save push via routing.
[ ] ``plain Save with a GitHub remote pushes from a file focus`` in `tests/Server.Tests/LoadSaveCommandTests.fs` checks plain Save routes to git and pushes from a File focus.
[ ] `GithubTransportLoadPullTests` call `GithubTransportActor.runLoad` with fakes and production `WorkspaceGit` for pull and reconciliation.
[ ] ``App source does not host or invoke GitHub transport`` in `tests/Server.Tests/AppGithubTransportBoundaryTests.fs` keeps Client and Desktop free of transport symbols.

## Copy-from

[ ] `workspaceFromFocus` in `GithubTransportActor` already resolves Workspace id and work-tree root via `GraphQuery.enclosingWorkspace`.
[ ] `runLoadOn` and `pullTracked` in `GithubTransportActor` already run commit-before-pull and head diff for Load stage 3.
[ ] `WorkspaceGit.pullTrackedBranch` and `completeRemoteWins` in `src/Server/WorkspaceGit.fs` already implement remote-wins pull with automerge on conflict.
[ ] `WorkspaceGit.saveTracked` in `src/Server/WorkspaceGit.fs` already implements commit then push for stage 2 and stage 4 git work.
[ ] `DocumentPersistChange.persistNode` in `src/Server/DocumentPersistChange.fs` already writes one graph node to disk for stage 1 persist focus.
[ ] `finishLoadOn` pattern of `changes.loadNode workspaceId` in `GithubTransportActor.finishLoadOn` posts the Workspace after Load; Save does not call it today.
[ ] `LoadSaveRouting.resolvePath` in `src/Server/LoadSaveRouting.fs` already isolates desk path choice from git path choice.

## Claims that differ

[ ] Ticket **Reported** says Save skips the upward Workspace scan; `runBody` calls `workspaceFromFocus` for `GithubTransportOperation.Save`, which calls `GraphQuery.enclosingWorkspace`.
[ ] Ticket **Reported** says Save skips Workspace upload with unknown cause; `runSaveStep` calls `saveTracked`, which calls `pushTracked`, and integration tests push successfully.
[ ] Ticket **Reported** sequences persist focus, commit, pull, then push; `runSaveStep` runs only `saveTracked` (commit then push) with no persist step and no `pullTracked` call.
[ ] Ticket **Observed** says `saveTracked` is commit then push; that matches `GithubTransportActor.saveTracked` and `WorkspaceGit.saveTracked`.
[ ] Ticket **Observed** says `finishLoadOn` calls `changes.loadNode` with the Workspace id; Save never calls `changes.loadNode`.
[ ] Ticket **Prior lock** [10 — git Save is commit then push](10-git-save-commit-then-push.md) omits pull and persist-focus stages; [30 — GitHub save path: missing workspace upward scan and workspace upload](../issues/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md) adds those stages in Alan's 2026-11-08 voice.
[ ] Plan reference [StartActorDeprecated gap](../../actor-as-client/reports/startactor-deprecated-gap.md) is absent on this branch.
[ ] Plan reference [Git Load and Save actor functions](../../actor-as-client/reports/git-load-and-save-actor-functions.md) is absent on this branch.
[ ] Spoken name `FirstFactorStart` has no definition under `src/` on this branch.
[ ] [79 — Place git Save as Order.Save](../../core-refinement/issues/79-place-git-save-as-order-save.md) describes `runOperation` and `Order.Save`; this tree has `saveActorFunction` and no `runOperation` or `Order.Save`.

## New tests

[ ] ``git Save persists focus then commits pulls and pushes`` in `tests/Server.Tests/GithubTransportActorTests.fs` should observe step order through a fake `GithubTransportGit` at the `GithubTransportActor.start` public seam.
[ ] ``git Save uses workspace root from subnode focus`` in `tests/Server.Tests/GithubTransportActorTests.fs` should observe `commit` and `push` roots match `DocumentPersistPath.workspaceRootFor` for a focus under a Workspace.
[ ] ``git Save gates commit then releases before push`` in `tests/Server.Tests/GithubTransportActorTests.fs` should gain assertions that `pullTracked` runs between commit and push when stage 3 lands.
[ ] ``routed Git Save commits then pushes through Peer Actor`` in `tests/Server.Tests/LoadSaveCommandTests.fs` should still pass after pull is inserted, or gain a sibling test that clones verify after pull-merge.

## Investigation

[ ] **Which Save** — **verified:** Alan means `saveActorFunction` in `GithubTransportActor`, which `start` selects for `GithubTransportOperation.Save` and which runs `runActorFunction` → `runBody` → `runSaveStep`.
[ ] **Which scan** — **verified:** Load and Save both call `workspaceFromFocus`, which calls `GraphQuery.enclosingWorkspace` on the owner chain. Load alone then calls `LoadSubject.enclosingSubject` inside `runLoadStep`. **reported:** Alan said Save skips the Workspace scan; current code does not skip `enclosingWorkspace` on Save.
[ ] **Which value is in hand** — **verified:** After `workspaceFromFocus` succeeds, `runBody` holds `workspaceId` and `root` (work-tree directory string) and passes both into `runSaveStep`.
[ ] **Which upload** — **intended:** Stage 1 is graph→file persist of the focus node, stages 2–4 are git commit, git pull with Load resolution, and git push of the whole Workspace work tree. **verified:** Save today runs only git commit and push via `saveTracked`; it does not post the Workspace node through `loadNode`.
[ ] **Why the skip** — **verified:** `runSaveStep` never uses `workspaceId`, never calls `pullTracked`, and never calls `DocumentPersistChange.persistNode` or another persist entry for `focusId`. The body is only `saveTracked dependencies.git root`. The missing upload stages are absent calls, not a disabled branch on an existing call.
[ ] **Where the other path sits** — **verified:** `src/Server/RouteRegistration.fs` isolates desk Load and Save in the `/ambit/load-save-command` handler when `path = LoadSavePath.Desk` routes to `CoreMailboxApi.startLoadSaveCommand` and `DeskLoadPush.runLoadSave`.
[ ] **Stage map** — **verified:** Stage 1 persist focus has no Save actor function today (`ActorChanges` has no `persistNode`; use `DocumentPersistChange.persistNode` as the disk helper). Stage 2 commit is `saveTracked` → `commitAll` / `WorkspaceGit.commitTracked`. Stage 3 pull is `pullTracked` in `GithubTransportActor` used only from `runLoadOn`, not from `runSaveStep`. Stage 4 push is `saveTracked` → `pushTracked` / `WorkspaceGit.pushTrackedBranch`.
[ ] **Spoken struct** — **reported:** Alan said `FirstFactorStart` is a struct. **verified:** `src/` contains no symbol `FirstFactorStart`.

## Verified cause (upload skip)

[ ] `runSaveStep` receives `workspaceId` and `root` from `runBody` but the parameter `workspaceId` is unused.
[ ] The function body is only `saveTracked dependencies.git root |> Result.map ignore` inside `async.Return`.
[ ] `saveTracked` on the Save path never calls the module-private `pullTracked` that Load uses from `runLoadOn`.
[ ] `finishLoadOn` calls `changes.loadNode workspaceId` after Load; no Save counterpart calls `changes.loadNode` or `persistNode` for focus or Workspace.

```248:251:src/Server/GithubTransportActor.fs
    let private runSaveStep workspaceId dependencies root =
        async.Return(
            saveTracked dependencies.git root
            |> Result.map ignore)
```

[ ] Unused binding: `workspaceId` in `runSaveStep`.
[ ] Missing call: `pullTracked dependencies.git workspaceRoot` (present on Load in `runLoadOn`, absent on Save).
[ ] Missing call: persist focus (for example `DocumentPersistChange.persistNode dataDir graph focusId`) before git commit on the Save path.

## Refactor and naming notes

[ ] `loadActorFunction` and `saveActorFunction` are separate entry points; shared logic sits below them per Alan's split aim.
[ ] `runActorFunction` and `runBody` still match on `GithubTransportOperation` to pick `runLoadStep` or `runSaveStep`.
[ ] `Order.StartActorDeprecated of ActorStart` and `Order.StartActor of ActorOrder` live in `src/Shared/History.fs`.
[ ] `ActorStart.runDefinitionId` is the third field `runDefinitionId` on `ActorStart`.
[ ] `ActorOrder.actorNodeId` is the second field on `ActorOrder`.
[ ] `plan/actor-as-client/reports/startactor-deprecated-gap.md` is not in the tree on this branch.
[ ] `plan/actor-as-client/reports/git-load-and-save-actor-functions.md` is not in the tree on this branch.

## Smallest later build (≤8 source files, ≤4 tests)

[ ] Source: `src/Server/GithubTransportActor.fs` (required: new Save body with four stages, shared helpers with Load, remove unused `workspaceId` or use it).
[ ] Source: `src/Server/DocumentPersistChange.fs` only if persist focus must change the public persist helper (unlikely).
[ ] Source: `src/Server/WorkspaceGit.fs` only if pull behavior for Save needs a new entry (unlikely; reuse `pullTrackedBranch`).
[ ] Tests changed: ``git Save gates commit then releases before push`` in `tests/Server.Tests/GithubTransportActorTests.fs`.
[ ] Tests changed: ``Load and Save return the same conflict reject shape`` in `tests/Server.Tests/GithubTransportActorTests.fs` if Save gains a pull stage before commit conflict checks.
[ ] Tests added or changed: one new ``git Save`` stage-order test in `tests/Server.Tests/GithubTransportActorTests.fs`.
[ ] Tests changed: ``routed Git Save commits then pushes through Peer Actor`` in `tests/Server.Tests/LoadSaveCommandTests.fs`.
[ ] Count: 1 source file and up to 3 test files if persist stays inside `GithubTransportActor` via `DocumentPersistChange.persistNode` without new mailbox APIs.
