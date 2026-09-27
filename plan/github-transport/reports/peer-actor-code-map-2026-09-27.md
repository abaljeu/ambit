# Peer Actor code map for [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md)

Date: 2026-09-27
Project: [github-transport](plan/github-transport/project.md)
Ticket: [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md)
Blocked by: [12 — Run the Workspace git tracked-branch round-trip](plan/github-transport/issues/12-workspace-git-tracked-branch-round-trip.md) (Status `done`)
Architecture: [github-transport architecture](plan/github-transport/arch.md) module **Peer Actor**, seam **Peer Actor git Load/Save**
This file is a write-once code map. It does not change product locks.

## 1. Scope

1. Map the live types, functions, and files that [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) must use.
2. Name the smallest seams that implement git Load and git Save as a Server Peer Actor.
3. Name ambiguities. Do not pick a silent product choice.

## 2. What exists today

1. There is no [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs). The architecture names that file. The spoken name is Server Peer Actor. It is not a new Kind.
2. Registered actors are only `test` and `ai` in [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) `CreateBoot`.
3. Person Load and Save do not reach the mailbox. Load is [src/Client/UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs) `loadOp`. Save is [src/Client/UpdateSave.fs](src/Client/UpdateSave.fs) `saveOp` → Server `GitSave.commitAll` on the whole DataDir. Run is [src/Client/Commands.fs](src/Client/Commands.fs) `execRunOp` → `CommandRequest.tryStart` → `SubmitCommand` → POST `/ambit/command`.
4. POST `/ambit/load` is Fetch + Poll only ([src/Server/Api.fs](src/Server/Api.fs) `postLoad`). It is not git Load.

## 3. CoreActorPool and ActorFn invocation

File: [src/Server/Core/CoreActorPool.fs](src/Server/Core/CoreActorPool.fs)

1. **ActorName** — `ActorName of string`. Lookup key in `defs`.
2. **ActorInput** — `{ graph; zoomId; focusId; commandId; sessionId; secret }`. `secret` is the live Actor Credential. It is not a GitHub token.
3. **ActorFn** — `ActorInput -> CoreChanges -> Async<unit>`.
4. **CoreActorPool** — `{ register; startActor; schedule; isLive; admit; drop; finish; liveFocusIds; getFocusId; trySecretForFocus; deliver; takeInbox }`.
5. **create** — `CoreActorPool.create : unit -> CoreActorPool`.
6. **register** — `register (ActorName name) actorFn` stores the function in `defs`.
7. **startActor** — `ActorStart -> (unit -> Graph) -> Result<Credential, string>`. It admits (non-empty `graphIds`, Focus free, commandId free), builds a subgraph from `graphIds`, names the actor from the command node, mint a secret, and stores a live row with `pending`.
8. **Actor name from command node** — `CommandRequest.actorNameFromText` if text starts with `?`; else first CSS class `actor-*`; else trimmed lowercase text. ROOT text is `"ROOT"`, so tests register `ActorName "root"`.
9. **schedule** — takes the pending body and `Async.Start(actorFn input coreChanges, cancel.Token)`. This is the invocation hop.
10. **finish / drop** — cancel the token and remove the live row. [4.1.1 — Keep only live Actor state](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) matches this live-row lifetime.
11. **deliver / takeInbox** — session inbox for bot text. This is not the Load/Save start door.

Composition: [src/Server/Core/CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) `CoreBoot.Actors : (ActorName * ActorFn) list`. `create` registers each pair, then starts the mailbox. [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) injects closed-over config into `RunAgentActor.actorFn`. The Peer Actor must take DataDir and git operations the same way. Core does not open WorkspaceGit.

Reference actors:

1. [src/Server/TestActor.fs](src/Server/TestActor.fs) `actorFn` — body then `coreChanges.asCaller(actorCaller).actorStop result`.
2. [src/Server/RunAgentActor.fs](src/Server/RunAgentActor.fs) `actorFn keys repos grok : ActorFn` — same stop shape; dependencies in the closure.

