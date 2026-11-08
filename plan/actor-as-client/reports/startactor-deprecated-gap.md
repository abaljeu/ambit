# StartActorDeprecated gap

Branch: `dev`. [Git protocol](../../../.agents/skills/git-protocol/SKILL.md) allows a read on `dev`. This pass edits no source.

The product names in [Actor as client](../project.md) predate the current case names. `Order.StartActor` of `ActorStart` in those tickets is `Order.StartActorDeprecated` in `src/Shared/History.fs`. `Order.ActorOrder` in those tickets is `Order.StartActor` of `ActorOrder`.

## 1. Payloads

### 1. ActorStart

`ActorStart` in `src/Shared/History.fs` carries

1. `zoomId`
2. `focusId` — the Focus
3. `actorNodeId` — the Command node
4. `graphIds` — the Included expand of Zoom
5. `eventId`

### 2. ActorOrder

`ActorOrder` in the same file carries

1. `actor` — an `ActorFunction`
2. `focusId`
3. `actorName`

`Equals` and `GetHashCode` use `focusId` and `actorName`. The Actor function stays out of equality.

The shared field is `focusId`. No function in `src/` or `tests/` builds one value from the other.

### 3. Who writes StartActorDeprecated

The case is built in three source sites:

1. `EventBody.ActorStart` in `src/Shared/History.fs` wraps the case.
2. `coreMailboxStartActor` in `src/Server/Core/CoreMailboxApi.fs` posts the case. Callers of that function sit in tests. The count is 27 call expressions in 10 test files.
3. `decodeOrder` in `src/Shared/EventJson.fs` builds the case for wire kind `startActor`.

`EventBody.ActorStart` is the factory the rest of the program calls. Source callers:

1. `RunLaunch.queueStart` in `src/Shared/RunLaunch.fs`. `Commands.queueRun` in `src/Client/Commands.fs` reaches it from `CommandRequest.tryStart` and `CommandRequest.tryQueryStart` in `src/Shared/CommandRequest.fs`.
2. `finishReadyStart` in `src/Server/Core/CoreMailboxActors.fs`.
3. `recordFailedStart` in the same file.
4. `queueAdmittedTest` in the same file.
5. `CoreEventDispatch.actorStart` in `src/Server/Core/CoreEventDispatch.fs`. `RecordSearchStart` in `src/Server/Core/CoreMailboxLoop.fs` calls it.

18 test files name `EventBody.ActorStart`.

### 4. Who reads StartActorDeprecated

Direct matches:

1. The active pattern `EventBody.ActorStart` in `src/Shared/History.fs`. This is the only unwrap.
2. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls `dispatchStartActor`.
3. `encodeOrder` in `src/Shared/EventJson.fs` writes wire kind `startActor`.
4. `historyBody` in `src/Shared/MailboxLog.fs` keeps the order.

Readers of the active pattern in source:

1. `expandStored` in `src/Server/Core/CoreMailboxApi.fs`
2. `dispatchPostEvent` in `src/Server/Core/CoreMailboxActors.fs`
3. `completeOther` in `src/Server/Core/CoreEventDispatch.fs`
4. `ActorLive.applyEvent` in `src/Shared/ActorLive.fs`
5. `ActorLive.resultOf` in the same file

`Ev.ops`, `Ev.withOps`, and `Ev.target` in `src/Shared/History.fs` treat every `EventBody.Order` as a non-Action. Both order cases already share that.

### 5. Who writes StartActor

1. `postActorOrder` in `src/Server/Core/MailboxHost.fs` places `Order.StartActor`.
2. `replyActorOrder` in `src/Server/Core/CoreMailboxLoop.fs` logs that same order.
3. `logDeskOrder` in `src/Server/Core/CoreMailboxLoad.fs` logs `stubActor` and an empty `actorName`.
4. `decodeOrder` in `src/Shared/EventJson.fs` builds the case for wire kind `actorOrder` and sets `actor` to `stubActor`.
5. `queueAdmittedTest` builds the record and calls `postActorOrder`.

Test builders that already hold an `ActorStart` and an Actor function:

1. `postTestActorOrder` in `tests/Server.Tests/TestBackend.fs`
2. The `?test` facts in `tests/Server.Tests/TestActorHelloTests.fs` and `tests/Server.Tests/TestActorCommandErrorTests.fs`, through that helper

