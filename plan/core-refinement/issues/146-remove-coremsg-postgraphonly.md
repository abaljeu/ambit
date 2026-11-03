# 146 — Remove CoreMsg.PostGraphOnly
**Status**: `defined`
One job: The Server places every graph-only Change, the Changes that ParseThread and reconcile post, as `Order.Change` with origin `Parse` through `Mailbox.PlaceOrder`, and `CoreMsg.PostGraphOnly` is gone. `postGraphOnly` and `postEvents` stay as thin shells over the one mailbox Change post. The reason is decision 1 **Change** and decision 18 **ChangeOrigin** in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Change` replaces `CoreMsg.PostGraphOnly`, and origin `Parse` keeps the file skip `PostGraphOnly` uses today.

Type: coding
Blockers: none. [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md) and [81 — Log Actor stops as Notices](81-log-actor-and-search-stops-as-notices.md) are `coded`. Not blocked by the Actor-start cutover, but it shares files with it: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoop.fs` and `CoreMailboxEvents.fs` with [15 — Delete the old Actor starts](../../actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md), `CoreMailboxLoop.fs` (`placeOrder`) with [06 — Log Order.RunActor](../../actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md), and `CoreMailboxApi.fs` with [09 — Dispatch the query command](../../actor-as-client/issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md). It also shares `CoreMsg.fs`, `CoreMailboxLoop.fs` and `CoreMailboxEvents.fs` with [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). Each ticket touches different cases and arms, so whichever lands second merges staging in.

## Inputs

### Working

#### The one Change post

[ ] ``postMailboxChange`` in `src/Server/Core/CoreMailboxApi.fs:254-271` is the one mailbox Change post. It takes `host`, `caller`, `graphOnly` and an `Ev list`, and replies `Result<CoreChangesAccepted, string>` rebuilt through ``acceptedFromIds``.
[ ] ``postEvents`` (`CoreMailboxApi.fs:275-280`) calls it with `graphOnly` `false`. ``postGraphOnly`` (`CoreMailboxApi.fs:283-288`) calls it with `graphOnly` `true` and one `Ev`. Both are already thin shells and keep their signatures.
[ ] ``changeMessage`` in `CoreMailboxApi.fs:238-251` picks the message per Ev. With `graphOnly` `true` it skips ``clientBatchError`` and builds `Mailbox.CoreMsg(PostGraphOnly(caller, event, channel))` for every Ev. With `graphOnly` `false` it calls ``messageForPosted``, which places a Change with id zero, an Undo, or a Redo as `Mailbox.PlaceOrder` through ``MailboxLog.placedOrderFromEvent`` and ``Mailbox.placeForCaller``, and sends every other Ev as `CoreMsg.PostEvent`.
[ ] ``Mailbox.placeForCaller`` in `src/Server/Core/CoreMsg.fs` sets the `PlacedOrder` authority to the caller's. Read only.
[ ] ``MailboxLog.placedOrderFromEvent`` in `src/Shared/MailboxLog.fs` keeps the body's `Order`, so an Ev built with `EventBody.Change ops` gives `Order.Change(ops, ChangeOrigin.Edit)`. It replies `Error "unsupported"` for a Notice. ``MailboxLog.eventFromPlacedOrder`` builds the Ev with the id it is given. Read only.

#### `CoreMsg.PostGraphOnly`

[ ] `CoreMsg.PostGraphOnly of caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>` in `src/Server/Core/CoreMsg.fs:6-9`. It is built only in ``changeMessage``.
[ ] It is matched in ``runMsg`` (`src/Server/Core/CoreMailboxLoop.fs:15-16`), ``messageCaller`` (`CoreMailboxLoop.fs:69`), ``operationContext`` (`src/Server/Core/CoreMailboxEvents.fs:40-42`) and ``replyFailure`` (`CoreMailboxEvents.fs:61`).
[ ] ``runMsg`` calls ``CoreMailboxEvents.dispatchPostGraphOnly`` (`CoreMailboxEvents.fs:112-126`), which calls ``CoreEventDispatch.postEvent`` with `graphOnly` `true` and replies the stored id. It has no other caller.
[ ] ``dispatch`` in `CoreMailboxLoop.fs` refuses a `CoreMsg` whose caller authority was dropped, with `"authority dropped"`. ``dispatchMailbox`` does the same for `Mailbox.PlaceOrder`.

#### `placeOrder`