Shared start payload: [src/Shared/History.fs](src/Shared/History.fs) `ActorStart = { zoomId; focusId; commandId; graphIds; eventId }`. Constructor: [src/Shared/CommandRequest.fs](src/Shared/CommandRequest.fs) `actorStart`. Product Run scan: `tryStart` (requires a `?` command on the owner path). [07 — Actor start door](plan/github-transport/issues/07-actor-start-door.md) forbids Run and `?git`.

## 4. Server mailbox command delivery

Files: [src/Server/Core/CoreMsg.fs](src/Server/Core/CoreMsg.fs), [src/Server/Core/CoreMailbox.fs](src/Server/Core/CoreMailbox.fs), [src/Server/Core/CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs), [src/Server/Core/CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs), [src/Server/Api.fs](src/Server/Api.fs), [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs)

1. **CoreMsg** has `StartActor of caller * ActorStart * reply`. There is no Load or Save case.
2. **CoreMailbox.startActor** posts that message. Public door for tests.
3. **dispatchStartActor** admits the Browser Caller, calls `pool.startActor`, appends `EventBody.ActorStart` (`commandName` is empty), then `pool.schedule secret (make caller)`.
4. HTTP: POST `/ambit/command` → `Api.postCommand` → `EventJson.decodeStartRequest` → `CoreMailbox.startActor`. Wire: [src/Shared/EventJson.fs](src/Shared/EventJson.fs) `encodeStartRequest` / `decodeStartRequest`.
5. Client Run: [src/Client/App.fs](src/Client/App.fs) `runSubmitCommand` POSTs that body. This is the door [07 — Actor start door](plan/github-transport/issues/07-actor-start-door.md) rejects for git Load/Save.
6. Actor posts use [src/Server/Core/CoreChanges.fs](src/Server/Core/CoreChanges.fs) `CoreChanges` (`postEvents`, `postGraphOnly`, `actorStop`, `asCaller`). Failures are `ActorFailed of string` via `actorStop`.

## 5. Focus resolution to Workspace work tree

[4.2.2 — Resolve the work tree from Focus](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md): Focus identifies the Workspace work tree. A subnode does not narrow git.

Existing functions:

1. [src/Shared/GraphQuery.fs](src/Shared/GraphQuery.fs) `enclosing` — owner-chain walk, inclusive of the start node. `enclosingWorkspace` — first `Special Workspace` (named Workspace or ROOT).
2. [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) `enclosingWorkspaceName` — same walk, but skips ROOT and unnamed Workspace. Use this when the work tree is a named Workspace folder.
3. [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) `artifactDirectoryRelative` — named Workspace → `name + "/"`; ROOT id → `None`.
4. [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) `workspaceRootFor dataDir graph nodeId` — `enclosingWorkspace` then `artifactDirectoryRelative` then `resolveUnderDataDir`. Named Workspace `"home"` and any descendant resolve to `{DataDir}/home`. Subnode Focus does not add a pathspec. This is the Persist work-tree function.
5. [src/Shared/WorkspaceSyncScope.fs](src/Shared/WorkspaceSyncScope.fs) `tryFromFocus` — desk Upload/Download scope. It can be Directory or File. Do not use it to narrow git. Label + DataDir is enough for the whole tree.

DataDir: [src/Server/DataDir.fs](src/Server/DataDir.fs). ActorFn has no DataDir field. Close DataDir over the ActorFn at `CreateBoot`, as `RunAgentActor` closes keys.

ROOT / SYSTEM / TRASH: `workspaceRootFor` is `None` for ROOT (and for Focus whose enclosing Workspace is ROOT). SYSTEM and TRASH are Directory-kind; `tryFromFocus` treats them as workspace-kind desk scopes. [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) does not say how git Load/Save behaves on those Focus values.

## 6. WorkspaceGit pull, commit, push, and the work-tree gate from [12 — Run the Workspace git tracked-branch round-trip](plan/github-transport/issues/12-workspace-git-tracked-branch-round-trip.md)

File: [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs). Process: [src/Server/GitSave.fs](src/Server/GitSave.fs) `runGit` / `commitAll`, [src/Shared/dotnet/GitRun.fs](src/Shared/dotnet/GitRun.fs). Tests: [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs).

