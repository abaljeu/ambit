# ActorOrder from ActorStart

Project: [Actor as client](../project.md)

Branch: `dev`

No commit. Desktop git protocol leaves the commit to the human.

## Files

- `src/Server/ActorOrderFromStart.fs` — new module. `orderFromStart` builds an `ActorOrder`. `actorNodeIdOf` is the one place that sets `actorNodeId`.
- `src/Server/Gambol.Server.fsproj` — compiles the new file before the mailbox actors.
- `src/Server/Core/CoreMailboxActors.fs` — `queueAdmittedTest` calls `orderFromStart` with `TestActor.functionActorAt` and actor name `test`.
- `tests/Server.Tests/ActorOrderFromStartTests.fs` — the new test.
- `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compiles the new test.

## Signature

```fsharp
ActorOrderFromStart.orderFromStart :
    makeActor: (NodeId -> NodeId -> Gambol.Shared.ActorFunction) ->
    actorName: string ->
    start: ActorStart ->
    ActorOrder
```

`actorNodeIdOf` is private. It returns `start.runDefinitionId`. A later actor replaces that function only.

`queueAdmittedTest` is the only call site moved off the inline record. `TestBackend.postTestActorOrder` still builds its own order with `actorNodeId = request.focusId`.

## Tests

Red: `dotnet test` on `FullyQualifiedName~orderFromStart` failed to compile. FS0039: `ActorOrderFromStart` was not defined.

Green, same filter plus `FullyQualifiedName~admits the caller and queues`:

- Passed: 2
- Failed: 0

The new test is `orderFromStart sets actorNodeId to runDefinitionId and the given actor name`. Focus and the run-definition node differ. The order's `actorNodeId` is `runDefinitionId`. The actor name is the given string (`test`, then `query`). The actor value is the factory result, and the factory receives `runDefinitionId` then `focusId`.

`?test Run admits the caller and queues ActorOrder` still passes. That test uses one node for `focusId` and `runDefinitionId`. Hello still lands under Focus. `ActorStop` still matches that node.

Full `tests/Server.Tests` with `AMBIT_FULL_SUITE=1`:

- Passed: 753
- Failed: 0
- Skipped: 1

The skip is `DocumentPersistenceTests.Change.upload structure persistence preserves existing file mtime`. No failed tests.

## Admission keys

The intended rule is: reject when `actorNodeId` is already live.

Live rows store `focusId` and `runDefinitionId`. `liveFocusIds` is the set of live-row `focusId` values.

- `admitStart` (bag `startActor` and `startPeerActor`) rejects when `liveFocusIds` contains `ActorStart.focusId`. Message: `focus already has a live Actor`. It then rejects when `commandIsLive` finds `ActorStart.runDefinitionId`. Message: `command already has a live Actor`. It also rejects an empty `graphIds` list. Bag `putActorStart` stores `ActorStart.focusId` and `ActorStart.runDefinitionId` on the live row as two fields.
- `commandIsLive` is true when a live row's `runDefinitionId` equals the given node. `admitStart` is its only caller.
- `admitFunctionStart` (`startFunction`, which `scheduleActorOrder` uses for `Order.StartActor`) rejects when `liveFocusIds` contains `FunctionStart.focusId`. Message: `focus already has a live Actor`. On that path `FunctionStart.focusId` is `ActorOrder.actorNodeId`. `putFunctionStart` writes that same node into both live-row fields.

`admitFunctionStart` is the check whose key is `actorNodeId` on the `ActorOrder` path. `admitStart` keys the cursor node (`ActorStart.focusId`) and the run-definition node (`ActorStart.runDefinitionId`).

After this step, a `?test` `ActorOrder` stores `runDefinitionId` in both live-row fields. A second function start with that `actorNodeId` fails `admitFunctionStart`. A bag start of that same run-definition node fails `commandIsLive`.

`Order.CancelActor` still carries a payload named `focusId`. `runCancel` looks up `trySecretForFocus` on that value, and that lookup matches live-row `focusId`. On the `ActorOrder` path that live field is `actorNodeId`. For `?test` that node is now `runDefinitionId`.

## Later bag ActorFn plug-in

Bag actors stay `ActorFn` values in `CoreActorPool.register`, started by `Order.StartActorDeprecated` and `admitStart`.

A later command plugs in like `?test`:

- Add a factory with the shape `NodeId -> NodeId -> ActorFunction`, same as `TestActor.functionActorAt`. The function reads the graph with `getGraph`.
- At that command's admit site, call `ActorOrderFromStart.orderFromStart factory actorName start`.
- Replace `actorNodeIdOf` when that actor's cancelable node is a different field.

A registry can map an actor name to that factory and name, then call `orderFromStart`. This step adds no registry and no adapter from `ActorFn` to `ActorFunction`.
