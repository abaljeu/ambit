# 146 — Remove CoreMsg.PostGraphOnly
**Status**: `coded`
One job: The Server places every graph-only Change, the Changes that ParseThread and reconcile post, as `Order.Change` with origin `Parse` through `Mailbox.PlaceOrder`, and `CoreMsg.PostGraphOnly` is gone. The core dispatch reads the Change's `ChangeOrigin` to choose the graph-only behaviour, so the `graphOnly` flag leaves the mailbox post and `CoreEventDispatch`. `postGraphOnly` and `postEvents` stay as thin shells over the one mailbox Change post. The reason is decision 1 **Change** and decision 18 **ChangeOrigin** in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Change` replaces `CoreMsg.PostGraphOnly`, `CoreEventDispatch` reads the `origin`, and origin `Parse` keeps the file skip `PostGraphOnly` uses today.

Type: coding
Blockers: none. [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md) and [81 — Log Actor stops as Notices](81-log-actor-and-search-stops-as-notices.md) are `coded`. Not blocked by the Actor-start cutover, but it shares files with it: `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoop.fs`, `CoreMailboxEvents.fs` and `CoreMailboxActors.fs` with [15 — Delete the old Actor starts](../../actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md), `CoreMailboxLoop.fs` (`placeOrder`) with [06 — Log Order.RunActor](../../actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md), `CoreMailboxApi.fs` and `CoreMailboxActors.fs` with [09 — Dispatch the query command](../../actor-as-client/issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md), `CoreMailboxActors.fs` with [07 — Dispatch the test command](../../actor-as-client/issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) and [08 — Dispatch the AI Actor](../../actor-as-client/issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md), and `tests/Server.Tests/CoreMailboxDoorTests.fs` with [11 — Move door and case tests](../../actor-as-client/issues/11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md). It also shares `CoreMsg.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoop.fs`, `CoreMailboxEvents.fs` and `CoreMailboxActors.fs` with [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). `CoreEventDispatch.fs` is in no cutover ticket. Each ticket touches different cases, arms and arguments, so whichever lands second merges staging in.

## Inputs

### Working

#### The one Change post

[ ] ``postMailboxChange`` in `src/Server/Core/CoreMailboxApi.fs:254-271` is the one mailbox Change post. It takes `host`, `caller`, `graphOnly: bool` and an `Ev list`, and replies `Result<CoreChangesAccepted, string>` rebuilt through ``acceptedFromIds``.
[ ] ``postEvents`` (`CoreMailboxApi.fs:275-280`) calls it with `graphOnly` `false`. ``postGraphOnly`` (`CoreMailboxApi.fs:283-288`) calls it with `graphOnly` `true` and one `Ev`. Both are already thin shells and keep their names and signatures.
[ ] ``changeMessage`` in `CoreMailboxApi.fs:238-251` picks the message per Ev. With `graphOnly` `true` it skips ``clientBatchError`` and builds `Mailbox.CoreMsg(PostGraphOnly(caller, event, channel))` for every Ev. With `graphOnly` `false` it calls ``messageForPosted``, which places a Change with id zero, an Undo, or a Redo as `Mailbox.PlaceOrder` through ``MailboxLog.placedOrderFromEvent`` and ``Mailbox.placeForCaller``, and sends every other Ev as `CoreMsg.PostEvent`.
[ ] ``Mailbox.placeForCaller`` in `src/Server/Core/CoreMsg.fs` sets the `PlacedOrder` authority to the caller's. Read only.
[ ] ``MailboxLog.placedOrderFromEvent`` in `src/Shared/MailboxLog.fs` keeps the body's `Order` and its origin. An Ev built with `EventBody.Change ops` gives `Order.Change(ops, ChangeOrigin.Edit)`. `EventJson` decodes origin `parse`, so a posted Change can carry origin `Parse`. It replies `Error "unsupported"` for a Notice. Read only.
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs:376` calls `postMailboxChange host testCaller true [ event ]`. It is the only caller outside `CoreMailboxApi.fs`.

#### `CoreMsg.PostGraphOnly`

[ ] `CoreMsg.PostGraphOnly of caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>` in `src/Server/Core/CoreMsg.fs:6-9`. It is built only in ``changeMessage``.
[ ] It is matched in ``runMsg`` (`src/Server/Core/CoreMailboxLoop.fs:15-16`), ``messageCaller`` (`CoreMailboxLoop.fs:69`), ``operationContext`` (`src/Server/Core/CoreMailboxEvents.fs:40-42`) and ``replyFailure`` (`CoreMailboxEvents.fs:61`).
[ ] ``runMsg`` calls ``CoreMailboxEvents.dispatchPostGraphOnly`` (`CoreMailboxEvents.fs:112-126`), which calls ``CoreEventDispatch.postEvent`` with `true` and replies the stored id. It has no other caller.
[ ] ``dispatch`` in `CoreMailboxLoop.fs` refuses a `CoreMsg` whose caller authority was dropped, with `"authority dropped"`. ``dispatchMailbox`` does the same for `Mailbox.PlaceOrder`.

#### `placeOrder`

[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs:188-207` handles `Order.Change(_, ChangeOrigin.Edit)` through ``replyEdit``, which rebuilds the Ev with origin `Edit` through ``editEvent`` and calls ``replyPosted``. ``replyPosted`` (`CoreMailboxLoop.fs:151-165`) calls `CoreEventDispatch.postEvent` with `false`.
[ ] ``placeOrder`` replies `Error "unsupported"` through ``replyUnsupported`` for `Order.Change(_, ChangeOrigin.Parse)`, `Order.StartActor`, and `Order.Cancel`. A Change of origin `Parse` posted through `postEvents` is refused this way today.

