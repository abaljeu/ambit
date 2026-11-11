# 23 — Run the Load actor for Git and Neither
**Status**: coded
One job: A Load starts the Load actor. The Git arm pulls. The Neither arm is empty. The actor then reconciles the subject, and it posts its start and its stop.

```fsharp
Load x =
    ( | Git x -> pull x | Desktop_mapped -> Webdav x | Neither -> do_nothing )
    reconcile x
```

A directory with no desktop access and no upload is the Neither case. `GithubTransportActor.start` in `src/Server/GithubTransportActor.fs` posts `Order.StartActor` for `loadActorFunction`. Pull request 589 removed `CoreMailboxLoad.logDeskOrder`, which appended `Order.StartActor` with `EventJson.stubActor` and an empty `actorName` and posted no stop. This ticket replaces that removal. The Neither case runs the Load actor. Desktop_mapped waits for the WebDAV design on [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md). A later Save actor is the follow-on: Git commit, pull, and push under the existing Save rules; Desktop_mapped WebDAV after that design; an empty Neither arm.

A plain Load enters the Load actor. It does not take the Desk path. A work tree with a Git remote takes the Git arm: pull, then reconcile. A Workspace, a Directory, or a File with no Git remote takes the Neither arm: no transport, then reconcile, then stop. A missing work tree takes that same arm. An explicit Git Load of a Directory that is not a repository takes that same Neither arm. A git error other than not a repository fails the Load actor. An object outside a workspace work tree takes the Neither arm and does not pull or push.

[ ] `loadActorFunction` builds the Load `ActorFunction`. `GithubTransportActor.start` calls it for Load and posts through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[ ] `finishActor` calls `changes.actorStop` after the Load body.
[ ] `runLoad` pulls through `pullTracked`. `finishLoadOn` calls `LazyLoadReconciliationServer.reconcileFromChanges`, then `loadNode`.
[ ] `PathPick.resolveCommand` in `src/Shared/PathPick.fs` sends a plain Load to `LoadSavePath.Git`, which starts the Load actor. It does not keep that Load on `LoadSavePath.Desk`. `DeskLoadPush.runLoadSave` does not coerce it back.
[ ] `CoreMailboxApi.startLoadSaveCommand` in `src/Server/Core/CoreMailboxApi.fs` is the send for that desk path. `CoreMailboxLoad.dispatchStartLoadSaveCommand` runs `DeskLoadPush.runLoadSave`.
[ ] `saveActorFunction` is the Git Save body. This ticket leaves it for the Save-actor follow-on.

Type:

[ ] coding

Blockers:

[ ] [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)

## Inputs

### Working

[ ] `GithubTransportActor.start` matches `GithubTransportOperation.Load` to `loadActorFunction` and `GithubTransportOperation.Save` to `saveActorFunction`.
[ ] `runLoadChoice` sends a Workspace, a Directory, and a File through the work-tree remote check. A remote pulls. No work tree, no remote, and not a repository are Neither. Any other git error fails the actor.
[ ] The Desk arm of `RouteRegistration.registerStateRoutes` calls `CoreMailboxApi.startLoadSaveCommand`.
[ ] `CoreActorPool.startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.

### Reference

[ ] [Arch](../../arch.md) **Load and save**.

## Do not load

[ ] The Desktop_mapped WebDAV branch. Its design is open on 20 — Move Desk Load and Desk Save.
[ ] The Save actor follow-on.
[ ] `Order.StartActorDeprecated`.

## Process

1. [x] If an input is missing, stop and name that input.
2. [x] A Git Load and a Neither Load call `GithubTransportActor.start` with `loadActorFunction`.
3. [x] The Git arm keeps the existing pull in `runLoad`.
4. [x] The Neither arm is an empty step.
5. [x] After that arm, the actor reconciles the subject. The Git pull keeps `finishLoadOn`.
6. [x] The actor posts `Order.StartActor` at start and posts its stop through `finishActor`.
7. [x] A plain Load reaches `GithubTransportActor.start`. A remote takes the Git arm. No Git takes the Neither arm. The Load does not stay on `LoadSavePath.Desk`.
8. [x] Leave the Desktop_mapped arm unused.
9. [x] Leave `saveActorFunction` unchanged.

## Outputs

[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Shared/PathPick.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoad.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/23-run-the-load-actor-for-git-and-neither-stop.md`, then ask.
[ ] Stop when this ticket builds WebDAV or the Save actor.
[ ] Stop when the Neither case posts `Order.StartActor` with an empty `actorName` and posts no stop.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `LoadSaveCommandTests`
  - `tests/Server.Tests`: `ParseThreadLoadTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/23-run-the-load-actor-for-git-and-neither-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
