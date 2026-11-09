# 2026-11-08 live-call refactor context

Alan spoke these definitions on a live call. The relay date is 2026-11-08. This note records them. The source on `staging` stays at `fcc01cd07e9efe4c78c2871172eecaf8bfc6183b`.

## 1. Spoken definitions

[ ] In History, the actor start's third parameter is Run definition id.
[ ] ActorStart's third parameter is Run definition id.
[ ] StartActor was renamed to StartActor deprecated.
[ ] ActorLoad was renamed to StartActor.
[ ] Load-save moved onto the new StartActor.
[ ] ActorOrder's second parameter is Actor node id.
[ ] FirstFactorStart is a struct.
[ ] Alan is separating Load and Save at the head of the GitHub transport actor file, with shared functionality below.
[ ] That line means a Load function and a Save function.
[ ] Common functionality is called by both.
[ ] Alan: "We DO NOT want a single function that is Load or Save."
[ ] Ruled out: one combined Load/Save switch function.
[ ] Ruled out: both operations calling a function that does both.
[ ] Correct the GitHub Save path behavior only.
[ ] The other path is isolated at a higher level.
[ ] Save should behave similarly to Load.

## 2. Checkout

Verified on `staging` `fcc01cd07e9efe4c78c2871172eecaf8bfc6183b`.

### 1. src/Shared/History.fs

[ ] `ActorStart` is a record. The fields in order are `zoomId`, `focusId`, `runDefinitionId`, `graphIds`, `eventId`.
[ ] The third field is `runDefinitionId`.
[ ] `ActorOrder` is a record. The fields in order are `actor`, `actorNodeId`, `actorName`.
[ ] The second field is `actorNodeId`.
[ ] `Order.StartActorDeprecated` carries `ActorStart`.
[ ] `Order.StartActor` carries `ActorOrder`.
[ ] `EventBody.ActorStart` wraps `Order.StartActorDeprecated`.

### 2. Renames in git

[ ] `d895b5cf` "renaming members of Order" renames `Order.StartActor of ActorStart` to `Order.StartActorDeprecated of ActorStart`.
[ ] The same commit renames `Order.ActorOrderCase of ActorOrder` to `Order.StartActor of ActorOrder`.
[ ] `e09d7530` "rename fields in ActorStart and ActorOrder" renames `ActorStart.actorNodeId` to `runDefinitionId`.
[ ] The same commit renames `ActorOrder.focusId` to `actorNodeId`.
[ ] `git log -S ActorLoad -- src/Shared/History.fs` returns no commit.
[ ] `git log -S FirstFactorStart` returns no commit.
[ ] `ActorStart` uses record braces. That type carries no `[<Struct>]` attribute.

### 3. GitHub transport

[ ] The file is `src/Server/GithubTransportActor.fs`. The module is `GithubTransportActor`.
[ ] `start` places `Order.StartActor` through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[ ] `actorNodeIdOf` on that placement reads `focusId`.
[ ] `601dc957` "Moving Save onto ActorStart" is the commit that moves that start.
[ ] `fcc01cd0` "refactoring Load from Save" splits `runStep` into `runLoadStep` and `runSaveStep`.
[ ] `loadActorFunction` and `saveActorFunction` both call `runActorFunction`.
[ ] `runActorFunction` calls `runBody`. `runBody` switches on `GithubTransportOperation` between `runLoadStep` and `runSaveStep`.
[ ] That switch is the combined Load/Save function the spoken aim rules out. The aim is still in flight on this commit.
[ ] `runLoadStep` calls `LoadSubject.enclosingSubject`. A Workspace pulls. A Directory or a File calls `loadNode`.
[ ] `runSaveStep` calls `saveTracked` on the workspace root: commit, then push.
[ ] Desk Load and Save enter `CoreMailboxLoad.dispatchStartLoadSaveCommand`. `logDeskOrder` logs `Order.StartActor`.

## 3. Spoken name beside the identifier

[ ] Spoken "StartActor deprecated" is the identifier `StartActorDeprecated`.
[ ] Spoken "ActorLoad" stays "ActorLoad". The identifier renamed in `d895b5cf` is `ActorOrderCase`.
[ ] Spoken "FirstFactorStart" stays "FirstFactorStart". This checkout has a record named `ActorStart`.

## 4. Place

[ ] [Actor as client](../project.md) points here.
[ ] [StartActorDeprecated gap](startactor-deprecated-gap.md) stays the earlier gap report.
