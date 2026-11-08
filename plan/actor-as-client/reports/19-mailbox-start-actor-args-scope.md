# 19 — Mailbox start Actor args — scope report

Mode: implement. Slice: refactor [CoreMailboxApi.coreMailboxStartActorFromFunction](../../src/Server/Core/CoreMailboxApi.fs) so no function keeps five or more parameters. Branch: `dev`. No commit in this pass.

### Signatures

[ ] **Today — `CoreMailboxApi.coreMailboxStartActorFromFunction`** in `src/Server/Core/CoreMailboxApi.fs`: `(host: MailboxHost) -> (caller: Caller) -> (request: Gambol.Shared.ActorStart) -> (actor: Gambol.Shared.ActorFunction) -> (actorName: string) -> (actorNodeIdOf: Gambol.Shared.ActorStart -> NodeId) -> Async<Result<EventId, string>>`. Six parameters.

[ ] **Today — `ActorOrderFromStart.orderFromStart`** in `src/Server/ActorOrderFromStart.fs`: `(makeActor: NodeId -> NodeId -> Gambol.Shared.ActorFunction) -> (actorName: string) -> (actorNodeIdOf: ActorStart -> NodeId) -> (start: ActorStart) -> ActorOrder`. Four parameters.

[ ] **Today — `MailboxHost.postActorOrder`** in `src/Server/Core/MailboxHost.fs`: `(host: MailboxHost) -> (caller: Caller) -> (order: ActorOrder) -> Async<Result<EventId, string>>`. Three parameters.

[ ] **Today — `Gambol.Shared.ActorOrder`** in `src/Shared/History.fs`: `{ actor: ActorFunction; actorNodeId: NodeId; actorName: string }`.

[ ] **Today — `Gambol.Shared.ActorFunction`** in `src/Shared/History.fs`: `FunctionRun -> ActorChanges -> Async<unit>`.

[ ] **Today — `Gambol.Shared.ActorChanges`** in `src/Shared/History.fs`: `{ postChanges; actorStop; graphAtEvent; loadNode; postNotice; postGraphOnly }`. The uncommitted diff on `dev` adds **notice** and **graphOnly** on `CoreDoor.actorChanges`; it does not add a start bundle type.

[ ] **Proposed type — `FunctionActorPlacement`** in `src/Server/ActorOrderFromStart.fs` beside `orderFromStart`: `{ request: ActorStart; actor: ActorFunction; actorName: string; actorNodeIdOf: ActorStart -> NodeId }`. This is the value every caller of `coreMailboxStartActorFromFunction` already assembles before the mailbox post. It is not logged on the wire. It is not `CoreActorPool.FunctionStart` (the pool supplies `getGraph` at schedule time).

[ ] **Proposed helper — `ActorOrderFromStart.orderFromPlacement`** in the same file: `(placement: FunctionActorPlacement) -> ActorOrder` — one parameter. It calls `orderFromStart (fun _ _ -> placement.actor) placement.actorName placement.actorNodeIdOf placement.request`.

[ ] **Resulting — `CoreMailboxApi.coreMailboxStartActorFromFunction`**: `(host: MailboxHost) -> (caller: Caller) -> (placement: FunctionActorPlacement) -> Async<Result<EventId, string>>`. Three parameters. Body: `orderFromPlacement placement |> MailboxHost.postActorOrder host caller`.

[ ] **Outside the bundle — `host` and `caller`**: Every `CoreMailboxApi` door (`postEvents`, `mbLoadNode`, `startPeerActor`, `coreMailboxStartActorFromFunction`) takes `MailboxHost` and `Caller` separately today. They are not part of `FunctionActorPlacement` because the bundle is only the supplied function start; mailbox identity stays on the existing Core surface. **`writable`** stays on `CoreChangesDoor` only; this path does not use `callIfReady`.

[ ] **Unchanged parameter counts — `GithubTransportActor.start`**: `(host: MailboxHost) -> (caller: Caller) -> (dataDir: string) -> (dependencies: GithubTransportActorDependencies) -> (operation: GithubTransportOperation) -> (request: ActorStart) -> Async<Result<unit, string>>`. Six parameters on that function remain out of this slice. Only its inner call to `coreMailboxStartActorFromFunction` shortens.

### Match sites

