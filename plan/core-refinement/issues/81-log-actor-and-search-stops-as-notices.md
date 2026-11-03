# 81 — Log Actor stops as Notices
**Status**: `defined`
One job: The Server logs every Actor stop as `Notice.ActorStopped`, so History `EventBody` is `Order | Notice` with no third case. The Browser drops the stopped focus from its live set the way it handles any Notice, and a poll that holds only a stop starts no catch-up. The reason is decision 9 **EventBody** in [Message shape](plan/core-refinement/cqrs-messages.md): `EventBody` is `Order of Order | Notice of Notice`. This ticket finishes the goal of [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md).

Type: coding
Blockers: none. [110 — Log ActorStart and Cancel as EventBody.Order](110-log-actorstart-and-cancel-as-eventbody-order.md) and [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md) are `coded`.

## Inputs

### Working

#### History

[ ] ``EventBody`` in `src/Shared/History.fs:95-98` is `ActorStop of focusId: NodeId * result: ActorResult | Order of Order | Notice of Notice`. `ActorStop` is the last old case.
[ ] ``Notice`` in `src/Shared/History.fs` has ten cases, `NodeParsed` through `ParseFailed`. It has no `ActorStopped`.
[ ] The `EventBody` module in `src/Shared/History.fs` already keeps the old names `Change`, `Undo`, `Redo`, `Load`, `ActorStart` and `Cancel` as a function and an active pattern over `EventBody.Order`. `ActorStop` gets the same kind of thin shell over `Notice.ActorStopped`.
[ ] ``Ev.ops``, ``Ev.withOps`` and ``Ev.target`` in `src/Shared/History.fs` group `EventBody.ActorStop _` with `EventBody.Notice _`.
[ ] ``Ev.documentStateFromNotice`` in `src/Shared/History.fs` matches every `Notice` case with no wildcard.

#### Exhaustive Notice matches

The build treats warnings as errors (`Directory.Build.props`), so an incomplete match fails it. Each of these matches every `Notice` case with no wildcard:

[ ] ``encodeNotice`` in `src/Shared/EventJson.fs`
[ ] ``writeNoticeRow`` in `src/Server/Database.fs`
[ ] ``applyNotice`` in `src/Server/Core/CoreMailboxLoop.fs`
[ ] ``Ev.documentStateFromNotice`` in `src/Shared/History.fs`
[ ] ``writeGraph`` in `src/Shared/NoticeGraph.fs`
[ ] ``replaySkipsNotice`` in `src/Shared/EventLog.fs`
[ ] ``noticeNodeId`` in `src/Shared/SyncLogic.fs`

#### JSON

[ ] `src/Shared/EventJson.fs` writes a stop with body `kind` `actorStop` (``encodeActorStop``, ``encodeBody``) and reads it with ``decodeActorStopBody`` under ``decodeBody``. ``decodeNoticeBody`` and ``noticeFrom`` read Notices.

#### Writers and readers of `EventBody.ActorStop`

`EventBody.ActorStop` is used in 7 source files and 20 test files. Through the shell in the `EventBody` module, these sites build or match it as written:

[ ] Constructors: ``CoreEventDispatch.actorStop`` in `src/Server/Core/CoreEventDispatch.fs`, ``runCancel`` and ``recordFailedStart`` in `src/Server/Core/CoreMailboxActors.fs`, ``decodeActorStopBody`` in `src/Shared/EventJson.fs`.
[ ] `CoreMsg.RecordSearchStop` in ``runMsg`` in `src/Server/Core/CoreMailboxLoop.fs` calls `CoreEventDispatch.actorStop` with the root id and `ActorSucceeded`. A search stop is an Actor stop of the root.
[ ] Patterns: `Ev.ops`, `Ev.withOps`, `Ev.target`, ``ActorLive.applyEvent``, ``ActorLive.resultOf``, ``CoreEventDispatch.completeOther``, ``CoreMailboxActors.dispatchPostEvent``, ``CoreMailboxApi.isClientActorStop``, ``CoreMailboxApi.expandStored``, `EventJson.encodeBody`, ``MailboxLog.orderOfBody``.
[ ] ``completeOther`` in `src/Server/Core/CoreEventDispatch.fs` has its `EventBody.Notice _` arm before the arm that holds `EventBody.ActorStop`. A posted ActorStop never gets there, because ``clientBatchError`` in `src/Server/Core/CoreMailboxApi.fs` and ``dispatchPostEvent`` in `src/Server/Core/CoreMailboxActors.fs` refuse it first.

