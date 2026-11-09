# 06 — Log ActorOrder
**Status**: coded
One job: The log has `Order.StartActor` of `ActorOrder`. The pool start stays `startFunction`.

[x] `ActorOrder` in `src/Shared/History.fs` holds `actor`, `actorNodeId`, and `actorName`. `encodeActorOrder` in `src/Shared/EventJson.fs` writes `actorNodeId` under the wire key `focusId`. `actorName` stands in for `actor` on the wire. The mailbox supplies `getGraph` on `FunctionStart`.
[x] `GraphCarrier`, `FunctionRun`, and `Credential` move to `src/Shared/History.fs`. `ActorChanges` and the log `ActorFunction` are added there. `ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>`. The pool `ActorFunction` stays in `src/Server/Core/CoreActorPool.fs` as `FunctionRun -> CoreChangesDoor -> Async<unit>`. `actorChanges` is in `src/Server/Core/CoreDoor.fs`. The source files are 8. The named test files are 2. `tests/Server.Tests/PersistApplyTests.fs` annotates `CoreMailboxAccess` where `getState` overlaps `ActorChanges`. The size limit is 8 source files and 4 test files.
[x] The folder name stays `06-add-the-function-and-focus-start` so existing links keep their path.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522. The cancel case is `Order.CancelActor`. The wire kind stays `cancel`. It leaves `MailboxHost.fs` untouched.

Type:

[x] coding

Blockers:

[x] [17 — Rename commandId to actorNodeId](../17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md). It runs before this ticket.
[x] [112 — Place Cancel through PlaceOrder](../../../core-refinement/issues/112-place-cancel-through-placeorder.md) landed on staging in PR 522.

## Inputs

### Working

[x] `startFunction` in `src/Server/Core/CoreActorPool.fs` takes `FunctionStart`. `FunctionStart` and the pool `ActorFunction` are in that file. `GraphCarrier` and `FunctionRun` are in `src/Shared/History.fs`.
[x] `Credential` is `Credential of string` in `src/Server/Core/CoreChanges.fs`.
[x] `CoreChangesDoor` stays in `src/Server/Core/CoreChangesDoor.fs`. Actors post changes, stop, read state, and load a node through `CoreDoor`.
[o] `startActor` and `startPeerActor` are the other pool starts.
[x] `Order` in `src/Shared/History.fs` is `Change`, `Undo`, `Redo`, `StartActorDeprecated` of `ActorStart`, `StartActor` of `ActorOrder`, `Load`, and `CancelActor`.
[ ] `Order` is matched in `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs`, and `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] The mailbox queue is `MailboxProcessor<CoreQueue>` in `src/Server/Core/MailboxHost.fs`.

### Reference

[ ] [Arch](../../arch.md). `Order.StartActor` of `ActorOrder` is logged as-is. `postActorOrder` in `src/Server/Core/MailboxHost.fs` takes `MailboxHost`, `Caller`, and `ActorOrder`, and returns `Async<Result<EventId, string>>`.

## Do not load

[ ] Do not add a `CoreMsg` case.
[ ] Do not add `ActorDispatch.fs` or `actorForCommand`. Leave `Order` as the cases in `src/Shared/History.fs`.
[ ] Do not add a pool field. Do not change `startFunction`'s arguments.
[ ] Do not move a caller. That is 07 — Start the test command and the tickets after it.
[ ] Do not delete `startActor`. Do not delete `startPeerActor`. 15 — Delete the old Actor starts deletes `startPeerActor`.
[ ] Do not run `stubActor`. A wire-read `ActorOrder` is a log record.
[ ] Do not move `CoreChangesDoor`, `MailboxHost`, or `Caller`.
[ ] Closed: `src/Client`. Remote starts stay out of scope.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Expand: Move `GraphCarrier`, `FunctionRun`, and `Credential` to `src/Shared/History.fs`. Add `ActorChanges` and `ActorFunction` there. `ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>`. That is the log function. Keep the pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs` as `FunctionRun -> CoreChangesDoor -> Async<unit>`. `FunctionStart` stays on that pool function. In `src/Server/TestActor.fs`, `functionActor` is the pool `ActorFunction`.
3. [x] `actorChanges` is in `src/Server/Core/CoreDoor.fs`.
4. [x] Expand: Add `ActorOrder` and `| StartActor of ActorOrder` on `Order` beside the cases that exist after 112.
5. [x] The logged value is that case, stored as-is.
6. [x] Expand: Add `stubActor : ActorFunction`, `encodeActorOrder : ActorOrder -> IEncodable`, and `decodeActorOrder` in `src/Shared/EventJson.fs`. Encode writes the wire key `focusId` from `actorNodeId`, and `actorName`. `actorName` stands in for `actor`. Decode sets `actor` to `stubActor`.
7. [x] Expand: Add the `MailboxLog` arm. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` runs `replyActorOrder` for `Order.StartActor`. [18 — Place ActorOrder through PlaceOrder](../18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md) is that pull.
8. [x] Expand: Add `postActorOrder` on `MailboxHost`. It places one `Order.StartActor`. The current arguments are `MailboxHost`, `Caller`, and `ActorOrder`.
9. [x] Leave `startFunction`, `startActor`, `startPeerActor`, and `CoreMsg` as they are.
10. [x] A match of the cancel case uses `Order.CancelActor`.

## Outputs

[x] `src/Shared/History.fs`
[x] `src/Server/Core/CoreActorPool.fs`
[x] `src/Server/Core/CoreChanges.fs`
[x] `src/Server/TestActor.fs` — `functionActor` is the pool `ActorFunction`
[x] `src/Shared/EventJson.fs`
[x] `src/Shared/MailboxLog.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `src/Server/Core/MailboxHost.fs`
[x] `tests/Shared.Tests/EventJsonTests.fs` — `ActorOrder round-trips on the wire with stubActor`
[x] `tests/Shared.Tests/MailboxLogTests.fs` — `ActorOrder carries command name and FocusId`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/06-add-the-function-and-focus-start-stop.md`, then ask.
[ ] Stop when the edit moves `CoreChangesDoor`, `MailboxHost`, or `Caller`.
[ ] Stop when the edit adds a `CoreMsg` case or a pool start. Leave `Order` as the cases in `src/Shared/History.fs`.
[ ] Stop when the edit needs a file outside Inputs.
[x] 112 — Place Cancel through PlaceOrder landed on staging in PR 522.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
  - `tests/Shared.Tests`: ``ActorOrder carries command name and FocusId``
[x] All executed tests pass.
[x] Write the verdict to `plan/actor-as-client/reports/06-add-the-function-and-focus-start-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