[ ] **Constructor — `FunctionActorPlacement` record** — new in `src/Server/ActorOrderFromStart.fs`. Callers set `request`, `actor`, `actorName`, and `actorNodeIdOf`.

[ ] **Match — `orderFromPlacement`** — `src/Server/ActorOrderFromStart.fs`, function `orderFromPlacement`, delegates to `orderFromStart` with `(fun _ _ -> placement.actor)`.

[ ] **Caller — `CoreMailboxApi.coreMailboxStartActorFromFunction`** — `src/Server/Core/CoreMailboxApi.fs`, module `CoreMailboxApi`, uses `orderFromPlacement` then `MailboxHost.postActorOrder`.

[ ] **Caller — `GithubTransportActor.start`** — `src/Server/GithubTransportActor.fs`, module `GithubTransportActor`. After choosing `loadActorFunction` or `saveActorFunction`, builds one `FunctionActorPlacement` with `transportActorName operation` and `(fun item -> item.focusId)` (uncommitted diff on `dev`).

[ ] **Caller — test helper `startRootActor`** — `tests/Server.Tests/ActorCoreChangesDoorTests.fs`, passes `sampleRequest`, the supplied `ActorFunction`, `"root"`, and `(fun start -> start.focusId)`.

[ ] **Caller — ``coreMailboxStartActorFromFunction places StartActor from the selector``** — `tests/Server.Tests/ActorOrderFromStartTests.fs`, inline six arguments become one record plus `host` and `testCaller`.

[ ] **Indirect caller — load-save routing** — `tests/Server.Tests/LoadSaveCommandTests.fs`, functions `productionRouter` and `routeMatrixRouter`, call `GithubTransportActor.start` for `LoadSavePath.Git`; no direct reference to `coreMailboxStartActorFromFunction`.

[ ] **Indirect caller — git transport tests** — `tests/Server.Tests/GithubTransportActorTests.fs`, calls `GithubTransportActor.start` with the expanded argument list from the uncommitted diff.

[ ] **No change — `CoreMailboxActors.queueAdmittedTest`** — `src/Server/Core/CoreMailboxActors.fs`, still calls `ActorOrderFromStart.orderFromStart` with `TestActor.functionActorAt` and posts through `MailboxHost.postActorOrder` (factory shape, not a pre-built `actor`).

[ ] **No change — `TestBackend.postTestActorOrder`** — `tests/Server.Tests/TestBackend.fs`, builds `ActorOrder` via `orderFromStart` and calls `MailboxHost.postActorOrder` directly.

[ ] **No change — deprecated `CoreMailboxApi.coreMailboxStartActor`** — three parameters; posts `Order.StartActorDeprecated`.

### Existing tests

[ ] **``orderFromStart sets actorNodeId from the selector and the actor name``** — `tests/Server.Tests/ActorOrderFromStartTests.fs`. Checks `ActorOrderFromStart.orderFromStart` sets `actorNodeId`, `actorName`, and the factory-produced `actor`. Does not call `coreMailboxStartActorFromFunction`.

[ ] **``coreMailboxStartActorFromFunction places StartActor from the selector``** — `tests/Server.Tests/ActorOrderFromStartTests.fs`. Posts through `CoreMailboxApi.coreMailboxStartActorFromFunction` with a custom `actorNodeIdOf` on `runDefinitionId`. Asserts one `Order.StartActor` in history, no deprecated `ActorStart` body. **Primary seam for the new three-parameter call shape.**

[ ] **``Actor getEventId surfaces persist error instead of Revision 0``** — `tests/Server.Tests/ActorCoreChangesDoorTests.fs`. Uses `startRootActor` → `coreMailboxStartActorFromFunction`.

[ ] **``Actor postChange on scheduled handle reaches persist``** — `tests/Server.Tests/ActorCoreChangesDoorTests.fs`. Same `startRootActor` helper.

[ ] **``every Git choice starts the Server Peer Actor``** — `tests/Server.Tests/LoadSaveCommandTests.fs`. Git path via `GithubTransportActor.start`; asserts `hasStartedActor` on history (uncommitted assertion rename from `hasActorStart`).

[ ] **``routed Git Load pulls the tracked branch``** — `tests/Server.Tests/LoadSaveCommandTests.fs`. Same Git start path and `hasStartedActor` check.

