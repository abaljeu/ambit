# 23 — Run the Load actor for Git and Neither
**Status**: build
One job: A Load starts the Load actor. The Git arm pulls. The Neither arm is empty. The actor then reconciles the subject, and it posts its start and its stop.

```fsharp
Load x =
    ( | Git x -> pull x | Desktop_mapped -> Webdav x | Neither -> do_nothing )
    reconcile x
```

A directory with no desktop access and no upload is the Neither case. `GithubTransportActor.start` in `src/Server/GithubTransportActor.fs` posts `Order.StartActor` for `loadActorFunction`. Pull request 589 removed `CoreMailboxLoad.logDeskOrder`, which appended `Order.StartActor` with `EventJson.stubActor` and an empty `actorName` and posted no stop. This ticket replaces that removal. The Neither case runs the Load actor. Desktop_mapped waits for the WebDAV design on [20 — Move Desk Load and Desk Save](../20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md). A later Save actor is the follow-on: Git commit, pull, and push under the existing Save rules; Desktop_mapped WebDAV after that design; an empty Neither arm.

[ ] `loadActorFunction` builds the Load `ActorFunction`. `GithubTransportActor.start` calls it for Load and posts through `CoreMailboxApi.coreMailboxStartActorFromFunction`.
[ ] `finishActor` calls `changes.actorStop` after the Load body.
[ ] `runLoad` pulls through `pullTracked`. `finishLoadOn` calls `LazyLoadReconciliationServer.reconcileFromChanges`, then `loadNode`.
[ ] `PathPick.plainLoadStaysDesk` in `src/Shared/PathPick.fs` keeps a plain Load of a Directory, a File, or a missing subject on `LoadSavePath.Desk`. `PathPick.resolveCommand` and `PathPick.effectiveLoadPath` use that result.
[ ] `CoreMailboxApi.startLoadSaveCommand` in `src/Server/Core/CoreMailboxApi.fs` is the send for that desk path. `CoreMailboxLoad.dispatchStartLoadSaveCommand` runs `DeskLoadPush.runLoadSave`.
[ ] `saveActorFunction` is the Git Save body. This ticket leaves it for the Save-actor follow-on.

Type:

[ ] coding

Blockers:

[ ] [10 — Start git Load](../10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)

## Inputs

### Working

[ ] `GithubTransportActor.start` matches `GithubTransportOperation.Load` to `loadActorFunction` and `GithubTransportOperation.Save` to `saveActorFunction`.
[ ] `runLoadChoice` pulls for `LoadSubject.Workspace` and calls `loadFocus` for a Directory or a File.
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
7. [x] A plain Load of the Neither case reaches `GithubTransportActor.start`. `PathPick.plainLoadStaysDesk` is the function that currently keeps that Load on `LoadSavePath.Desk`.
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
