# 06 — Log ActorOrder
**Status**: defined
One job: The log has `Order.ActorOrder` of `ActorOrder`. The pool start stays `startFunction`.

[ ] `ActorOrder` holds `actor`, `getGraph`, `focusId`, and `commandName`.
[ ] `FunctionActor`, `FunctionRun`, `GraphCarrier`, `Credential`, and `ActorChanges` move to `src/Shared/History.fs`. `CoreDoor.actorChanges` builds `ActorChanges` from `CoreChangesDoor`. The source files are 8. The test files are 2. The size limit is 8 source files and 4 test files.
[ ] The folder name stays `06-add-the-function-and-focus-start` so existing links keep their path.
[ ] This ticket starts after 112 — Place Cancel through PlaceOrder has landed. That ticket renames `Order.Cancel` to `Order.CancelActor`. The wire kind stays `cancel`. It leaves `MailboxHost.fs` untouched.

Type:

[ ] coding

Blockers:

[ ] [112 — Place Cancel through PlaceOrder](../../../core-refinement/issues/112-place-cancel-through-placeorder.md)

## Inputs

### Working

[x] `startFunction` in `src/Server/Core/CoreActorPool.fs` takes `FunctionStart`. `GraphCarrier`, `FunctionRun`, and `FunctionActor` are defined in that file.
[x] `Credential` is `Credential of string` in `src/Server/Core/CoreChanges.fs`.
[x] `CoreChangesDoor` stays in `src/Server/Core/CoreChangesDoor.fs`. Actors post changes, stop, read state, and load a node through `CoreDoor`.
[o] `startActor` and `startPeerActor` are the other pool starts.
[ ] `Order` in `src/Shared/History.fs` has `StartActor`, `StartPeerActor`, `LoadSave`, and `Cancel`. It has no `ActorOrder`.
[ ] `Order` is matched in `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs`, and `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs`.
[ ] The mailbox queue is `MailboxProcessor<CoreQueue>` in `src/Server/Core/MailboxHost.fs`.

### Reference

[ ] [Arch](../../arch.md). `Order.ActorOrder` is logged as-is. `postActorOrder : ActorOrder -> unit`.

## Do not load

[ ] Do not add a `CoreMsg` case.
[ ] Do not add `ActorDispatch.fs`, `actorForCommand`, or `Order.RunActor`.
[ ] Do not add a pool field. Do not change `startFunction`'s arguments.
[ ] Do not move a caller. That is 07 — Start the test command and the tickets after it.
[ ] Do not delete `startActor` or `startPeerActor`. That is 15 — Delete the old Actor starts.
[ ] Do not run `stubActor`. A wire-read `ActorOrder` is a log record.
[ ] Do not move `CoreChangesDoor`, `MailboxHost`, or `Caller`.
[ ] Closed: `src/Client`. Remote starts stay out of scope.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand: Move `GraphCarrier`, `FunctionRun`, `FunctionActor`, and `Credential` to `src/Shared/History.fs`. Add `ActorChanges` there. `FunctionActor` is `FunctionRun -> ActorChanges -> Async<unit>`. Delete the moved definitions from `CoreActorPool.fs` and `CoreChanges.fs`.
3. [ ] Expand: Add `actorChanges : CoreChangesDoor -> ActorChanges` in `CoreDoor`. `postChanges` returns `Error` when the door is not writable.
4. [ ] Expand: Add `ActorOrder` and `| ActorOrder of ActorOrder` on `Order` beside the cases that exist after 112.
5. [ ] The logged value is that case, stored as-is. It is not `Order.RunActor`.
6. [ ] Expand: Add `stubActor : FunctionActor`, `encodeActorOrder : ActorOrder -> IEncodable`, and `decodeActorOrder : string -> Result<ActorOrder, string>` in `EventJson`. Encode leaves `actor` off the wire. Decode sets `actor` to `stubActor`.
7. [ ] Expand: Add the `MailboxLog` arm. Add a `placeOrder` arm that stays unsupported. 80 — Place Actor starts through PlaceOrder runs it.
8. [ ] Expand: Add `postActorOrder : ActorOrder -> unit` on `MailboxHost`. It puts one `ActorOrder` on the mailbox queue.
9. [ ] Leave `startFunction`, `startActor`, `startPeerActor`, and `CoreMsg` as they are.
10. [ ] A match of the cancel case uses `Order.CancelActor`.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/Core/CoreChanges.fs`
[ ] `src/Server/Core/CoreDoor.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/MailboxHost.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs` — `ActorOrder round-trips on the wire with stubActor`
[ ] `tests/Shared.Tests/MailboxLogTests.fs` — `ActorOrder carries command name and FocusId`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/06-add-the-function-and-focus-start-stop.md`, then ask.
[ ] Stop when the edit moves `CoreChangesDoor`, `MailboxHost`, or `Caller`.
[ ] Stop when the edit adds a `CoreMsg` case, a pool start, or `Order.RunActor`.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when 112 has not landed.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
  - `tests/Shared.Tests`: ``ActorOrder carries command name and FocusId``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/06-add-the-function-and-focus-start-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