Types and functions for [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md):

1. **WorkspaceTrackedBranch** — `{ branch; remote; upstream }`.
2. **remoteExists** — `workspaceRoot -> Result<bool, string>`. PathPick uses this later ([11 — Pick git or desk for plain Load and Save](plan/github-transport/issues/11-pick-git-or-desk-for-plain-load-save.md) is done). [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) does not call PathPick.
3. **trackedBranch** — current branch + upstream. No Server branch map.
4. **pullTracked** — `withWorkTreeGate` then `git pull --ff-only --no-rebase {remote} {upstream}`. Whole tree. No pathspec. Conflict → `Error $"Git conflict: {path}"`. Other git text → `condenseGitError` (≤400 chars, key line plus two).
5. **saveTracked** — `withWorkTreeGate` around `GitSave.commitAll` of all work-tree edits, then **push outside the gate** `git push {remote} HEAD:{upstream}`. No Persist call. Same conflict / condensed error shape as pull.
6. **withWorkTreeGate** — one `SemaphoreSlim(1,1)` per normalized root (`ConcurrentDictionary`). Not reentrant. Second caller waits. Persist writes call this through [src/Server/DocumentPersistWrite.fs](src/Server/DocumentPersistWrite.fs) and [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) `withWorkTreeGates`.
7. **No GitHub credential argument** on any of these functions. Host git loads credentials.

Gate deadlock: `SemaphoreSlim` is not recursive. If the Peer Actor calls `withWorkTreeGate` and then `pullTracked` / `saveTracked`, the same thread waits on itself. [12 — Run the Workspace git tracked-branch round-trip](plan/github-transport/issues/12-workspace-git-tracked-branch-round-trip.md) already acquires the gate inside `pullTracked` and inside the commit half of `saveTracked`. [4.2.9 — Acquire and release the work-tree gate](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) is already true when the Actor calls those two functions and does not wrap them again.

Do not use [src/Server/Api.fs](src/Server/Api.fs) `gitSave` or [src/Client/UpdateSave.fs](src/Client/UpdateSave.fs) `saveOp` for git Save. Those commit the Server DataDir as one repo. They do not push a Workspace tracked branch.

## 7. Load continuation into Parse / graph-push

Today’s desk/web Load after files land ([src/Client/UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs), [src/Client/UpdateImport.fs](src/Client/UpdateImport.fs), [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs), [src/Client/App.fs](src/Client/App.fs)):

1. **parseFileOp** → `Effect.ContinueParseFile` → POST `/ambit/parse` ([src/Server/Api.fs](src/Server/Api.fs) `postParseFile` → [src/Server/DocumentPersistWrite.fs](src/Server/DocumentPersistWrite.fs) `planParseFile` → `GraphOnlyChangePost.mint "Parse"` → `postGraphOnly`) → `tryStartLoadFetch` (Fetch + Poll, POST `/ambit/load`).
2. **ContinueDirectoryReconcile** → POST `/ambit/workspace/reconciliation/directory` ([src/Server/LazyLoadReconciliationServer.fs](src/Server/LazyLoadReconciliationServer.fs)) → poll + path-sync.
3. **WorkspaceUpload.plan** chooses DesktopPush, ReconcileServerDisk, or ParseServerDisk from Focus. Git pull is not in that plan.

Architecture story **Load keeps Parse**: after a whole-tree pull, keep this coupling. Do not expand [06 — Selection-scoped Parse after whole-tree git Load](plan/github-transport/issues/06-selection-scoped-parse-after-whole-tree-git-load.md). [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) also owns “preserve Load completion” on the Command surface.

## 8. Server.Tests patterns and fsproj registration

Project: [tests/Server.Tests/Gambol.Server.Tests.fsproj](tests/Server.Tests/Gambol.Server.Tests.fsproj). Helpers: [tests/Server.Tests/TestBackend.fs](tests/Server.Tests/TestBackend.fs) (`newTempDir`, `admittedCredentials`, `testCaller`).