#### `CoreEventDispatch` and the flag

[ ] ``CoreEventDispatch.postEvent`` in `src/Server/Core/CoreEventDispatch.fs:341-349` takes `graphOnly: bool` and passes it to ``persistNew`` (`CoreEventDispatch.fs:317-339`). A stored `submissionId` replies the stored Ev. Otherwise `persistNew` refuses an Ev whose id is not zero with `"posted EventId must be zero"`.
[ ] Inside `persistNew` the flag drives three things: ``withParseOrigin`` (`CoreEventDispatch.fs:305-315`) rewrites the Change's origin to `Parse`; ``persist`` (`CoreEventDispatch.fs:275-280`) passes it to `context.persist.applyEvent`, which skips the document file write; ``markAcceptedSpecials`` (`CoreEventDispatch.fs:291-303`) skips the `NodeUnpersisted` mark and the persist push.
[ ] ``completeOther`` refuses a Notice body with `"Notice is not a client event type"`, so a graph-only post of an Actor stop is refused today.
[ ] Today a graph-only post of an Undo or a Redo is accepted through ``completeUndoRedo``. No caller posts one.
[ ] `CoreEventDispatch.postEvent` callers: ``replyPosted`` in `CoreMailboxLoop.fs`, ``dispatchPostGraphOnly`` in `CoreMailboxEvents.fs`, ``dispatchPostEvent`` in `src/Server/Core/CoreMailboxActors.fs:235-258` (passes `false`), and ``runOrdinaryEdit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs:961` (passes `false`).
[ ] `CoreMailboxAccess.applyEvent: Ev -> bool -> Result<CoreChangesAccepted, string>` in `src/Server/Core/CoreMsg.fs:96`. `FileAgent`, `DbAgent`, ``failedPersist`` in `CoreMailboxLoop.fs`, and the test handlers in `TestBackend.cannedHandlers`, `PersistApplyTests.fs`, `PersistThreadTests.fs` and `CoreLoopNoticeTests.fs` take or pass that `bool`. It stays as written; see Do not load.
[ ] ``TestBackend.cannedHandlers`` in `tests/Server.Tests/TestBackend.fs` builds `CoreMailboxAccess` from a given `apply: Ev -> bool -> ...`. ``runOrdinaryEdit`` in `CoreMailboxDoorTests.fs` is the model for calling `CoreEventDispatch.postEvent` with a `CoreEventDispatch.Context` directly. Read only.

#### Callers, unchanged

[ ] ``CoreDoor.postGraphOnly`` in `src/Server/Core/CoreDoor.fs` calls `CoreMailboxApi.postGraphOnly` through ``callIfReady``.
[ ] ``ParseThread.postParseOps`` in `src/Server/ParseThread.fs` and `LazyLoadReconciliationServer` in `src/Server/LazyLoadReconciliationServer.fs` (through ``GraphOnlyChangePost.postChunks``) call `CoreDoor.postGraphOnly`. ``GraphOnlyChangePost.mint`` in `src/Server/GraphOnlyChangePost.fs` builds an Ev with id zero, authority `Parse` and body `EventBody.Change ops`.
[ ] 14 test files name `postGraphOnly`. None builds `CoreMsg.PostGraphOnly`.

#### Tests

[ ] ``postGraphOnly updates Graph and EventLog without file write`` and ``a graphOnly post stores the Change without a document file write`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``postEvent rejects a non-zero EventId on a new client Event`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` is the model for the refusal tests.
[ ] ``A graph-only change pushes nothing``, ``a graph-only change logs no NodeUnpersisted``, ``Graph-only post reads authority without repeated admission``, ``Graph-only post with admitted Caller reaches persist``, ``a stored Parse Change keeps origin Parse`` and ``a stored Edit Change keeps origin Edit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``Graph-only post does not repeat entry admission`` in `tests/Server.Tests/CoreRuntimeTests.fs`. Read only.
[ ] ``unionCaseNames`` and ``SetDocumentState leaves the Op list`` in `tests/Server.Tests/MailboxExpandTests.fs` read the cases of `CoreMsg`.
[ ] `MailboxHost.postMailboxAndAsyncReply` is how a test posts a `Mailbox` message directly. `tests/Server.Tests/SessionTests.fs` uses it. Read only.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decision 1 **Change** and decision 18 **ChangeOrigin**.
[ ] Alan, 2026-11-03: `postEvents` and `postGraphOnly` are kept only as thin shells that call the single merged mailbox post. `ChangeOrigin.Parse` must be supported.
[ ] Alan, 2026-11-03: dispatch reads the origin, not a `graphOnly` flag.
[ ] Alan, 2026-11-03: refusing every graph-only post that is not a Change, Undo and Redo included, is accepted for now. It may change later.
[ ] [Cutover overlap](plan/actor-as-client/reports/cutover-overlap.md).

