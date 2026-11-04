# 112 — Place Cancel through PlaceOrder
**Status**: `coded`
One job: The Server places a cancel by focus as `Order.Cancel` through `Mailbox.PlaceOrder`, and `CoreMsg.CancelActor` leaves. The reason is decision 7 **Cancel** and decision 19 **One case each** in [Message shape](plan/core-refinement/cqrs-messages.md): `Order.Cancel` of the focus `NodeId` replaces `CoreMsg.CancelActor`.

Type: coding
Blockers: none. It lands before the Actor start cutover, tickets 06–15 in [Actor as client](plan/actor-as-client/project.md).

## Inputs

### Working

[x] `CoreMsg.CancelActor` in `src/Server/Core/CoreMsg.fs:40` is `Caller * NodeId * AsyncReplyChannel<Result<unit, string>>`.
[x] ``cancelByFocus`` in `src/Server/Core/CoreMailboxApi.fs:363` builds it. Its only caller is ``cancelFocus`` in `tests/Server.Tests/AskCancelHarness.fs:366`, which every test in `CancelByFocusTests.fs` uses. Read only.
[x] ``runMsg`` and ``messageCaller`` in `src/Server/Core/CoreMailboxLoop.fs`, and ``operationContext`` and ``replyFailure`` in `src/Server/Core/CoreMailboxEvents.fs`, match it.
[x] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs:188` refuses `Order.Cancel` with `replyUnsupported` (lines 203–207), alongside `Order.Change (_, ChangeOrigin.Parse)`, `Order.StartActor`, `Order.StartPeerActor` and `Order.LoadSave`.
[x] ``dispatchCancelActor`` in `src/Server/Core/CoreMailboxActors.fs:164` runs ``runCancel`` with a new submission id and replies `Ok ()`.
[x] ``runCancel`` in `src/Server/Core/CoreMailboxActors.fs:142` is private. It logs `Notice.ActorStopped (focusId, ActorCancelled)` through the `EventBody.ActorStop` shell, then calls `pool.finish`. With no live run for the focus, it logs nothing.
[x] ``postCancel`` in `src/Server/Core/CoreMailboxActors.fs:222` handles a client's posted Cancel, which arrives as `CoreMsg.PostEvent` through `messageForPosted` and `dispatchPostEvent`. It finds a resubmission with ``storedBySubmission``. Read only.
[x] `tests/Server.Tests/CancelByFocusTests.fs`: every test, including ``posted Cancel cancels a hanging Focus by NodeId`` and ``actorStop after cancel does not emit a root Focus ActorStop``.
[x] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` in `tests/Server.Tests/MailboxExpandTests.fs` builds `CancelActor` as its `CoreMsg` sample.

### Reference

[x] [Message shape](plan/core-refinement/cqrs-messages.md) decision 7 **Cancel** and decision 19 **One case each**.
[x] [Cutover overlap](plan/actor-as-client/reports/cutover-overlap.md) §112: Cancel stays `Order.Cancel` of the FocusId, the FocusId stays the pool key, the cancel arms stay, and no `CoreMsg` case is added.

## Do not load

[x] The client's posted Cancel path: `messageForPosted`, `dispatchPostEvent` and `postCancel`. That is [68 — Remove the CoreMsg case](68-remove-the-coremsg-case.md).
[x] `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, `Order.RunActor` and the pool start. Those belong to the Actor start cutover, 06–15.
[x] `tests/Server.Tests/AskCancelHarness.fs`. Cutover ticket 11 edits it.
[x] `src/Shared/ResidentProjection.fs`, `src/Shared/ViewModelDomPlan.fs`, `tests/Shared.Tests/WantTests.fs`, `tests/Shared.Tests/ViewModelTests.fs` and `tests/Shared.Tests/SyncLogicTests.fs`. Another ticket is changing them.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `cancelByFocus` places `Order.Cancel focusId` through `Mailbox.PlaceOrder` with a new `orderId` and the caller's authority. It keeps its signature and maps the reply to `()`.
3. [x] `placeOrder` handles `Order.Cancel` with the logic `runCancel` has today, using `placed.orderId` as the submission id. Its other refusals stay.
4. [ ] A Cancel of a live run logs `Notice.ActorStopped (focusId, ActorCancelled)`, finishes the pool row, and replies `Ok` of the stored stop's id.
5. [x] A Cancel with no live run for the focus logs nothing and replies `Ok`.
6. [x] `CoreMsg.CancelActor` leaves, with its arms and `dispatchCancelActor`. Add no `CoreMsg` case.
7. [x] A client's posted Cancel behaves as today.
8. [x] Add ``cancelByFocus places Order.Cancel`` to `CancelByFocusTests.fs`. The Actor ends, its live row is gone, and the log holds `Notice.ActorStopped` of the focus and `ActorCancelled`.
9. [x] Add ``second Cancel for the same run logs no stop`` to `CancelByFocusTests.fs`. The second Cancel replies `Ok ()` and logs no second stop.
10. [x] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` uses another `CoreMsg` case as its sample. Its name and assertions stay.
11. [x] Every other test in `CancelByFocusTests.fs` keeps its name and assertions. Tests do not count events, assert event ids, or use time.

## Outputs

[x] `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxActors.fs` and `src/Server/Core/CoreMailboxEvents.fs`. 5 source files.
[x] `tests/Server.Tests/CancelByFocusTests.fs` and `tests/Server.Tests/MailboxExpandTests.fs`. 2 test files.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/112-place-cancel-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs or under Do not load.
[ ] Stop when the edit adds a `CoreMsg` case or changes the pool key.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``cancelByFocus places Order.Cancel``
  - `tests/Server.Tests`: ``second Cancel for the same run logs no stop``
  - `tests/Server.Tests`: `CancelByFocusTests`
  - `tests/Server.Tests`: ``Mailbox is CoreMsg, PlaceOrder, and Ask``
[x] All executed tests pass.
[x] Write the verdict to `plan/core-refinement/reports/112-place-cancel-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
