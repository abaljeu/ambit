# 64 — Place Edit changes as Order.Change

Ticket: [[../issues/64-merge-postevent-and-postgraphonly-into-change.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- No findings. `python scripts/standards-scan.py --diff origin/staging` exited 0. The scan printed `postedMailbox` at 21 lines, `editEvent` at 10 lines, `replyEdit` at 18 lines, and `placeOrder` at 13 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`. `placeOrder` takes `MailboxContext` and `TrackingOrder`. `replyEdit` takes `MailboxContext`, `TrackingOrder`, and `ops`. Both are within four positional arguments. `OrderId.fs` is untracked, so that scan omits it. The file is 13 lines, a copy of `NodeId`, and each line is within 100 characters.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261024-160157.log`. `scripts/check-test.sh 55243` printed `DONE` and that log path. Server.Tests passed 758, failed 1, skipped 0, total 759, duration 4 m 1 s. The server failure ended the suite before Shared.Tests.
- 2. **Edit order** — `postEvents places Order.Change with origin Edit` is in `tests/Server.Tests/CoreMailboxDoorTests.fs`. It posts `postedEvent` through `CoreMailboxApi.postEvents`, reads the stored Ev with `getEventsSince`, and checks `MailboxLog.ofEv` is `Order.Change` with origin `Edit` and `Ok` of that `EventId`. It passed inside the server run.
- 3. **Resubmission** — `a resubmitted Change replies the stored id` is in the same file. A second `postEvents` of the same Change equals the first `eventId` and `events`. It passed inside the server run.
- 4. **Mapped origin** — `ofEv then toEv keeps a Change` is in `tests/Shared.Tests/MailboxLogTests.fs`. It expects `OrderId event.submissionId`, `Order.Change(ops, ChangeOrigin.Edit)`, and the same `Ev` on the round trip. The full suite stopped before Shared.Tests. `scripts/agent-test.sh --filter "FullyQualifiedName~ofEv then toEv keeps a Change"` then passed that one Shared test.
- 5. **Login constructor** — `login during a pending logout runs and a dropped generation rejects a late order` posts `PlaceOrder of TrackingOrder` and constructs `Order.Change(ops, ChangeOrigin.Edit)`. It passed inside the server run.
- 6. **DropAuthority constructor** — `admitted route order after DropAuthority is rejected` posts `PlaceOrder of TrackingOrder` and constructs `Order.Change(ops, ChangeOrigin.Edit)`. It passed inside the server run.
- 7. **Logout route** — `logout returns while the reconciliation route is open` posts `CoreMailboxApi.postEvent` of `orderEvent "reconcile"`. See Found elsewhere. It passed inside the server run.
- 8. **Edit persist** — `An Edit pushes its owning special node` posts `editFileText` through `postEvents` and expects `pushPersist` of that file. `An Edit marks its owning special node at the Edit's id` checks Unpersisted and `changedVersion` equal to the ack `eventId`. Both passed inside the server run.
- 9. **Other bodies** — `Undo, Redo, ActorStart, and Cancel still post as PostEvent` checks stored `EventBody.Undo`, `EventBody.Redo`, and `EventBody.ActorStart`. The Cancel arm checks `EventId.zero`, an empty `events` list, and an absent log row. See Found elsewhere. It passed inside the server run.
- 10. **Non-zero id** — `changes POST rejects non-zero EventId and admits zero` in `tests/Server.Tests/ChangeEndpointResilienceTests.fs` expects `BadRequest` and the text `posted EventId must be zero`. It passed inside the server run. `postedMailbox` places `EventBody.Change` at `EventId.zero` as `PlaceOrder of TrackingOrder`. A Change at `EventId.Int` posts `CoreMsg.PostEvent`, and `persistNew` returns `posted EventId must be zero`.
- 11. **Idempotent ack** — `POST duplicate submissionId with stale revision stays idempotent` failed. Expected Change submission ids `[cid]`. Actual was `[cid, other]`. The failure matches [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md). `Api.eventsThroughSnapshot` appends the later Change. This ticket's files leave that append on `Api.fs`.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Logout route test** — Accept.
  - **Problem.** Section 6 named `logout returns while the reconciliation route is open` as an `Order.Change` site. That test posts `CoreMailboxApi.postEvent` of `orderEvent "reconcile"`.
  - **Fix.** Leave that test on `postEvent`. The `Order.Change` constructor list omits it.
- 2. **Omitted constructor** — Accept.
  - **Problem.** Section 6 said the login test, the logout test, and `ofEv then toEv keeps a Change` are every `Order.Change` site. `admitted route order after DropAuthority is rejected` also constructs `Order.Change(ops, ChangeOrigin.Edit)`.
  - **Fix.** Section 6 names `admitted route order after DropAuthority is rejected`.
- 3. **Cancel storage** — Accept.
  - **Problem.** Section 6 said Cancel through `postEvents` stores `EventBody.Cancel`. `CoreMailboxActors.postCancel` replies the inbound draft when `runCancel` returns `Ok None`, so an idle focus acks `EventId.zero` and stores no row. A live focus stores `EventBody.ActorStop` through `appendLifecycle`.
  - **Fix.** Keep Cancel on `CoreMsg.PostEvent`. An idle Cancel acks `EventId.zero` with no row. Storage of `EventBody.Cancel` stays with a later ticket.