#### Client

[ ] ``ActorLive.applyEvent`` in `src/Shared/ActorLive.fs` adds the focus on `ActorStart`, removes it on `EventBody.ActorStop`, and leaves the live set alone for a Notice.
[ ] ``ActorLive.resultOf`` in `src/Shared/ActorLive.fs` maps a stop to its `CmdLastResult`. ``UpdateActorLive.withActorCmdResult`` in `src/Client/UpdateActorLive.fs` reads the event list through it. Read only.
[ ] ``isNoticeEvent`` in `src/Shared/SyncLogic.fs` is true for every `EventBody.Notice`. ``foldProjectedEvents`` sends a Notice to ``applyLoggedNotice``, which calls ``projectLoggedNotice`` and does not call `ActorLive.applyEvent`. Only ``withProjectedGraph`` calls `ActorLive.applyEvent`.
[ ] ``loadResponseToPoll`` in `src/Shared/SyncLogic.fs` sets `externalChanges` when any event is not a Notice. A stop-only poll starts a catch-up today.
[ ] ``countsAsExternal`` in `src/Server/Api.fs` is false for every `EventBody.Notice`. Read only.
[ ] `BootCache.applyLoggedEvent` in `src/Shared/BootCache.fs` sends a Notice to `ResidentProjection.applyDocumentNotice`, which goes through `Ev.documentStateFromNotice`. Read only.

#### Tests

[ ] ``ActorStop failed without message decodes as empty`` in `tests/Shared.Tests/EventJsonTests.fs` decodes a JSON string of `kind` `actorStop`.
[ ] ``Notice JSON round trip`` in `tests/Shared.Tests/EventJsonTests.fs` lists the ten Notice cases.
[ ] ``An old actorStart or cancel row is skipped on read`` in `tests/Shared.Tests/EventJsonTests.fs` is the model for an old-row test.
[ ] ``Ev JSON round-trips ActorStart and ActorStop`` in `tests/Shared.Tests/EventJsonTests.fs`.
[ ] `tests/Shared.Tests/ActorLiveTests.fs` already calls `SyncLogic.applyServerTail`: ``ActorStart adds and ActorStop removes a live Focus``, ``cancel response ActorStop Cancelled drops live after applyServerTail``.
[ ] ``applyServerTail ActorStart adds and ActorStop removes a live Focus`` in `tests/Shared.Tests/SyncLogicTests.fs`. Read only.
[ ] ``postSearch records ActorStart at the root`` in `tests/Server.Tests/SearchActorTests.fs`. Read only.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §3 decision 9 **EventBody**, decision 20 **Type cleanup**, and the `ActorStopped` row of the Notice table.
[ ] [Spec](plan/core-refinement/spec.md) §4 **Mailbox log mapping**, item 1 **Read**.
[ ] Alan, 2026-10-24: names follow `Order.Cancel*` and `Notice.*Stopped`. Existing `EventBody` member names stay. Old-form logs are dropped.
[ ] Alan, 2026-10-24: the rule of [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md) covers stops. A stop Notice tells the client it can drop its hold on the Actor; the client acts on it like any other Notice, and it starts no catch-up.
[ ] Alan, 2026-11-03: this ticket logs Actor stops only. `Notice.SearchStopped` is added by [111 — Place search start as an order, and post SearchStopped](111-place-search-start-as-an-order-and-post-searchstopped.md).

## Do not load

