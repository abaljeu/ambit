# 108 — Log Change as EventBody.Order
**Status**: `defined`
One job: The Server logs every Change as `EventBody.Order` of `Order.Change`, holding the op list and its `origin`, and the `EventBody.Change` case leaves the union. A `graphOnly` post logs its Change with origin `Parse`, so a stored Change keeps its origin for both Edit and Parse. The reason is decision 1 **Logged as placed** in [Message shape](plan/core-refinement/cqrs-messages.md): history `EventBody.Order` holds the placed `Order` unchanged.

Type: coding
Blockers: none ([102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md) and [107 — Add EventBody.Order](107-add-eventbody-order.md) are on staging)

## Inputs

### Working

[ ] ``EventBody`` in `src/Shared/History.fs` has the case `Change of ops: Op list` and the case `Order of Order`.
[ ] ``Ev.ops``, ``Ev.target`` and ``Ev.withOps`` in `src/Shared/History.fs` each have an `EventBody.Change` arm and an `EventBody.Order` arm.
[ ] ``Ev.withOps`` keeps `origin` for `EventBody.Order (Order.Change(_, origin))`.
[ ] ``encodeBody`` in `src/Shared/EventJson.fs` writes `EventBody.Change` as `kind` `change`.
[ ] ``decodeChangeBody`` in `src/Shared/EventJson.fs` builds `EventBody.Change`; ``decodeBody`` selects it for `kind` `change`.
[ ] ``orderOfBody`` in `src/Shared/MailboxLog.fs` maps `EventBody.Change ops` to `Order.Change(ops, ChangeOrigin.Edit)`, and `EventBody.Order order` to `order`.
[ ] ``historyBody`` in `src/Shared/MailboxLog.fs` maps `Order.Change(ops, _)` to `EventBody.Change ops`, so the stored Change drops `origin`.
[ ] ``editEvent`` in `src/Server/Core/CoreMailboxLoop.fs` builds the stored Ev through `MailboxLog.toEv` with `Order.Change(ops, ChangeOrigin.Edit)`. Read only.
[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.Change(_, ChangeOrigin.Parse)`. Read only.
[ ] ``postMailboxChange`` in `src/Server/Core/CoreMailboxApi.fs` takes `graphOnly: bool`. ``postEvents`` calls it with `false` and ``postGraphOnly`` with `true`.
[ ] ``changeMessage`` in `src/Server/Core/CoreMailboxApi.fs` sends a `graphOnly` post as `CoreMsg.PostGraphOnly(caller, event, channel)`. It carries an Ev, with no `origin`.
[ ] ``changeMessage`` sends a zero-id Change with `graphOnly` `false` as `Mailbox.PlaceOrder` of `Order.Change(ops, ChangeOrigin.Edit)`, read through `MailboxLog.ofEv`.
[ ] ``dispatchPostGraphOnly`` in `src/Server/Core/CoreMailboxEvents.fs` stores the posted Ev through `CoreEventDispatch.postEvent` with `graphOnly` `true`.
[ ] ``mint`` in `src/Server/GraphOnlyChangePost.fs` builds the parse Ev with `EventBody.Change ops`. `ParseThread` and `LazyLoadReconciliationServer` post it through `CoreDoor.postGraphOnly`.
[ ] Nothing on staging stores a Change with origin `Parse`.
[ ] Source constructors of `EventBody.Change`: `ClientHistory` (104), `GraphOnlyChangePost` (14), `RunAgentActor` (119), `TestActor` (19, 86), `Ev.withOps`, `EventJson.decodeChangeBody`.
[ ] Source patterns on `EventBody.Change`: `Ev.ops`, `Ev.target`, `Ev.withOps`, `ActorLive.applyEvent`, `ActorLive.resultOf`, `CoreEventDispatch` (135, 228), `CoreMailboxApi` (144, 218), `EventJson.encodeBody`, `MailboxLog.orderOfBody`.
[ ] `EventBody.Change` appears 236 times in 63 test files.
[ ] `TreatWarningsAsErrors` is on in `Directory.Build.props`, so any warning fails the build.
[ ] `EventLogFile.decodeEvent` and the `DbAgent` restore skip an entry that does not decode. `BootCache` fetches `/state` on a decode error.
[ ] Alan, 2026-10-24: `origin` is logged. Old-form logs are dropped, not mapped.
[ ] ``Ev JSON round-trips a Change`` and ``mintChange encodes eventId 0 on the wire`` in `tests/Shared.Tests/EventJsonTests.fs`.
[ ] ``ofEv then toEv keeps a Change`` in `tests/Shared.Tests/MailboxLogTests.fs`.
[ ] ``postEvents places Order.Change with origin Edit`` and ``a resubmitted Change replies the stored id`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``A graph-only change pushes nothing`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`.
[ ] ``postGraphOnly updates Graph and EventLog without file write`` and ``a graphOnly post stores the Change without a document file write`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs`.
[ ] ``decideBootRead fetches /state on decode error`` in `tests/Shared.Tests/BootCacheTests.fs`.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §2 **Order** and §3 decisions 1, 4 and 18. They already say the logged Change holds `origin`.

## Do not load

[ ] `MailboxEventBody`, `MailboxLog.Ev`, `ofEv` and `toEv` stay. Their removal is [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md).
[ ] Undo, Redo, Load, ActorStart and Cancel stay in their own cases.
[ ] The 63 test files that use `EventBody.Change` stay as written.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The `EventBody.Change` case leaves the `EventBody` union in `src/Shared/History.fs`.
3. [ ] Every Change is logged and written as `EventBody.Order` of `Order.Change` with its `origin`. Its JSON `kind` is `order`.
4. [ ] An old `change` row is skipped on read.
5. [ ] The build stays warning-free.
6. [ ] Add ``An old change row is skipped on read`` in `tests/Shared.Tests/EventJsonTests.fs`: a JSON body with `kind` `change` fails `EventJson.decode`.
7. [ ] Change the `graphOnly` post so it logs its Change with origin `Parse`. The way it carries `origin` is the builder's choice. Every `graphOnly` post logs origin `Parse`.
8. [ ] Add ``a stored Edit Change keeps origin Edit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`: `postEvents` of a Change stores `EventBody.Order (Order.Change(ops, ChangeOrigin.Edit))`.
9. [ ] Add ``a stored Parse Change keeps origin Parse`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`: `postGraphOnly` of a Change stores `EventBody.Order (Order.Change(ops, ChangeOrigin.Parse))`.

## Outputs

[ ] `src/Shared/History.fs`, and the other source files the builder changes, up to 8 source files in total
[ ] `tests/Shared.Tests/EventJsonTests.fs` — add ``An old change row is skipped on read``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — add ``a stored Edit Change keeps origin Edit`` and ``a stored Parse Change keeps origin Parse``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/108-log-change-as-eventbody-order-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``An old change row is skipped on read``
  - `tests/Server.Tests`: ``a stored Edit Change keeps origin Edit``
  - `tests/Server.Tests`: ``a stored Parse Change keeps origin Parse``
  - `tests/Shared.Tests`: ``Ev JSON round-trips a Change``
  - `tests/Shared.Tests`: ``mintChange encodes eventId 0 on the wire``
  - `tests/Shared.Tests`: ``ofEv then toEv keeps a Change``
  - `tests/Server.Tests`: ``postEvents places Order.Change with origin Edit``
  - `tests/Server.Tests`: ``a resubmitted Change replies the stored id``
  - `tests/Shared.Tests`: ``decideBootRead fetches /state on decode error``
  - `tests/Server.Tests`: ``A graph-only change pushes nothing``
  - `tests/Server.Tests`: ``postGraphOnly updates Graph and EventLog without file write``
  - `tests/Server.Tests`: ``a graphOnly post stores the Change without a document file write``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/108-log-change-as-eventbody-order-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
