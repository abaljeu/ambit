# 102 — Place Parse changes as Order.Change
**Status**: `defined`
One job: The parse door places `Order.Change` with origin `Parse` through `placeOrder`, so a parse Change lands in Graph and EventLog without a document file write, and `CoreMsg.PostGraphOnly` leaves. The reason is decisions 1 **Change** and 18 **ChangeOrigin** in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Change` replaces `CoreMsg.PostGraphOnly`, and origin `Parse` keeps the file skip `PostGraphOnly` uses today.

Type: coding
Blockers: none ([104 — PlacedOrder is the mailbox envelope](104-placedorder-is-the-mailbox-envelope.md) and [107 — Add EventBody.Order](107-add-eventbody-order.md) are on staging)

## Inputs

### Working

[ ] ``postGraphOnly`` in `src/Server/Core/CoreMailboxApi.fs` takes `host`, `caller`, and a History `Ev`, and replies `Result<CoreChangesAccepted, string>`.
[ ] ``postGraphOnly`` posts `CoreMsg.PostGraphOnly(caller, event, channel)` through `reply`. It carries an `Ev`, not an `Order`.
[ ] ``postGraphOnly`` rebuilds `CoreChangesAccepted` through `acceptedFromIds host [ event ] [ eventId ]`.
[ ] ``postedMailbox`` in `src/Server/Core/CoreMailboxApi.fs` reads the `PlacedOrder` of a Change Ev through `MailboxLog.ofEv` and posts `Mailbox.placeForCaller placed caller channel`. It is the Edit precedent from [64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md).
[ ] ``Mailbox.placeForCaller`` in `src/Server/Core/CoreMsg.fs` builds `Mailbox.PlaceOrder` with `authority = caller.authority`.
[ ] ``CoreMsg.PostGraphOnly`` in `src/Server/Core/CoreMsg.fs` is `caller: Caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs` routes `Order.Change(ops, ChangeOrigin.Edit)` to `replyEdit`.
[ ] ``placeOrder`` replies `Error "unsupported"` through `replyUnsupported` for `Order.Change(_, ChangeOrigin.Parse)`.
[ ] ``editEvent`` in `src/Server/Core/CoreMailboxLoop.fs` builds the Ev through `MailboxLog.toEv` with `Order.Change(ops, ChangeOrigin.Edit)`.
[ ] ``replyEdit`` in `src/Server/Core/CoreMailboxLoop.fs` calls `CoreEventDispatch.postEvent` with `graphOnly` `false` and replies `Ok stored.id`.
[ ] ``runMsg`` in `src/Server/Core/CoreMailboxLoop.fs` matches `PostGraphOnly` and calls `CoreMailboxEvents.dispatchPostGraphOnly`.
[ ] ``messageCaller`` in `src/Server/Core/CoreMailboxLoop.fs` matches `PostGraphOnly`.
[ ] ``dispatchMailbox`` in `src/Server/Core/CoreMailboxLoop.fs` runs `CoreMsg` through `dispatch`, inside a `try`. It calls `placeOrder` outside that `try`, after `authorityHeld`.
[ ] ``dispatchPostGraphOnly`` in `src/Server/Core/CoreMailboxEvents.fs` calls `CoreEventDispatch.postEvent` with `graphOnly` `true` and replies `Ok stored.id`.
[ ] ``operationContext`` and ``replyFailure`` in `src/Server/Core/CoreMailboxEvents.fs` each match `PostGraphOnly`.
[ ] ``CoreEventDispatch.postEvent`` in `src/Server/Core/CoreEventDispatch.fs` takes `graphOnly: bool`. `true` skips the persist push and the special-node marks, and passes `true` to `persist.applyEvent`. Read only.
[ ] ``MailboxLog.ofEv`` in `src/Shared/MailboxLog.fs` maps `EventBody.Change ops` to `Order.Change(ops, ChangeOrigin.Edit)`, with `orderId` from `submissionId` and `commandName` from the Ev. Read only.
[ ] ``MailboxLog.toEv`` in `src/Shared/MailboxLog.fs` maps `Order.Change(ops, _)` to `EventBody.Change ops`. A Parse Change is logged as a Change, as today. Read only.
[ ] ``Ev.ops`` in `src/Shared/History.fs` gives `Some ops` for a Change Ev. Read only.
[ ] `CoreDoor.postGraphOnly` in `src/Server/Core/CoreDoor.fs` calls `CoreMailboxApi.postGraphOnly`. Read only.
[ ] `ParseThread.postParseOps` in `src/Server/ParseThread.fs` calls `CoreDoor.postGraphOnly` with `GraphOnlyChangePost.mint "Parse" ops`. Read only.
[ ] `LazyLoadReconciliationServer` in `src/Server/LazyLoadReconciliationServer.fs` calls `GraphOnlyChangePost.postChunks (CoreDoor.postGraphOnly listed.handle) "Parse"`. Read only.
[ ] ``mint`` in `src/Server/GraphOnlyChangePost.fs` builds an Ev with `EventBody.Change ops`, `EventId.zero`, and `Authority "Parse"`. Read only.
[ ] `CoreMsg.PostGraphOnly` is built only in `CoreMailboxApi.postGraphOnly`. No test names it.
[ ] 14 test files call `CoreMailboxApi.postGraphOnly`. `tests/Server.Tests/CoreRuntimeTests.fs` also calls `CoreDoor.postGraphOnly`. The signature stays, so they stay.
[ ] ``A graph-only change pushes nothing`` and ``a graph-only change logs no NodeUnpersisted`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``Graph-only post reads authority without repeated admission`` and ``Graph-only post with admitted Caller reaches persist`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``postEvents places Order.Change with origin Edit`` and ``a resubmitted Change replies the stored id`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] `hostRecordingPush`, `fileWithText`, `editFileText`, and `acceptPosted` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. `acceptPosted` ignores its `graphOnly` argument.
[ ] ``postGraphOnly updates Graph and EventLog without file write`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``Graph-only post does not repeat entry admission`` and ``CoreRuntime seeds a Parse process Caller distinct from Browser cookie`` in `tests/Server.Tests/CoreRuntimeTests.fs`.
[ ] `latePlace` in `tests/Server.Tests/SessionTests.fs` places an Order through `Mailbox.placeForCaller` and `MailboxHost.postMailboxAndAsyncReply`. Read only.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decisions 1 and 18.
[ ] [TDD](.agents/skills/tdd/SKILL.md) "No time as a mechanism".

## Do not load

[ ] `src/Shared/MailboxLog.fs`, `src/Shared/History.fs` and `src/Shared/EventJson.fs` stay. Logging `origin` is [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md).
[ ] `src/Server/Core/CoreEventDispatch.fs` stays. Its `graphOnly` flag stays.
[ ] `CoreDoor`, `ParseThread`, `LazyLoadReconciliationServer` and `GraphOnlyChangePost` stay. They keep calling `postGraphOnly`.
[ ] `FileAgent`, `DbAgent` and `ChangeAmendment` stay.
[ ] `CoreMsg.PostEvent` and the other `CoreMsg` cases stay.
[ ] The test files that call `postGraphOnly` stay as written, except `tests/Server.Tests/CoreMailboxDoorTests.fs`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `src/Server/Core/CoreMailboxLoop.fs`: `editEvent` becomes `changeEvent placed ops origin` and builds `Order.Change(ops, origin)`.
3. [ ] Change `src/Server/Core/CoreMailboxLoop.fs`: `replyEdit` becomes `replyChange context placed caller reply ops origin`. It passes `graphOnly` `true` to `CoreEventDispatch.postEvent` for origin `Parse`, and `false` for origin `Edit`.
4. [ ] Change `src/Server/Core/CoreMailboxLoop.fs`: `placeOrder` routes `Order.Change(ops, origin)` to `replyChange`. The `Order.Change(_, ChangeOrigin.Parse)` arm leaves the `replyUnsupported` list.
5. [ ] Change `src/Server/Core/CoreMailboxApi.fs`: `postGraphOnly` keeps its signature. An Ev where `Ev.ops` is `None` replies `Error "graph-only post needs a Change"`.
6. [ ] Change `src/Server/Core/CoreMailboxApi.fs`: `postGraphOnly` reads `placed` through `MailboxLog.ofEv event`. It posts `Mailbox.placeForCaller { placed with order = Order.Change(ops, ChangeOrigin.Parse) } caller channel` through `MailboxHost.postMailboxAndAsyncReply`. An `ofEv` `Error` is the reply.
7. [ ] Keep `src/Server/Core/CoreMailboxApi.fs`: `postGraphOnly` rebuilds `CoreChangesAccepted` through `acceptedFromIds host [ event ] [ eventId ]`.
8. [ ] Contract `src/Server/Core/CoreMailboxLoop.fs`: remove the `PostGraphOnly` arms from `runMsg` and `messageCaller`.
9. [ ] Contract `src/Server/Core/CoreMailboxEvents.fs`: remove `dispatchPostGraphOnly`, and the `PostGraphOnly` arms from `operationContext` and `replyFailure`.
10. [ ] Contract `src/Server/Core/CoreMsg.fs`: remove `CoreMsg.PostGraphOnly`.
11. [ ] Add ``a placed Parse Change is stored without a file write`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`:
    - place `Order.Change(ops, ChangeOrigin.Parse)` through `Mailbox.placeForCaller` and `MailboxHost.postMailboxAndAsyncReply`, with the ops of `editFileText`
    - the host records each `graphOnly` that `applyEvent` receives, and each `pushPersist`
    - the reply is `Ok` of an accepted id
    - `getEventsSince` holds a Change at that id with the order's `submissionId`
    - `applyEvent` received `true`
    - nothing was pushed

## Outputs

[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — add ``a placed Parse Change is stored without a file write``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/102-place-parse-changes-as-order-change-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when a test file needs an edit beyond the one in Outputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``a placed Parse Change is stored without a file write``
  - `tests/Server.Tests`: ``A graph-only change pushes nothing``
  - `tests/Server.Tests`: ``a graph-only change logs no NodeUnpersisted``
  - `tests/Server.Tests`: ``Graph-only post reads authority without repeated admission``
  - `tests/Server.Tests`: ``Graph-only post with admitted Caller reaches persist``
  - `tests/Server.Tests`: ``postEvents places Order.Change with origin Edit``
  - `tests/Server.Tests`: ``a resubmitted Change replies the stored id``
  - `tests/Server.Tests`: ``postGraphOnly updates Graph and EventLog without file write``
  - `tests/Server.Tests`: ``Graph-only post does not repeat entry admission``
  - `tests/Server.Tests`: ``CoreRuntime seeds a Parse process Caller distinct from Browser cookie``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/102-place-parse-changes-as-order-change-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