Patterns to copy:

1. **Mailbox + pool host** — [tests/Server.Tests/TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) and [tests/Server.Tests/CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs): `CoreActorPool.create`, `register`, `CoreMailbox.host pool (FileAgent.persist ...) admittedCredentials`, `CoreMailbox.startActor`, wait for `EventBody.ActorStop`, `pool.liveFocusIds` drop, `CoreMailbox.dispose` in `finally`.
2. **Stub the whole pool record** — [tests/Server.Tests/CoreMsgActorCasesTests.fs](tests/Server.Tests/CoreMsgActorCasesTests.fs) `recordingPool`.
3. **ActorFn closure** — [tests/Server.Tests/ActorCoreChangesDoorTests.fs](tests/Server.Tests/ActorCoreChangesDoorTests.fs) `startRootActor`.
4. **HTTP StartActor decode** — [tests/Server.Tests/ApiPostCommandTests.fs](tests/Server.Tests/ApiPostCommandTests.fs) (Run door; not the git door).
5. **Workspace graph fixture** — [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) `graphWithWorkspace` (named Workspace under Workspaces; Persist write uses `{dataDir}/{label}`).
6. **xUnit** — `[<Fact>]` for in-process stubs. `[<SkippableFact>]` + `gitOnPath` only when a real git process is required. [4.3.2 — Depend on WorkspaceGit](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) stubs WorkspaceGit, so new tests stay `[<Fact>]`.
7. **Compile include** — add the production file in [src/Server/Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj) after [src/Server/RunAgentActor.fs](src/Server/RunAgentActor.fs). Add the test file in [tests/Server.Tests/Gambol.Server.Tests.fsproj](tests/Server.Tests/Gambol.Server.Tests.fsproj) after [tests/Server.Tests/TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) (or beside [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) if the file is git-host only). F# compile order is the registration.

## 9. Recommended minimum implementation seams

Keep [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) on Server. Leave PathPick, git/desk pre-pick, and App thinness to [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) and [15 — Keep the App outside Peer Actor hosting](plan/github-transport/issues/15-keep-app-outside-peer-actor-hosting.md), except where a mailbox request is required to invoke the pool.

1. **Injected git ops record** (new, small) — `{ pullTracked: string -> Result<string, string>; saveTracked: string -> string -> string option -> Result<string, string> }`. Production closes `WorkspaceGit.pullTracked` and `WorkspaceGit.saveTracked`. Tests stub. WorkspaceGit is a module, not an interface; without this record, [4.3.2 — Depend on WorkspaceGit](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) has no seam. Do not pass an Ambit credential.
2. **GithubTransportActor** — new [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs). `actorFn dataDir gitOps : ActorFn`. Resolve work tree with `DocumentPersistPath.workspaceRootFor dataDir input.graph input.focusId`. Load → `gitOps.pullTracked`. Save → `gitOps.saveTracked` (commit then push already). Map `Error` to `ActorFailed` with the same string WorkspaceGit returned (conflict path and condensed git). Always `actorStop`. Do not call `withWorkTreeGate`. Do not call Persist. Do not call GitRun with a token.
3. **Mailbox start that is not Run / `?git`** — keep `CoreMailbox.startActor` → `CoreMsg.StartActor` → `pool.startActor` + `schedule` as the pool lifecycle. Add a start that supplies `ActorName` (or a dedicated CoreMsg) so name lookup does not read `?git` or Run scan. Register that name in `CreateBoot` next to `test` and `ai`. Same door for Load and Save; distinguish action on the request (commandId / a Load|Save tag), not with two pool types.
4. **Live row** — use existing `startActor` admit + `finish` on stop. [4.1.1 — Keep only live Actor state](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) and [4.3.1 — Use the actor pool](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) are then the existing pool.
5. **Parse continuation (v1 boundary)** — after successful git Load pull, do not invent a new Parse actor. Either (a) the Actor stops and [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) continues `parseFileOp` / directory reconcile / Fetch+Poll from today’s `loadOp`, or (b) the Actor, on Load only, calls the existing Server parse/reconcile functions (`DocumentPersistWrite.planParseFile` / directory reconciliation) then Fetch stays Client. Prove (a) or (b) with tests. Do not implement [06 — Selection-scoped Parse after whole-tree git Load](plan/github-transport/issues/06-selection-scoped-parse-after-whole-tree-git-load.md).
6. **Tests** — new Server.Tests file: register the Peer Actor, stub git ops, `CoreMailbox.startActor` (or the new named door) with Focus on a named Workspace from `graphWithWorkspace`, assert Load calls `pullTracked` once with `{dataDir}/{label}`, Save calls `saveTracked` once, stub error text is the `ActorFailed` message, Save stub is not paired with Persist, live row drops after stop, Focus on a File under that Workspace still uses the Workspace root path.

