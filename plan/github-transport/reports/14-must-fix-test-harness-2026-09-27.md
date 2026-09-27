# 14 — Must-fix routed Git E2E test harness (2026-09-27)

Closes review gaps **3.2** and **3.3** in [14-independent-code-review-2026-09-27.md](14-independent-code-review-2026-09-27.md): prove **Git Save** and **Git Load** through `Api.postLoadSaveCommand` → `CoreMailbox.startLoadSaveCommand` → `CoreActorPool` → **real** `GithubTransportActor` (not stub peers), with Poll-visible lifecycle/Parse events on Load.

## Reuse map (existing fixtures)

| Concern | Exact symbols | Source |
|--------|----------------|--------|
| Temp data dir, admitted caller | `newTempDir`, `testCaller`, `admittedCredentials` | `TestBackend` |
| Request encode/decode | `EventJson.encodeLoadSaveCommandRequest`, `ApiResponseSerialization.decodeLoadSaveCommandResponseDecoder` | `LoadSaveCommandTests` (`encodeRequest`, `requireResponse`) |
| Production peer registration | `GithubTransportActor.productionDependencies`, `GithubTransportActor.actorFn`, `GithubTransportActor.peerName` | `RouteRegistration.createPersistenceContext` (mirror in test) |
| Production path resolution | `LoadSaveRouting.resolvePath` | `LoadSaveRouting.fs` |
| Graph seed (workspace + command + subnode focus) | `seedOperation`, `request`, `waitForStop` | `GithubTransportActorTests` |
| Bare remote + clone worktree | `trackedWorkspace`, `commitFile`, `configureIdentity`, `git`, `branchOid` | `WorkspaceGitTests` (private; lift or duplicate) |
| Workspace on disk + graph | `FileNodeOps.planCreateWorkspace`, `DocumentPersistWrite.writeDocument`, `postWorkspace` | `LazyLoadReconciliationServerTests` / `GraphOnlyChangePostTests` |
| Poll door | `encodePollRequest`, `Api.postPoll`, `decodeChangeSuccessResponseDecoder` | `CoreChangesTests`; HTTP variant `postPoll` in `StateEndpointTests` |
| Poll via mailbox (no HTTP) | `pollEventsSince` | `AskCancelHarness` |
| Git skip | `gitOnPath`, `Skip.IfNot(gitOnPath(), "git not on PATH")` | `WorkspaceGitTests` |
| Save remote proof | clone + `branchOid` compare | `WorkspaceGitTests` ``saveTracked commits then pushes while honoring gitignore`` |
| Load remote proof | `commitFile` on `source` + `git push origin main` | `WorkspaceGitTests` ``pullTracked fast forwards the current tracked branch`` |
| Parse event shape | `commandName = "Parse"` on reconciler posts | `GraphOnlyChangePostTests`; producer `LazyLoadReconciliationServer.reconcileChangedPathsWithDiscovery` → `GraphOnlyChangePost.postChunks` |

## Recommended shared harness (new private module or bottom of `LoadSaveCommandTests.fs`)

Keep each `[<Fact>]` ≤40 lines by splitting helpers (standards **3.3**).

### `registerProductionGithubPeers dataDir pool`

Same as production:

1. `let deps = GithubTransportActor.productionDependencies dataDir`
2. `pool.registerPeer (GithubTransportActor.peerName Load) (GithubTransportActor.actorFn dataDir Load deps)`
3. `pool.registerPeer (GithubTransportActor.peerName Save) (GithubTransportActor.actorFn dataDir Save deps)`

### `createLoadSaveHost dataDir`

1. `let pool = CoreActorPool.create ()`
2. `registerProductionGithubPeers dataDir pool`
3. `CoreMailbox.host pool (FileAgent.persist (FileAgent.create dataDir)) admittedCredentials`

### `productionLoadSaveRouter dataDir host`

Match `RouteRegistration` load-save route:

- `resolvePath = LoadSaveRouting.resolvePath dataDir`
- `startCommand path request` → `CoreMailbox.startLoadSaveCommand host testCaller path (GithubTransportActor.peerName (githubOperation request.operation)) request`
- Local `githubOperation`: `Load` → `GithubTransportOperation.Load`, `Save` → `Save`

### `seedGitWorkspaceHost ()`

1. `Skip.IfNot(gitOnPath(), …)` at fact entry.
2. `let dataDir = newTempDir ()`
3. **Remote worktree** (adapt `WorkspaceGitTests.trackedWorkspace`): bare `remote.git`, seed `source`, clone to `Path.Combine(dataDir, "home")`, `configureIdentity workspace`.
4. **Graph**: `let host = createLoadSaveHost dataDir`; `seedOperation host "load"` (or `"save"`) → `{ workspaceId; commandId; focusId }`.
5. `DocumentPersistWrite.writeDocument dataDir graph workspaceId` so `LoadSaveRouting.resolvePath` sees a repo with `WorkspaceGit.remoteExists`.
6. `CoreMailbox.flushSnapshot host` (optional, matches persistence tests).
7. Return `(dataDir, host, seed, workspace, parent, remote, source)` for assertions.

### `loadSaveBody operation prePick seed eventId`

Reuse `LoadSaveCommandTests.request`: `LoadSaveOperation`, `LoadSavePrePick.Git`, `start` fields from `GithubTransportActorTests.request seed`, `eventId` from last graph post or `EventId.zero` after seed.