`SearchActor.functionStart` in `src/Server/SearchActor.fs` builds a `FunctionStart` from an `ActorStart`. It builds no `ActorOrder`. Search still logs through `RecordSearchStart`.

### 6. Who reads StartActor

1. `placeOrder` calls `replyActorOrder`, which calls `scheduleActorOrder`, which calls `startFunction`.
2. `encodeOrder` writes wire kind `actorOrder` with `focusId` and `actorName`.
3. `historyBody` keeps the order.
4. Six test files match `Order.StartActor`: `TestActorHelloTests.fs`, `TestActorCommandErrorTests.fs`, `EventJsonTests.fs`, `CoreMsgActorCasesTests.fs`, `LoadSaveCommandTests.fs`, `MailboxLogTests.fs`.

## 2. Why a rename stays blocked

### 1. Shape

A call site that holds an `ActorStart` still needs an `ActorFunction` and an `actorName`. A call site that holds an `ActorOrder` still needs `zoomId`, `actorNodeId`, `graphIds`, and `eventId`.

The browser builds the id bag in `CommandRequest.actorStart`. The Actor function lives on the Server. `RunLaunch.queueStart` can post `EventBody.ActorStart` and has no Actor function to place.

### 2. Pool effect

`dispatchStartActor` calls `pool.startActor` (`src/Server/Core/CoreMailboxActors.fs`). `runStartActor` in `src/Server/Core/CoreActorPool.fs` requires a non-empty `graphIds` list, extracts that subgraph, reads the Command node, resolves a registered `ActorFn`, and rejects a live Focus or a live Command node. The live row stores `actorNodeId`. Pending work is `BagPending` with that extracted Graph.

`scheduleActorOrder` calls `pool.startFunction`. `admitFunctionStart` checks the live Focus. `putFunctionStart` stores `focusId` in the live row's `actorNodeId` slot. Pending work is `FunctionPending`. The mailbox passes `graphNow` as `getGraph`.

### 3. Log and reply

`replyActorOrder` appends `Order.StartActor` first. `submissionId` is the placed order id. `commandName` comes from `actorName` in `postActorOrder`. The reply is the stored Event id. A failed `startFunction` leaves that Event in the log.

`dispatchStartActor` appends `EventBody.ActorStart` after `startActor` succeeds. `finishReadyStart` uses a new `Guid` and `lifecycleEvent` sets `commandName` to `""`. `replyStarted` replies `EventId.zero`. A rejected start replies the error and appends no Event.

A client post of the same `ActorStart` takes another door. `dispatchPostEvent` calls `postActorStart`. A `?test` Command logs `EventBody.ActorStart` and then queues `ActorOrder`. Any other name uses `startActor`. A rejected posted start goes through `recordFailedStart`, which appends `ActorStart` and then `ActorStop`.

### 4. Wire

Kind `startActor` encodes `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId` (`src/Shared/EventJson.fs`). Kind `actorOrder` encodes `focusId` and `actorName`. Decode sets `actor` to `stubActor`. [06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) keeps a wire-read `ActorOrder` as a log record. `scheduleActorOrder` would run that stub.

### 5. Readers of the active pattern

`ActorLive.applyEvent` adds `focusId` for `EventBody.ActorStart`. The same function leaves every other `EventBody.Order` out of the live set. `resultOf` uses `zoomId` for the started line. A row that is only `Order.StartActor` stays invisible to those two functions, to `expandStored`, and to `dispatchPostEvent`.

`queueAdmittedTest` writes both rows on purpose. The `EventBody.ActorStart` row is the events-door ack for the client submission. The `Order.StartActor` row is the row `startFunction` runs. Replacing the ack row changes the body `postEvents` returns.

### 6. Desk

`logDeskOrder` appends `Order.StartActor` with `stubActor`. `dispatchStartLoadSaveCommand` then still calls `dispatchActorStartResult` on `request.start`. Git Load and Save use `startPeerActor` and the id bag. Feeding the desk stub to the `StartActor` arm of `placeOrder` would run `stubActor`.

## 3. Recommended next step