## Do not load

[ ] `CoreMailboxAccess.applyEvent` keeps its `bool`, and `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `tests/Server.Tests/TestBackend.fs`, `tests/Server.Tests/PersistApplyTests.fs`, `tests/Server.Tests/PersistThreadTests.fs` and `tests/Server.Tests/CoreLoopNoticeTests.fs` stay as written. `CoreEventDispatch` computes that `bool` from the origin. Removing it from the persist handlers needs those six files and goes over the test-file limit.
[ ] The callers of `postGraphOnly` and `postEvents`, and `CoreDoor`, stay as written.
[ ] Every other `CoreMsg` case. `CoreMsg.CancelActor` is [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` are [15 — Delete the old Actor starts](../../actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md). `CoreMsg.RecordSearchStart` is [09 — Dispatch the query command](../../actor-as-client/issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md). `CoreMsg.ActorStop` is [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md).
[ ] The `placeOrder` arms for `Order.StartActor` and `Order.Cancel`, and every arm of `dispatchPostEvent` in `CoreMailboxActors.fs` except the argument it passes to `CoreEventDispatch.postEvent`. The cutover tickets and 112 change them.
[ ] Every test in `tests/Server.Tests/CoreMailboxDoorTests.fs`. Only the `CoreEventDispatch.postEvent` call in ``runOrdinaryEdit`` changes. Cutover ticket 11 moves tests in that file.
[ ] `plan/actor-as-client/**` and `plan/core-refinement/issues/80-place-actor-orders-through-placeorder.md`. The cutover owns them.
[ ] `src/Shared/ResidentProjection.fs`, `src/Shared/ViewModelDomPlan.fs`, `tests/Shared.Tests/WantTests.fs`, `tests/Shared.Tests/ViewModelTests.fs` and `tests/Shared.Tests/SyncLogicTests.fs`. Another ticket is changing them.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `CoreEventDispatch.postEvent` takes no `graphOnly` argument. For a Change it reads the Change's `ChangeOrigin`. Origin `Parse` skips the document file write, the `NodeUnpersisted` mark and the persist push, as `graphOnly` `true` does today. Origin `Edit`, an Undo and a Redo keep today's `graphOnly` `false` behaviour. The stored Change keeps the origin it arrived with. `withParseOrigin` leaves.
3. [x] Every caller of `CoreEventDispatch.postEvent` drops the argument: `replyPosted` in `CoreMailboxLoop.fs`, `dispatchPostEvent` in `CoreMailboxActors.fs`, and `runOrdinaryEdit` in `CoreMailboxDoorTests.fs`.
4. [x] `postMailboxChange` takes a `ChangeOrigin` in place of `graphOnly`. `postEvents` passes `ChangeOrigin.Edit` and `postGraphOnly` passes `ChangeOrigin.Parse`. Both keep their names and signatures and stay thin shells. The call in `Issue41CoreMailboxTests.fs:376` passes `ChangeOrigin.Parse` in place of `true`; that test keeps its name and assertions.
5. [x] A post of origin `Parse` places each Ev that is a Change with id zero as `Mailbox.PlaceOrder` of `Order.Change(ops, ChangeOrigin.Parse)`, with the Ev's `submissionId` as the order id and the caller's authority.
6. [x] `placeOrder` stores `Order.Change(_, ChangeOrigin.Parse)` through `CoreEventDispatch.postEvent`, as it stores origin `Edit`. A resubmitted `submissionId` replies the stored id. It replies the stored `EventId`.
7. [x] A post of origin `Parse` of an Ev that is not a Change with id zero is refused with an `Error` and stores nothing. A non-zero id is refused with `"posted EventId must be zero"`, as today. A Notice, an Undo, a Redo, or any other Order is refused. This refusal may change later.
8. [x] A post of origin `Edit` of a Change whose body carries origin `Parse` is refused and stores nothing, as today. Only `postGraphOnly` places origin `Parse`.
9. [x] A graph-only post from a caller whose authority was dropped is refused with `"authority dropped"`, as today.
10. [x] `CoreMsg.PostGraphOnly` leaves `CoreMsg`, with its arms in `runMsg`, `messageCaller`, `operationContext` and `replyFailure`, and `CoreMailboxEvents.dispatchPostGraphOnly`. No `CoreMsg` case is added.
11. [x] Add ``dispatch takes the graph-only round from origin Parse`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It calls `CoreEventDispatch.postEvent` directly with a `CoreEventDispatch.Context` built from `TestBackend.cannedHandlers` that records the `bool` given to `applyEvent` and every `pushPersist`. An Ev of `Order.Change(ops, ChangeOrigin.Parse)` editing a File's text gives `applyEvent` `true` and pushes nothing. The same edit with origin `Edit` gives `applyEvent` `false`.
12. [x] Add ``a placed Parse Change is stored without a document file write`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It posts `Mailbox.PlaceOrder` of `Order.Change(ops, ChangeOrigin.Parse)` through `MailboxHost.postMailboxAndAsyncReply`. The reply is `Ok`, the stored Change with that `submissionId` keeps origin `Parse`, the graph holds the new text, and the File's document file does not exist.
13. [x] Add ``a graph-only post of a Notice is refused and stores nothing`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It posts an Ev of `Notice.ActorStopped` through `postGraphOnly`. The reply is `Error`, and the log holds no Ev with that `submissionId`.
14. [x] Add ``a graph-only post with a non-zero EventId is refused`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. The reply is `Error "posted EventId must be zero"`.
15. [x] Add ``postEvents refuses a Change of origin Parse`` to `tests/Server.Tests/Issue41CoreMailboxTests.fs`. It posts through `postEvents` an Ev whose body is `Order.Change(ops, ChangeOrigin.Parse)`. The reply is `Error`, and the log holds no Ev with that `submissionId`.
16. [x] Add ``CoreMsg has no PostGraphOnly case`` to `tests/Server.Tests/MailboxExpandTests.fs`, through ``unionCaseNames``.
17. [x] The new tests do not count events, assert event ids, or use time. Existing tests keep their names and assertions.

