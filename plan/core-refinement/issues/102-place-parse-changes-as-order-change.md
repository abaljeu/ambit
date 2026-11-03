# 102 — Place Parse changes as Order.Change
**Status**: `coded`
One job: The Server has one mailbox Change post with a `graphOnly` parameter, and `postEvents` and `postGraphOnly` become shells that call it with `graphOnly` `false` and `true`. The reason is decision 1 **Change** in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Change` replaces `CoreMsg.PostEvent` and `CoreMsg.PostGraphOnly`.

Type: coding
Blockers: none ([104 — PlacedOrder is the mailbox envelope](104-placedorder-is-the-mailbox-envelope.md) and [107 — Add EventBody.Order](107-add-eventbody-order.md) are on staging)

## Inputs

### Working

#### The two posts

[ ] ``postEvents`` in `src/Server/Core/CoreMailboxApi.fs` takes `host`, `caller`, and an `Ev list`, and replies `Result<CoreChangesAccepted, string>`.
[ ] ``postEvents`` sends each Change Ev with id zero as `Mailbox.PlaceOrder` through `postedMailbox`, and every other Ev as `CoreMsg.PostEvent`.
[ ] ``postGraphOnly`` in `src/Server/Core/CoreMailboxApi.fs` takes `host`, `caller`, and one `Ev`, and replies `Result<CoreChangesAccepted, string>`.
[ ] ``postGraphOnly`` sends `CoreMsg.PostGraphOnly(caller, event, channel)`.
[ ] Both rebuild `CoreChangesAccepted` through `acceptedFromIds`.
[ ] ``CoreDoor.postEvents`` and ``CoreDoor.postGraphOnly`` in `src/Server/Core/CoreDoor.fs` call them through `callIfReady`.
[ ] No function named `postChange` exists on staging.

#### Where each is handled

[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs` handles `Order.Change(_, ChangeOrigin.Edit)` through `replyEdit`, which calls `CoreEventDispatch.postEvent` with `graphOnly` `false`.
[ ] ``placeOrder`` replies `Error "unsupported"` for `Order.Change(_, ChangeOrigin.Parse)`.
[ ] ``runMsg`` in `src/Server/Core/CoreMailboxLoop.fs` handles `PostGraphOnly` through `CoreMailboxEvents.dispatchPostGraphOnly`, which calls `CoreEventDispatch.postEvent` with `graphOnly` `true`.
[ ] `CoreMsg.PostGraphOnly` in `src/Server/Core/CoreMsg.fs` is matched in `CoreMailboxLoop.runMsg`, `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`.
[ ] ``CoreEventDispatch.postEvent`` in `src/Server/Core/CoreEventDispatch.fs` takes `graphOnly: bool`. `true` skips the document file write and the persist push. Read only.

#### Callers

[ ] `CoreDoor.postEvents` is called from `Api.postEvents` in `src/Server/Api.fs`, `TestActor` in `src/Server/TestActor.fs`, and `RunAgentActor` in `src/Server/RunAgentActor.fs`.
[ ] `CoreDoor.postGraphOnly` is called from `ParseThread.postParseOps` in `src/Server/ParseThread.fs` and from `LazyLoadReconciliationServer` in `src/Server/LazyLoadReconciliationServer.fs` through `GraphOnlyChangePost.postChunks`.
[ ] 33 test files call `postEvents` or `postGraphOnly` on `CoreMailboxApi` or `CoreDoor`. They stay as written.

#### Tests

[ ] ``A graph-only change pushes nothing`` and ``a graph-only change logs no NodeUnpersisted`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``Graph-only post reads authority without repeated admission`` and ``Graph-only post with admitted Caller reaches persist`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``postGraphOnly updates Graph and EventLog without file write`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``Graph-only post does not repeat entry admission`` in `tests/Server.Tests/CoreRuntimeTests.fs`.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decisions 1 and 18.

## Do not load

[ ] The callers of `postEvents` and `postGraphOnly` stay as written.
[ ] Logging `origin` is [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md).

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Add one mailbox Change post with a `graphOnly` parameter in `src/Server/Core/CoreMailboxApi.fs`.
3. [x] Make `postEvents` and `postGraphOnly` shells that keep their signatures and call that post with `graphOnly` `false` and `true`.
4. [x] Add a test that a `graphOnly` post stores the Change without a document file write.

## Outputs

[x] `src/Server/Core/CoreMailboxApi.fs` — one mailbox Change post with a `graphOnly` parameter; `postEvents` and `postGraphOnly` call it
[x] One test in `tests/Server.Tests` that a `graphOnly` post stores the Change without a document file write

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/102-place-parse-changes-as-order-change-stop.md`, then ask.
[ ] Stop when the edit needs a caller of `postEvents` or `postGraphOnly` to change.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: the new `graphOnly` test name written in the test file
  - `tests/Server.Tests`: ``A graph-only change pushes nothing``
  - `tests/Server.Tests`: ``postGraphOnly updates Graph and EventLog without file write``
[x] All executed tests pass.
[x] Write the verdict to `plan/core-refinement/reports/102-place-parse-changes-as-order-change-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