Add `start: ActorStart` to `ActorOrder`. Read Focus from `start.focusId` in `scheduleActorOrder`, equality, and the `actorOrder` codec. Keep the wire keys `focusId` and `actorName`.

Fill `start` from the `ActorStart` these builders already hold:

1. `queueAdmittedTest`
2. `postTestActorOrder`
3. `logDeskOrder`

Decode of kind `actorOrder` builds `start` with `zoomId` and `actorNodeId` set to that `focusId`, `graphIds` empty, and `eventId` zero.

This step leaves these alone:

1. Both `placeOrder` arms
2. `startActor` and `startFunction`
3. The active pattern `EventBody.ActorStart`
4. `coreMailboxStartActor` and its 27 test call expressions
5. `RunLaunch.queueStart`, `finishReadyStart`, `recordFailedStart`, and `CoreEventDispatch.actorStart`

After this step, the three builders above keep zoom, the Command node, `graphIds`, and `eventId` on the order they already post. A later one-line arm on the active pattern can return `order.start`. That later arm is a separate decision: `dispatchPostEvent` would then treat a posted `Order.StartActor` as a bag start, and the `?test` log would project two starts.

The 27 `coreMailboxStartActor` tests stay on `StartActorDeprecated`. They assert registry failure, `graphIds`, and a reply of `EventId.zero`.

## 4. Weaker next steps

### 1. Lossy active pattern

Widen `EventBody.ActorStart` now and synthesize `zoomId`, `actorNodeId`, and empty `graphIds` from `focusId`.

Readers would see both cases. `dispatchPostEvent` would bag-start a posted `Order.StartActor`. The started line would lose Zoom. `admitStart` would reject the empty `graphIds`. The three builders would still drop the real id bag.

### 2. Retarget the deprecated arm

Send `StartActorDeprecated` through `replyActorOrder` with `stubActor` or a wrapper around `startActor`.

That merges the two pool starts, the two reply ids, and the two failure logs in one edit. The 27 tests are the suite for the bag start. The browser queue would still post `EventBody.ActorStart`.

## 5. Open questions

1. 18 — Place ActorOrder through PlaceOrder stops when `ActorOrder` carries `actorNodeId`. A `start: ActorStart` field carries that id. Does that stop still hold?
2. The `?test` path logs an `EventBody.ActorStart` ack and then an `Order.StartActor` row. Which row should Poll and `postEvents` treat as the start?
3. Should a decoded `actorOrder` (stub Actor function, synthetic ids) stay off the active pattern?
4. The map keeps `graphIds` for this cutover and drops them in the end state. Should `ActorOrder` carry the full `ActorStart` through the cutover?

## 6. Plan already on this migration

[06 — Log ActorOrder](../issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md) adds the function order. Status on the ticket is coded. The shape is `actor`, `focusId`, and `actorName`.

[07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) moves `?test` onto that order. The review records the extra `EventBody.ActorStart` ack.

[18 — Place ActorOrder through PlaceOrder](../issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md) runs the Actor function from `placeOrder`. Desk logs the order and runs no Actor function. The ticket forbids `actorNodeId` on `ActorOrder`.

[15 — Delete the old Actor starts](../issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deletes `startActor`, `startPeerActor`, and the remaining start `CoreMsg` cases after each Command has moved. `CoreMsg` in `src/Server/Core/CoreMsg.fs` has `StartPeerActor`, `StartLoadSaveCommand`, and `RecordSearchStart`. It has no `StartActor` case.

[16 — Delete the old Order cases](../issues/16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) deletes the id-bag order and leaves the function order. Its inputs still say `placeOrder` refuses the old case. The current `placeOrder` runs `dispatchStartActor` for `StartActorDeprecated` and `replyActorOrder` for `StartActor`.

[08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md), [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md), and [10 — Start git Load and Save](../issues/10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md) are the remaining Command moves.

[66 — Move Actor orders onto Order and Notice](../../core-refinement/issues/66-move-actor-orders-onto-order-and-notice.md) and [80 — Place Actor starts through PlaceOrder](../../core-refinement/issues/80-place-actor-orders-through-placeorder.md) are cancelled. 80 names 18 as its replacement.

[Map](../map.md) records the cutover: in-process start posts a curried Actor function; this cutover keeps the browser `ActorStart`, `graphIds` included.