## 10. Ambiguity

1. **Door shape vs 14 — Route Load and Save by path pre-pick** — [4.2.1 — Use the Load/Save start door](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) and [1.2.5 — Use the command-request door](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) both say Load/Save command request → mailbox → pool. [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) can prove the Server Actor from `CoreMailbox` without Client HTTP. [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) can wire person Commands and PathPick. If [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) also changes `/ambit/command` or `loadOp`, it overlaps [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md).
2. **How the pool selects the Peer Actor** — `startActor` names the actor from command-node text / `actor-*` / `?name`. Load and Save have no command node. Inventing a `?git` node is forbidden. A fixed `ActorName` on a new start path is the gap. The registered string is not locked (`github`, `git`, or another).
3. **Load vs Save on one ActorFn** — One Actor shape is locked. The discriminator (extra request field, commandId node text `load`/`save`, or two registered names that share one function) is not locked.
4. **Who continues Parse** — Architecture points at today’s Client `loadOp` hops. [4.3.4 — Preserve Load completion](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) asks Server.Tests to prove git Load continues to Parse / graph-push. Those hops are Client effects plus `/ambit/parse` and `/ambit/workspace/reconciliation/directory`. Server-only tests cannot POST those Client effects. Choose: Actor calls existing Server parse/reconcile, or [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) only proves pull + `ActorStop` and [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) proves the Client hops.
5. **Second gate wrap** — [4.2.9 — Acquire and release the work-tree gate](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) reads as Actor-level acquire/release. [12 — Run the Workspace git tracked-branch round-trip](plan/github-transport/issues/12-workspace-git-tracked-branch-round-trip.md) already gates inside WorkspaceGit. A second wrap deadlocks. Treat [4.2.9 — Acquire and release the work-tree gate](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) as “call `pullTracked` / `saveTracked` (they gate)” unless Alan asks for an Actor-visible gate hook.
6. **ROOT, SYSTEM, TRASH Focus** — `workspaceRootFor` is `None` for ROOT. SYSTEM/TRASH are not named Workspace folders. Reject, map to DataDir root, or out of scope: not locked.
7. **HTTP body for a load/save command request** — No type exists. Reuse `ActorStart` JSON, or add a Load|Save request. Unsettled in architecture is empty, but the wire is still unspecified.
8. **saveTracked clientHint** — Third argument is `string option` for the commit message. The Actor may pass `None` or a Browser client hint. Not locked.
9. **POST `/ambit/load` name clash** — That route is Fetch + Poll. Git Load must not be implemented by extending `Api.postLoad` unless architecture is changed.

## 11. Out of this ticket

1. [src/Shared/PathPick.fs](src/Shared/PathPick.fs) `choose` — [11 — Pick git or desk for plain Load and Save](plan/github-transport/issues/11-pick-git-or-desk-for-plain-load-save.md) is done; wiring is [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md).
2. Desk WebDAV / `loadOp` desk transit — keep; do not replace.
3. App GitRun / App Actor host — [15 — Keep the App outside Peer Actor hosting](plan/github-transport/issues/15-keep-app-outside-peer-actor-hosting.md).
4. Selection-scoped Parse after whole-tree pull — [06 — Selection-scoped Parse after whole-tree git Load](plan/github-transport/issues/06-selection-scoped-parse-after-whole-tree-git-load.md), not v1.
