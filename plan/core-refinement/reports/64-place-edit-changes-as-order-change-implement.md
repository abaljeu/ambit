# 64 — Place Edit changes as Order.Change implement

Implement report for [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md).

## Outcome

[x] Status is `coded`.
[x] `ChangeOrigin` is `Edit | Parse` beside `Order` in [MailboxLog.fs](../../../src/Shared/MailboxLog.fs).
[x] `Order.Change` is `ops: Op list * origin: ChangeOrigin`.
[x] `orderOfBody` and `ofEv` map a logged Change to origin `Edit`.
[x] `historyBody` and `toEv` write the op list.
[x] `MailboxHost.postForReplies` takes `(AsyncReplyChannel<'Reply> -> Mailbox) list` and posts `CoreQueue.Public` of each item.
[x] `CoreMailboxApi.postEvents` places `EventBody.Change` as `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`.
[x] The PlaceOrder `orderId` is the Ev `submissionId`.
[x] The PlaceOrder `commandName` is the empty-string map `ofEv` already uses.
[x] Undo, Redo, ActorStart, and Cancel in that list post `Mailbox.CoreMsg` of `CoreMsg.PostEvent`.
[x] `dispatchMailbox` passes `commandName` into `placeOrder`.
[x] Origin `Edit` builds an `Ev` with `id` `EventId.zero`, `submissionId` of the `orderId`, `commandName` from `toEv`, and body `EventBody.Change ops`, then calls `CoreEventDispatch.postEvent` with `graphOnly` false through `eventDispatchContext`.
[x] `prepare` fills the authority.
[x] The reply is `Ok` of the stored Ev's `id`.
[x] `graphOnly` false writes the document file and runs `pushChangeSpecials`.
[x] Origin `Parse` replies `Error "unsupported"`. [102 — Place Parse changes as Order.Change](../issues/102-place-parse-changes-as-order-change.md) owns that arm.

## Tests added

[x] ``postEvents places Order.Change with origin Edit`` in [CoreMailboxDoorTests.fs](../../../tests/Server.Tests/CoreMailboxDoorTests.fs) posts `postedEvent` through `CoreMailboxApi.postEvents`, reads the stored Ev with `getEventsSince`, and checks `MailboxLog.ofEv` is `Order.Change` with origin `Edit` and `Ok` of that `EventId`.
[x] ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` in the same file.
[x] Undo through `postEvents` replies `Ok` and stores `EventBody.Undo`.
[x] Redo through `postEvents` replies `Ok` and stores `EventBody.Redo`.
[x] ActorStart through `postEvents` replies `Ok` and stores `EventBody.ActorStart`.
[x] Cancel through `postEvents` replies `Ok`. An idle focus returns `eventId` `EventId.zero`, an empty ack, and no log row. See Found elsewhere.

## Tests changed

[x] ``ofEv then toEv keeps a Change`` in [MailboxLogTests.fs](../../../tests/Shared.Tests/MailboxLogTests.fs) expects `Order.Change(ops, ChangeOrigin.Edit)` and the same ops on the round trip.
[x] ``login during a pending logout runs and a dropped generation rejects a late order`` in [SessionTests.fs](../../../tests/Server.Tests/SessionTests.fs) constructs `Order.Change(ops, ChangeOrigin.Edit)`.
[x] ``admitted route order after DropAuthority is rejected`` in the same file constructs `Order.Change(ops, ChangeOrigin.Edit)`. Ticket section 6 omits this site. See Found elsewhere.

## scripts/agent-test.sh

[x] `scripts/agent-test.sh` with no arguments passed.
[x] Server filter `FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~SessionTests`: 47 passed.
[x] Shared filter `FullyQualifiedName~MailboxLogTests`: 5 passed.
[x] Both added tests ran inside `FullyQualifiedName~CoreMailboxDoorTests`.
[x] Focused filters during the red-green loop also passed: the two added tests, ``a resubmitted Change replies the stored id``, ``An Edit pushes its owning special node``, ``An Edit marks its owning special node at the Edit's id``, ``ofEv then toEv keeps a Change``, ``login during a pending logout runs and a dropped generation rejects a late order``, ``logout returns while the reconciliation route is open``, and ``admitted route order after DropAuthority is rejected``.

## Found elsewhere

### Logout route test

[x] **Problem.** 64 — Place Edit changes as Order.Change section 6 line 181 names ``logout returns while the reconciliation route is open`` as an `Order.Change` constructor. SessionTests.fs line 447 posts `CoreMailboxApi.postEvent` of `orderEvent "reconcile"`.
[x] **Fix.** Leave that test on `postEvent`. Drop the constructor item, or point it at a test that constructs `Order.Change`.

### Omitted constructor

[x] **Problem.** Ticket section 6 line 182 says the login test, the logout test, and ``ofEv then toEv keeps a Change`` are every `Order.Change` site. SessionTests.fs line 497, ``admitted route order after DropAuthority is rejected``, also constructs `Order.Change`. Section 6 omits it.
[x] **Fix.** Name that test in section 6. This build updated the constructor to `Order.Change(ops, ChangeOrigin.Edit)` so the solution compiles.

### Cancel storage

[x] **Problem.** Ticket section 6 line 202 says Cancel through `postEvents` stores `EventBody.Cancel`. [CoreMailboxActors.fs](../../../src/Server/Core/CoreMailboxActors.fs) `postCancel` line 222 replies the inbound draft when the focus has no live secret, so the ack `eventId` is `EventId.zero` and the log has no Cancel row. A live focus stores `EventBody.ActorStop`. [64 — Place Edit changes as Order.Change scope](64-merge-postevent-and-postgraphonly-into-change-scope.md) line 121 repeats the storage claim.
[x] **Fix.** Keep Cancel on `CoreMsg.PostEvent`. Assert the live `postCancel` outcome. Store `EventBody.Cancel` only in a later ticket that owns that log row.