[ ] **Git Load and Save Facts** — `tests/Server.Tests/GithubTransportActorTests.fs` (`git Load tells the client the actor completed`, `git Load gates pull from subnode Focus`, `git Save gates commit then releases before push`, `Load and Save return the same conflict reject shape`, and others). All call `GithubTransportActor.start` on the uncommitted six-parameter surface; they observe placement indirectly through actor lifecycle and history.

[ ] **No test file** names `FunctionActorPlacement` or `orderFromPlacement` today.

### Copy-from

[ ] **`ActorOrderFromStart.orderFromStart`** — `src/Server/ActorOrderFromStart.fs`. Same field wiring as today inside `coreMailboxStartActorFromFunction` (`makeActor` fixed to `(fun _ _ -> actor)`).

[ ] **`MailboxHost.postActorOrder`** — `src/Server/Core/MailboxHost.fs`. Final enqueue step; unchanged signature.

[ ] **`TestBackend.postTestActorOrder`** — `tests/Server.Tests/TestBackend.fs`. Pattern: build `ActorOrder`, then `postActorOrder host caller order`. After this slice, `coreMailboxStartActorFromFunction` is the shared wrapper for callers that already hold a built `ActorFunction`.

[ ] **Extend `ActorOrderFromStart.fs`, not a private record in `CoreMailboxApi`**. The uncommitted [git Load and Save actor functions](git-load-and-save-actor-functions.md) report already groups `request`, `actor`, actor name, and `actorNodeIdOf` at `GithubTransportActor.start`; it does not name a type yet. **`FunctionActorPlacement`** is that missing name.

[ ] **Do not reuse `CoreActorPool.FunctionStart`** — `src/Server/Core/CoreActorPool.fs`. Pool start carries `getGraph` and `actorNodeId`; mailbox placement from the client carries `ActorStart` and a selector for cancel node.

### write-ticket

[ ] **Source file — `src/Server/ActorOrderFromStart.fs`** — add `FunctionActorPlacement` and `orderFromPlacement`.

[ ] **Source file — `src/Server/Core/CoreMailboxApi.fs`** — shrink `coreMailboxStartActorFromFunction` to three parameters.

[ ] **Source file — `src/Server/GithubTransportActor.fs`** — build `FunctionActorPlacement` in `start` (already touched on `dev`).

[ ] **Test file — `tests/Server.Tests/ActorOrderFromStartTests.fs`** — update the `coreMailboxStartActorFromFunction` Fact and optionally add a unit Fact for `orderFromPlacement` beside the existing `orderFromStart` Fact.

[ ] **Test file — `tests/Server.Tests/ActorCoreChangesDoorTests.fs`** — update `startRootActor`.

[ ] **No new test file.** `tests/Server.Tests/LoadSaveCommandTests.fs` and `tests/Server.Tests/GithubTransportActorTests.fs` need no edit if `GithubTransportActor.start` signature stays the same.

### implement

[ ] **Slice claim — parameter limit** — Code today violates the slice rule: `coreMailboxStartActorFromFunction` has six parameters. The implementer adds `FunctionActorPlacement` and lands at three parameters on that function.

[ ] **Slice claim — broad type** — Code today passes four related values as separate arguments at every call site. No shared bundle type exists in the uncommitted diff. `FunctionActorPlacement` is the type callers already conceptually need; `GithubTransportActor.start` is the production caller on `dev`.

[ ] **Slice claim — join design in progress** — Uncommitted work on `dev` routes git Load and Save through `coreMailboxStartActorFromFunction` and expands `ActorChanges` on `CoreDoor.actorChanges`. Placement belongs in `ActorOrderFromStart` next to `ActorOrder` construction, not as a one-off parameter record visible only in `CoreMailboxApi`.

[ ] **No new test required** — ``coreMailboxStartActorFromFunction places StartActor from the selector`` already exercises the public API and `Order.StartActor` history. Recompile that Fact after the signature change. Optional: ``orderFromPlacement builds the same order as orderFromStart with a fixed actor`` in `ActorOrderFromStartTests.fs` if the implementer wants a direct unit check on the new helper.

[ ] **Check** — `scripts/agent-test.sh --filter FullyQualifiedName~ActorOrderFromStartTests|FullyQualifiedName~ActorCoreChangesDoorTests|FullyQualifiedName~GithubTransportActorTests|FullyQualifiedName~LoadSaveCommandTests server` (same filter family as [git Load and Save actor functions](git-load-and-save-actor-functions.md)).