[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs:188-207` handles `Order.Change(_, ChangeOrigin.Edit)` through ``replyEdit``, which rebuilds the Ev with origin `Edit` through ``editEvent`` and calls ``replyPosted``. ``replyPosted`` calls `CoreEventDispatch.postEvent` with `graphOnly` `false`.
[ ] ``placeOrder`` replies `Error "unsupported"` through ``replyUnsupported`` for `Order.Change(_, ChangeOrigin.Parse)`, `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave` and `Order.Cancel`.

#### `CoreEventDispatch`, read only

[ ] ``CoreEventDispatch.postEvent`` in `src/Server/Core/CoreEventDispatch.fs:341-349` takes `graphOnly: bool`. A stored `submissionId` replies the stored Ev. Otherwise ``persistNew`` refuses an Ev whose id is not zero with `"posted EventId must be zero"`.
[ ] With `graphOnly` `true`, `persistNew` stores the Change with origin `Parse` through ``withParseOrigin``, skips the document file write in ``persist``, and skips ``markAcceptedSpecials``, so it logs no `NodeUnpersisted` and pushes nothing.
[ ] ``completeOther`` refuses a Notice body with `"Notice is not a client event type"`, so a graph-only post of an Actor stop is refused today.
[ ] Today a graph-only post of an Undo or a Redo is accepted through ``completeUndoRedo``. No caller posts one.

#### Callers, unchanged

[ ] ``CoreDoor.postGraphOnly`` in `src/Server/Core/CoreDoor.fs` calls `CoreMailboxApi.postGraphOnly` through ``callIfReady``.
[ ] ``ParseThread.postParseOps`` in `src/Server/ParseThread.fs` and `LazyLoadReconciliationServer` in `src/Server/LazyLoadReconciliationServer.fs` (through ``GraphOnlyChangePost.postChunks``) call `CoreDoor.postGraphOnly`. ``GraphOnlyChangePost.mint`` in `src/Server/GraphOnlyChangePost.fs` builds an Ev with id zero, authority `Parse` and body `EventBody.Change ops`.
[ ] 14 test files name `postGraphOnly`. None builds `CoreMsg.PostGraphOnly`.

#### Tests

[ ] ``postGraphOnly updates Graph and EventLog without file write`` and ``a graphOnly post stores the Change without a document file write`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``postEvent rejects a non-zero EventId on a new client Event`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` is the model for the refusal test.
[ ] ``A graph-only change pushes nothing``, ``a graph-only change logs no NodeUnpersisted``, ``Graph-only post reads authority without repeated admission``, ``Graph-only post with admitted Caller reaches persist`` and ``a stored Parse Change keeps origin Parse`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. Read only.
[ ] ``Graph-only post does not repeat entry admission`` in `tests/Server.Tests/CoreRuntimeTests.fs`. Read only.
[ ] ``unionCaseNames`` and ``SetDocumentState leaves the Op list`` in `tests/Server.Tests/MailboxExpandTests.fs` read the cases of `CoreMsg`.
[ ] `MailboxHost.postMailboxAndAsyncReply` is how a test posts a `Mailbox` message directly. `tests/Server.Tests/SessionTests.fs` uses it. Read only.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decision 1 **Change** and decision 18 **ChangeOrigin**. Decision 18 names `Api.applyParseFile`, which is not on staging.
[ ] Alan: `postEvents` and `postGraphOnly` are kept only as thin shells that call the single merged mailbox post. `ChangeOrigin.Parse` must be supported.
[ ] [Cutover overlap](plan/actor-as-client/reports/cutover-overlap.md).

## Do not load

[ ] `src/Server/Core/CoreEventDispatch.fs` stays as written. Making it read `ChangeOrigin` in place of the `graphOnly` flag is not this ticket.
[ ] The callers of `postGraphOnly` and `postEvents`, and `CoreDoor`, stay as written.
[ ] Every other `CoreMsg` case. `CoreMsg.CancelActor` is [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` are [15 — Delete the old Actor starts](../../actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md). `CoreMsg.RecordSearchStart` is [09 — Dispatch the query command](../../actor-as-client/issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md). `CoreMsg.ActorStop` is [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md).
[ ] The `placeOrder` arms for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave` and `Order.Cancel`. The Actor-start cutover tickets 06–15 in `plan/actor-as-client/issues` and 112 change them.
[ ] `plan/actor-as-client/**` and `plan/core-refinement/issues/80-place-actor-orders-through-placeorder.md`. The cutover owns them.
[ ] `src/Shared/ResidentProjection.fs`, `src/Shared/ViewModelDomPlan.fs`, `tests/Shared.Tests/WantTests.fs`, `tests/Shared.Tests/ViewModelTests.fs` and `tests/Shared.Tests/SyncLogicTests.fs`. Another ticket is changing them.
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` and `tests/Server.Tests/CoreRuntimeTests.fs` stay as written. Cutover tickets 11 and 14 move tests in them.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] `postMailboxChange` with `graphOnly` `true` places each Ev that is a Change with id zero as `Mailbox.PlaceOrder` of `Order.Change(ops, ChangeOrigin.Parse)`, with the Ev's `submissionId` as the order id and the caller's authority.
3. [ ] `placeOrder` stores `Order.Change(_, ChangeOrigin.Parse)` the way `CoreMsg.PostGraphOnly` stores a Change today. The stored Change keeps origin `Parse`. It writes no document file, logs no `NodeUnpersisted` and pushes nothing. A resubmitted `submissionId` replies the stored id. It replies the stored `EventId`.
4. [ ] A graph-only post of an Ev that is not a Change with id zero is refused with an `Error` and stores nothing. A non-zero id is refused with `"posted EventId must be zero"`, as today. A Notice, an Undo, a Redo, or any other Order is refused.
5. [ ] A graph-only post from a caller whose authority was dropped is refused with `"authority dropped"`, as today.
6. [ ] `CoreMsg.PostGraphOnly` leaves `CoreMsg`, with its arms in `runMsg`, `messageCaller`, `operationContext` and `replyFailure`, and `CoreMailboxEvents.dispatchPostGraphOnly`. No `CoreMsg` case is added.
7. [ ] `postGraphOnly` and `postEvents` keep their signatures and stay thin shells over `postMailboxChange`. The `postEvents` path does not change.
8. [ ] Add ``a placed Parse Change is stored without a document file write`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It posts `Mailbox.PlaceOrder` of `Order.Change(ops, ChangeOrigin.Parse)` through `MailboxHost.postMailboxAndAsyncReply`. The reply is `Ok`, the stored Change with that `submissionId` keeps origin `Parse`, the graph holds the new text, and the File's document file does not exist.
9. [ ] Add ``a graph-only post of a Notice is refused and stores nothing`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It posts an Ev of `Notice.ActorStopped` through `postGraphOnly`. The reply is `Error`, and the log holds no Ev with that `submissionId`.
10. [ ] Add ``a graph-only post with a non-zero EventId is refused`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. The reply is `Error "posted EventId must be zero"`.
11. [ ] Add ``CoreMsg has no PostGraphOnly case`` to `tests/Server.Tests/MailboxExpandTests.fs`, through ``unionCaseNames``.
12. [ ] The new tests do not count events, assert event ids, or use time. Existing tests keep their names and assertions.

## Outputs

[ ] `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMsg.fs`. 4 source files.
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` — ``a placed Parse Change is stored without a document file write``, ``a graph-only post of a Notice is refused and stores nothing``, ``a graph-only post with a non-zero EventId is refused``.
[ ] `tests/Server.Tests/MailboxExpandTests.fs` — ``CoreMsg has no PostGraphOnly case``. 2 test files.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/146-remove-coremsg-postgraphonly-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit needs a file under Do not load.
[ ] Stop when the edit needs a caller of `postGraphOnly` or `postEvents` to change.
[ ] Stop when an existing test needs a new name or assertion.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``a placed Parse Change is stored without a document file write``
  - `tests/Server.Tests`: ``a graph-only post of a Notice is refused and stores nothing``
  - `tests/Server.Tests`: ``a graph-only post with a non-zero EventId is refused``
  - `tests/Server.Tests`: ``CoreMsg has no PostGraphOnly case``
  - `tests/Server.Tests`: ``postGraphOnly updates Graph and EventLog without file write``
  - `tests/Server.Tests`: ``a graphOnly post stores the Change without a document file write``
  - `tests/Server.Tests`: ``A graph-only change pushes nothing``
  - `tests/Server.Tests`: ``a graph-only change logs no NodeUnpersisted``
  - `tests/Server.Tests`: ``Graph-only post reads authority without repeated admission``
  - `tests/Server.Tests`: ``Graph-only post with admitted Caller reaches persist``
  - `tests/Server.Tests`: ``a stored Parse Change keeps origin Parse``
  - `tests/Server.Tests`: ``Graph-only post does not repeat entry admission``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/146-remove-coremsg-postgraphonly-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