## Outputs

[x] `src/Server/Core/CoreEventDispatch.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMailboxActors.fs`, `src/Server/Core/CoreMsg.fs`. 6 source files.
[x] `tests/Server.Tests/Issue41CoreMailboxTests.fs` — ``dispatch takes the graph-only round from origin Parse``, ``a placed Parse Change is stored without a document file write``, ``a graph-only post of a Notice is refused and stores nothing``, ``a graph-only post with a non-zero EventId is refused``, ``postEvents refuses a Change of origin Parse``; the `postMailboxChange` argument at line 376.
[x] `tests/Server.Tests/MailboxExpandTests.fs` — ``CoreMsg has no PostGraphOnly case``.
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs` — the `CoreEventDispatch.postEvent` call in ``runOrdinaryEdit`` drops its argument. 3 test files.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/146-remove-coremsg-postgraphonly-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit needs a file under Do not load.
[ ] Stop when the edit needs a caller of `postGraphOnly` or `postEvents` to change.
[ ] Stop when an existing test needs a new name or assertion.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``dispatch takes the graph-only round from origin Parse``
  - `tests/Server.Tests`: ``a placed Parse Change is stored without a document file write``
  - `tests/Server.Tests`: ``a graph-only post of a Notice is refused and stores nothing``
  - `tests/Server.Tests`: ``a graph-only post with a non-zero EventId is refused``
  - `tests/Server.Tests`: ``postEvents refuses a Change of origin Parse``
  - `tests/Server.Tests`: ``CoreMsg has no PostGraphOnly case``
  - `tests/Server.Tests`: ``postGraphOnly updates Graph and EventLog without file write``
  - `tests/Server.Tests`: ``a graphOnly post stores the Change without a document file write``
  - `tests/Server.Tests`: ``A graph-only change pushes nothing``
  - `tests/Server.Tests`: ``a graph-only change logs no NodeUnpersisted``
  - `tests/Server.Tests`: ``Graph-only post reads authority without repeated admission``
  - `tests/Server.Tests`: ``Graph-only post with admitted Caller reaches persist``
  - `tests/Server.Tests`: ``a stored Parse Change keeps origin Parse``
  - `tests/Server.Tests`: ``a stored Edit Change keeps origin Edit``
  - `tests/Server.Tests`: ``Graph-only post does not repeat entry admission``
[x] All executed tests pass.
[x] Write the verdict to `plan/core-refinement/reports/146-remove-coremsg-postgraphonly-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
