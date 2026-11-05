# 80 — Place Actor starts through PlaceOrder
**Status**: defined
One job: The client places `Order.ActorOrder` through `Mailbox.PlaceOrder` with that client's Caller, and the Server runs the actor already on that order on the .NET thread pool. `startFunction` mints the Actor credential and the Actor session id, which is the pool-start rule in [Arch](plan/actor-as-client/arch.md).

Type: coding

Blockers:

[ ] [06 — Log ActorOrder](plan/actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md). Draft [PR #527](https://github.com/abaljeu/ambit/pull/527), branch `cursor/log-actor-order-7fe1`. This ticket starts after 06 lands. 06 may still be open.

## Inputs

### Working

[ ] `authorityHeld` in `src/Server/Core/CoreMailboxLoop.fs` returns true for `Authority "Actor"` and does not read `secret`.
[ ] Every other authority is held when `Session.wasDropped` is false. Staging and the 06 branch match.
[ ] `Mailbox.PlaceOrder` with `Authority "Actor"` and `Credential ""` is accepted today. The client place path uses the client's Caller.
[ ] `postActorOrder` on the 06 branch, in `src/Server/Core/MailboxHost.fs`, posts `Mailbox.PlaceOrder` through `Mailbox.placeForCaller`. Its Caller is `Authority "Actor"`, `name` = `actorName`, and `secret` = `Credential ""`.
[ ] `postActorOrder` has no caller on the 06 branch. [07 — Start the test command](plan/actor-as-client/issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) is the first planned caller.
[ ] `Mailbox.placeForCaller` in `src/Server/Core/CoreMsg.fs` sets `PlacedOrder.authority` from the Caller. Read only.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` handles `Order.CancelActor` through `replyCancel`.
[ ] On the 06 branch the unsupported arm is `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, and `Order.ActorOrder`. The reply is `Error "unsupported"`. Staging has the same three old arms and gains the `ActorOrder` arm when 06 lands.
[ ] `Order.CancelActor` is the cancel case. [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md) has landed. The wire kind is `cancel`.
[ ] `ActorOrder` on the 06 branch, in `src/Shared/History.fs`, is `actor`, `getGraph`, `focusId`, and `actorName`. `Order.ActorOrder` carries that record. Equality uses `focusId` and `actorName`.
[ ] `ActorOrder` has no `actorNodeId`.
[ ] Shared `FunctionActor` is `FunctionRun -> ActorChanges -> Async<unit>`.
[ ] The pool `FunctionActor` in `src/Server/Core/CoreActorPool.fs` is `FunctionRun -> CoreChangesDoor -> Async<unit>`. `FunctionStart` holds the pool function, `getGraph`, `focusId`, and `actorNodeId`. `startFunction` is `FunctionStart -> Result<Credential, string>`.
[ ] `putFunctionStart` mints `Credential` from a new Guid `"N"` and a new session id, then stores `FunctionPending`. `schedule` calls `Async.Start` with the row cancel token.
[ ] `encodeActorOrder` writes `focusId` and `actorName`. `decodeActorOrder` sets `actor` to `stubActor`.
[ ] `MailboxLog.historyBody` on the 06 branch accepts `Order.ActorOrder`. It rejects `Order.StartPeerActor` and `Order.LoadSave`.
[ ] `startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`. A Desk Load or Save runs no actor.
[ ] 06 adds no `ActorOrder` log for Desk. The Desk arm stays off `postActorOrder`.
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` covers the actor `CoreMsg` cases. `tests/Server.Tests/CoreMailboxDoorTests.fs` holds ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``. `tests/Server.Tests/LoadSaveCommandTests.fs` holds ``Desk request reaches pool without starting Peer Actor``.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) decision 19 **One case each**.
[ ] A wire-read `ActorOrder` is a log record. `stubActor` stays unrun. Arch states that rule.

## Do not load

[ ] `src/Server/Core/CoreMsg.fs` stays as it is. No new `CoreMsg` case.
[ ] `authorityHeld` stays. The `Authority "Actor"` arm still returns true for other Actor posts.
[ ] `ActorDispatch.fs`, `actorForCommand`, and `Order.RunActor` stay out.
[ ] `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` stay unsupported. [15 — Delete the old Actor starts](plan/actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deletes the old starts.
[ ] `src/Server/SearchActor.fs` and `src/Server/TestActor.fs`. 06 left their pool `FunctionActor` on `CoreChangesDoor`.
[ ] The Desk HTTP arm. [10 — Start git Load and Save](plan/actor-as-client/issues/10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md) keeps that arm off `postActorOrder`.
[ ] `startFunction`'s arguments stay as 06 left them.
[ ] Remote starts.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postActorOrder` takes the client's `Caller` and posts `Mailbox.PlaceOrder` with that Caller. The Caller carries the client's authority, session name, and session `Credential`.
3. [ ] Change: `placeOrder` accepts that client-placed `Order.ActorOrder`. The pull builds `FunctionStart` from the order's `actor`, `getGraph`, and `focusId`, and from the actor node id the Run path closed over. It calls `startFunction`, then `schedule`, so the actor runs on the .NET thread pool. `putFunctionStart` mints the Actor credential and the Actor session id.
4. [ ] Change: an `ActorOrder` whose `actor` is `stubActor` stays a log record through `MailboxLog.eventFromPlacedOrder`. The pull leaves `stubActor` unrun.
5. [ ] Change: that same `placeOrder` match leaves `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` on `replyUnsupported`.

## Outputs

[ ] `src/Server/Core/MailboxHost.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — ``ActorOrder places the actor on the thread pool``
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — ``wire-read ActorOrder does not run stubActor``
[ ] Source files: 2. Test files: 1.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs or under Do not load.
[ ] Stop when a caller of `postActorOrder` exists outside Inputs.
[ ] Stop when the PlaceOrder Caller for this path is `Authority "Actor"` with `Credential ""`.
[ ] Stop when `order.actor` cannot be passed to `startFunction` without a change to `startFunction`'s arguments or an edit under Do not load.
[ ] Stop when `FunctionStart.actorNodeId` cannot be filled from the Run path closure. The pull does not look up `actorName`.
[ ] Stop when the edit adds a `CoreMsg` case, runs `stubActor`, or writes `Order.StartActor`, `Order.LoadSave`, or `Order.RunActor`.
[ ] Stop when a Desk Load or Save starts an actor.
[ ] Stop when the edit contradicts Arch.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``wire-read ActorOrder does not run stubActor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
  - `tests/Server.Tests`: ``Desk request reaches pool without starting Peer Actor``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