### `postLoadSave host body`

1. `let router = productionLoadSaveRouter dataDir host`
2. `Api.postLoadSaveCommand router (CoreMailbox.coreChanges host testCaller) body |> Async.StartAsTask`
3. `requireResponse` → assert `path = Git`, `command = Some _` for Git.

### `waitForActor host focusId`

Reuse `GithubTransportActorTests.waitForStop` (poll `CoreMailbox.eventHistory` for `EventBody.ActorStop(focusId, ActorSucceeded)`).

### `pollSince handle afterId want`

1. `encodePollRequest { eventId = afterId; want = want }` (`CoreChangesTests`)
2. `Api.postPoll handle … body` → decode with `decodeChangeSuccessResponseDecoder`
3. Assert on `response.events` (Poll order: newest-head per `StateEndpointTests` comment).

### `eventsMatching events pred`

List helpers for `ActorStart` (focus = seed.focusId), `ActorStop(_, ActorSucceeded)`, `Change` with `commandName = "Parse"`.

---

## Test 1 — Git Save through command door (commit then push)

**Name:** ``Git Save through postLoadSaveCommand commits then pushes to origin``

**Steps:**

1. Harness: `seedGitWorkspaceHost ()` with operation `"save"`.
2. Dirty tree: `File.WriteAllText(Path.Combine(dataDir, "home", "saved.txt"), "saved")` (pattern from `WorkspaceGitTests` ``saveTracked commits then pushes``).
3. Capture `let pollAfter =` current tail id: `CoreMailbox.getEventId host` or last accepted id before POST.
4. `postLoadSave` with `LoadSaveOperation.Save`, `LoadSavePrePick.Git`.
5. `waitForActor host seed.focusId` → `Some ActorSucceeded`.
6. **Commit then push (remote proof):** `git parent $"clone {remote} verify"` + `Assert.True(File.Exists(…/saved.txt))` + `Assert.Equal(branchOid verify "main", branchOid workspace "main")` (same as `saveTracked` test; message in workspace log should contain `gambol: git Save` if asserting locally).
7. **Poll:** `pollSince handle pollAfter []` includes `ActorStart` + `ActorStop` for `seed.focusId` (Save does not require Parse unless ops posted).
8. `CoreMailbox.dispose host` in `finally`.

**Not stubbed:** uses `GithubTransportActor.productionDependencies` → real `WorkspaceGit.commitTracked` / `pushTrackedBranch`.

---

## Test 2 — Git Load through command door (pull + reconciliation + Poll Parse)

**Name:** ``Git Load through postLoadSaveCommand pulls remote and Poll sees Actor and Parse events``

**Steps:**

1. Harness: `seedGitWorkspaceHost ()` with operation `"load"`.
2. **Remote ahead:** on `source`, `commitFile source "from-remote.txt" "pulled" "remote-change"`; `git source "push origin main"` (from ``pullTracked fast forwards``).
3. `let pollAfter =` event id before POST (must be `<` ActorStart/Parse tail).
4. `postLoadSave` with `LoadSaveOperation.Load`, `LoadSavePrePick.Git`.
5. `waitForActor host seed.focusId` → `Some ActorSucceeded`.
6. **Pull proof:** `Assert.True(File.Exists(Path.Combine(workspace, "from-remote.txt")))` and content `"pulled"`.
7. **Poll proof (required by review 3.2):** `pollSince handle pollAfter [ seed.workspaceId ]` (or `[]` if only events matter) must contain:
   - `EventBody.ActorStart` with `start.focusId = seed.focusId`
   - `EventBody.ActorStop(seed.focusId, ActorSucceeded)`
   - At least one `Ev` with `commandName = "Parse"` and `EventBody.Change` (from `LazyLoadReconciliationServer.reconcileWorkspace` via production `continueLoad`)
8. Optional graph assertion: child file node for `from-remote.txt` under workspace (same spirit as `LazyLoadReconciliationServerTests` ``workspace reconcile discovers under workspace root``).
9. Dispose host.

**Not stubbed:** real `WorkspaceGit.pullTrackedBranch` + `LazyLoadReconciliationServer.reconcileWorkspace` inside `productionDependencies`.

---

## Implementation notes

- Prefer **explicit** `LoadSavePrePick.Git` so `PathPick` does not depend on remote timing; still exercise real `LoadSaveRouting.resolvePath` + `remoteExists`.
- **Branch naming:** `trackedWorkspace` uses `main`; Gambol `ensureInit` defaults to `master`. Cloned `home` worktree should match tracked-branch tests (`origin` / `refs/heads/main`) — do not mix an `ensureInit`-only repo without upstream config.
- **Async:** Peer body runs off mailbox; never assert on HTTP response alone — always `waitForActor` then Poll/disk/remote.
- **Refactor for 3.3:** shrink existing ``Git request reaches Peer Actor…`` stub test or split; new E2E facts should call shared `seedGitWorkspaceHost` / `postLoadSave` only.
- **Run gate:** `Skip.IfNot(gitOnPath())`; full suite already skips many git facts the same way.

## Verification command

```bash
dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --filter "FullyQualifiedName~LoadSaveCommandTests"
```

(with git on PATH for the two new `[<SkippableFact>]` cases).
