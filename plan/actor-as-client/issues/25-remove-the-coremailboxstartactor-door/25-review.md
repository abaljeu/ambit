Check: pass

# 25 — Remove the coreMailboxStartActor door

Ticket: [[25-remove-the-coremailboxstartactor-door-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff 2ea00d1b` on commit `aff32910` printed no line and exited 0. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Range.** The squash is already on staging. The scan range is that commit against its parent. The source edit deletes `coreMailboxStartActor`, `dispatchStartActor`, and `replyStarted`, and the `Order.StartActorDeprecated` arm is one reply line.
- **Rules.** The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **One job.** `CoreMailboxApi.coreMailboxStartActor` is gone. A whole-word search of `src/` and `tests/` for `coreMailboxStartActor` is empty. No code places `Order.StartActorDeprecated` through that door.
- **Function start.** `coreMailboxStartActorFromFunction` stays in `src/Server/Core/CoreMailboxApi.fs`. `Order.StartActor` stays.
- **Deprecated arm.** `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies `Error "StartActorDeprecated is retired"` for `Order.StartActorDeprecated`. `dispatchStartActor` and `replyStarted` are gone. `Order.StartActorDeprecated` stays. [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) owns that case.
- **Client request.** `EventBody.ActorStart` stays in `src/Shared/History.fs`. It builds `EventBody.Order` of `Order.StartActorDeprecated`.
- **Load and Save start.** `dispatchActorStartResult` stays. `CoreMailboxLoad.dispatchStartLoadSaveCommand` calls it. `finishReadyStart` stays with that call.
- **Named calls.** ``Peer Actor is not available through the Run actor table`` and ``unregistered Actor name fails start`` are deleted. Each one called the door and asserted a name-lookup failure. A function start supplies the actor, so those assertions have no remaining seam.

## 3. Arch

- **Place actor order.** The arch line matches the code. `placeOrder` replies `Error "StartActorDeprecated is retired"` for `Order.StartActorDeprecated`.
- **Named start today.** A placed `Order.StartActorDeprecated` ends in Place actor order. The story path lists that section.
- **Module map.** The diff leaves `placeOrder`, `startFunction`, `postActorOrder`, and `EventBody.ActorStart` in place.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2907` printed `DONE` and `/tmp/ambit-full-suite-20261113-152955.log`. The script file has no execute bit, so the direct path reports `Permission denied`.
- **Server.** `tests/Server.Tests` passed 764, failed 0, skipped 1. Total 765. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1940, failed 0, skipped 3. Total 1943. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Later commit.** Staging also contains [28 — Limit the AI extract to the visible list](../28-limit-the-ai-extract-to-the-visible-list/28-limit-the-ai-extract-to-the-visible-list-ticket.md). That commit adds the two tests that raise the Server count from 762 to 764. It leaves the door files as the squash left them.
- **Inactive and blank.** ``function start from an inactive Actor is dropped`` and ``function start from an inactive Actor does not hand off`` expect `Error "authority dropped"` and no live row. ``function start with blank Authority does not hand off`` expects `Error` from the authority check and no live row.
- **Live row and lock.** ``function start creates live row synchronously`` records `liveActorNodeIds` at the `startFunction` return. ``function start exposes Graph lockPresent via getState`` and ``GetState stamps lockPresent from the live table after a function start`` leave `finish` as a no-op so `actorStop` does not clear the row, then assert `lockPresent`.
- **Reply and stop.** ``function start reply is bookkeeping without waiting for an Actor body`` expects the reply within one second and a live row for an actor that returns at once. ``function start yields its secret and a later stop is dropped`` reads `run.secret`, stops once, and expects `Error "authority dropped"` on the second stop.
- **Persist.** ``function start persist Error leaves Focus out of liveActorNodeIds`` and ``StartActor persist Error does not keep Ev in mailbox log`` fail `appendEvent` for `Order.StartActor` and expect that body absent from the live set or the log. ``StartActor persists across File restart`` expects `Order.StartActor` after dispose and create.
- **Lifecycle.** ``mailbox appends StartActor and ActorStop in lifecycle order`` expects `Order.StartActor` with actor name `test` and the focus id, then `EventBody.ActorStop` of `ActorFailed ""`.
- **Durability.** ``File mixed hello survives dispose create`` and ``Db mixed hello survives dispose create`` restart after Change, `Order.StartActor`, and `ActorStop`. ``Undo after restart refuses a target from before the restart`` and ``Undo after restart accepts a target after the restart`` use that same log. ``getEventId is ActorStop after in-process hello`` expects the tip at the stop. ``File empty Graph checkpoint live append continues past Graph`` expects the `Order.StartActor` id 6 and `getEventId` at least that id.
- **Deliver.** ``mint deliver enqueue drop then deliver fails`` and ``empty deliver keeps session live for a later chunk`` start through `coreMailboxStartActorFromFunction`, record `sessionIdFor`, and stay live for deliver and drop.
- **Cancel.** ``Change before Cancel keeps the accepted children`` starts through `startFromFunction`.
- **Deleted door tests.** ``CoreMailboxApi.startActor calls pool.startActor and returns bookkeeping result``, ``Actor start rejects a secret the pool does not admit``, ``Actor post with a secret the pool does not hold is dropped``, ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``, ``CoreActorPool.startActor uses client graphIds to build subgraph``, ``CoreActorPool.startActor selects actor from command node text``, ``CoreActorPool.startActor fails when graphIds is empty``, ``CoreActorPool.startActor fails when runDefinitionId not in graphIds``, ``mailbox records ActorStarted before actor body runs``, ``StartActor with live credentials calls startActor with ActorStart``, ``second start on the same runDefinitionId is rejected``, ``Peer Actor is not available through the Run actor table``, and ``unregistered Actor name fails start`` only enter through the door or `pool.startActor`.
- **Focus exclusivity.** ``focus exclusivity still rejects a second live Focus`` calls `pool.startActor` and expects `focus already has a live Actor`. ``second launch on the same Focus is refused`` still expects `actor node already has a live Actor` for a function start.
- **Ask extract.** ``Ask extract carries Graph.focus from launch`` is deleted. Focus capture belongs to the `?ai` function's interface.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Accepted.** **Problem.** [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) still says `placeOrder` runs `dispatchStartActor` for `Order.StartActorDeprecated`, and its process step says to remove that arm. **Fix.** When 16 — Delete the old Order cases deletes the case, drop the reject arm with it. `dispatchStartActor` is already gone.
- **Rejected.** **Problem.** Arch section Posted run and story path 6 — Query start today still say `startChosen` stays `pool.startActor`. **Fix.** Leave those sentences. [26 — Retire the posted named start](../26-retire-the-posted-named-start/26-retire-the-posted-named-start-ticket.md) removed `startChosen`. This ticket's arch edit is the `Order.StartActorDeprecated` reply.