[ ] `Notice.SearchStopped` and any change to the search stop. The search stop stays an Actor stop of the root.
[ ] `CoreMsg.ActorStop` and how an Actor reports its end. That is [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md), [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) and [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md).
[ ] `src/Shared/ResidentProjection.fs`, `src/Shared/ViewModelDomPlan.fs`, `tests/Shared.Tests/WantTests.fs`, `tests/Shared.Tests/ViewModelTests.fs` and `tests/Shared.Tests/SyncLogicTests.fs`. Another ticket is changing them.
[ ] `plan/core-refinement/issues/116-one-eventbody-type-and-mailboxlog-ev-leaves.md`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] `Notice` gains `ActorStopped of focusId: NodeId * result: ActorResult`.
3. [ ] `ActorStop` leaves the `EventBody` union. `EventBody` is `Order of Order | Notice of Notice`.
4. [ ] `EventBody.ActorStop (focusId, result)` and `EventBody.(|ActorStop|_|)` stay as thin shells over `EventBody.Notice (Notice.ActorStopped (focusId, result))`, so the sites in Inputs that build or match a stop keep their code.
5. [ ] Every exhaustive `Notice` match in Inputs handles `Notice.ActorStopped`. It writes no row, changes no document state, and leaves the graph unchanged.
6. [ ] JSON writes and reads a stop as a Notice with its focus and result fields. The `actorStop` body kind leaves. An old `actorStop` row fails decode and is skipped on read.
7. [ ] Every stop the Server logs today is logged as `Notice.ActorStopped`, with the same focus, result and caller fields. That covers a done Actor, a Cancel, a failed start and a search stop.
8. [ ] The Browser removes the focus from `actorLiveFocusIds` when it applies a logged `Notice.ActorStopped`, on the Notice path. It leaves the live set alone for every other Notice.
9. [ ] A poll that holds only stop Notices gives `externalChanges` false, so no catch-up starts. `isNoticeEvent` and `countsAsExternal` keep treating every Notice alike.
10. [ ] The last command result for a stop is the same as today.
11. [ ] Posted ActorStops are still refused.
12. [ ] Change ``ActorStop failed without message decodes as empty`` to decode a Notice of the new kind, failed with no message, as an empty `ActorFailed`.
13. [ ] Add `Notice.ActorStopped` to ``Notice JSON round trip``.
14. [ ] Add ``An old actorStop row is skipped on read`` to `tests/Shared.Tests/EventJsonTests.fs`.
15. [ ] Add ``a stop-only poll removes the live focus and starts no catch-up`` to `tests/Shared.Tests/ActorLiveTests.fs`, not `SyncLogicTests.fs`. The state has a live focus, and the poll holds only `Notice.ActorStopped` of that focus. `applyServerTail` removes the focus from `actorLiveFocusIds`, and `loadResponseToPoll` gives `externalChanges` false. The test does not count events, assert event ids, or use time.
16. [ ] Keep ``Ev JSON round-trips ActorStart and ActorStop``, ``ActorStart adds and ActorStop removes a live Focus``, ``applyServerTail ActorStart adds and ActorStop removes a live Focus`` and ``postSearch records ActorStart at the root`` as written.
17. [ ] Update [Spec](plan/core-refinement/spec.md) §4 item 1 so `placedOrderFromEvent` refuses only `EventBody.Notice`.
18. [ ] Run the full suite to check these behaviors, which this ticket does not test directly:
    - `changesOnly` in `tests/Server.Tests/TestBackend.fs` and `commandNames` in `tests/Server.Tests/OrderedEventStreamTests.fs` drop every Notice, so a stop leaves their lists.
    - In `src/Shared/SyncLogic.fs`, a stop no longer counts as substantive in the answer-only check of `applyLoadResponse`, and `dropNotices` drops it.

## Outputs

[ ] `src/Shared/History.fs`, `src/Shared/EventJson.fs`, `src/Server/Database.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Shared/NoticeGraph.fs`, `src/Shared/EventLog.fs`, `src/Shared/SyncLogic.fs`, `src/Shared/ActorLive.fs`. 8 source files.
[ ] `tests/Shared.Tests/EventJsonTests.fs` and `tests/Shared.Tests/ActorLiveTests.fs`, and any other test file the full suite needs, up to 4.
[ ] `plan/core-refinement/spec.md` §4.

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/81-log-actor-and-search-stops-as-notices-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit needs a file under Do not load.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``ActorStop failed without message decodes as empty``
  - `tests/Shared.Tests`: ``Notice JSON round trip``
  - `tests/Shared.Tests`: ``An old actorStop row is skipped on read``
  - `tests/Shared.Tests`: ``a stop-only poll removes the live focus and starts no catch-up``
  - `tests/Shared.Tests`: ``Ev JSON round-trips ActorStart and ActorStop``
  - `tests/Shared.Tests`: ``ActorStop removes a live Focus``
  - `tests/Server.Tests`: ``postSearch records ActorStart at the root``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/81-log-actor-and-search-stops-as-notices-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
