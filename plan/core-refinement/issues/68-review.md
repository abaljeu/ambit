# 68 — Remove CoreMsg.PostEvent

Ticket: [68 — Remove CoreMsg.PostEvent](68-remove-coremsg-postevent.md)

Spec: [core-refinement spec](../spec.md)

Arch: [core-refinement arch](../arch.md)

`origin/staging` at `da2f036e` is the merge base. The client log rows and the client replies on the paths below match that base.

## 1. Standards

- `python3 scripts/standards-scan.py --diff origin/staging` printed measure rows for `replyClientRequest`, `replyPlacedCancel`, `sendsAsOrder`, `refusedClientEvent`, `placedForPosted`, and `messageForPosted`. Each function is under the 40-line limit. No phrase row.
- No findings.

## 2. Spec

- `CoreMsg` is `StartLoadSaveCommand` and `RecordSearchStop`. `placeOrder` runs `replyClientRequest` for `Order.ClientRequestActor` and `replyPlacedCancel` for `Order.CancelActor`. `MailboxHost.postPlacedOrder` queues `Order.ClientRequestActor`.
- A client `ActorStart` is `Order.ClientRequestActor`. `placedOrderFromEvent` sets `orderId` to `OrderId event.submissionId`. A repeated submission hits `storedBySubmission` and `replyStored` replies that stored id.
- `?test`, `?ai`, and a query still go through `admittedQueue`, `whenAdmitted`, and `postBuiltOrder`. The stored body is `Order.StartActor`. Any other name replies `Error "unknown actor"`.
- A client `Cancel` is minted by `RunLaunch.posted` at `EventId.zero`, with `commandName` `"Cancel"`. `SyncBatch.toWireBatch` writes `EventId.zero` on the wire.
- On `da2f036e`, `postCancel` replies the stored event when `storedBySubmission` finds the submission. `runCancel` of `Ok (Some stored)` replies `stored.id`. `Ok None` replies `event.id` through `replyStored`. For a client Cancel that id is `EventId.zero`.
- `replyPlacedCancel` uses that same order. A live focus replies the stored stop id. The same submission replies that id. `Ok None` replies `EventId.zero`. `runCancel` still appends `EventBody.ActorStop` of `ActorCancelled` through `appendLifecycle`. An idle focus appends no row.
- ``second Cancel on the same focus replies the same`` reads the `postPlacedOrder` reply. The live place and the repeat expect the stored stop id. A new order id on an idle focus expects `EventId.zero`. Those are the client `postCancel` replies. `replyCancel` on `da2f036e` replied `EventId.zero` for a live focus. That reply was the `cancelByFocus` path.
- ``posted Cancel replies the stored stop id`` posts `EventBody.Cancel` through `CoreMailboxApi.postEvents`. The first ack and the same submission again are the stored stop id.
- ``Undo, Redo, and Cancel are placed as orders`` keeps the idle client ack at `EventId.zero`, with an empty `events` list and no log row.
- The checked boxes in [68 — Remove CoreMsg.PostEvent](68-remove-coremsg-postevent.md) match this tree. The scope for this ticket is that file's Scope section. The step files beside the ticket are [68-build](68-build.md) and this review.
- These names are absent from `src/` and `tests/`: `postActorStart`, `postCancel`, `replyCancel`, `dispatchPostEvent`, `cancelByFocus`, `clientRequestRetired`, and `CoreMsg.PostEvent`. `PostedReply` remains the reply-channel alias used by `postBuiltOrder` and the two place arms.

## 3. Arch

- The queue text names this ticket as the removal of `PostEvent`. `StartLoadSaveCommand` and `RecordSearchStop` stay. `runMsg` still sends `RecordSearchStop` to `CoreEventDispatch.actorStop`.
- No findings.

## 4. Tests

- The named tests are present and check the Outputs text. ``cancelByFocus places Order.CancelActor`` is absent.
- ``posted Cancel replies the stored stop id`` and ``second Cancel on the same focus replies the same`` wait on the mailbox reply, then read the log.
- `scripts/agent-test.sh --full` set `AMBIT_FULL_SUITE=1` and wrote `/tmp/ambit-full-suite-20261115-162856.log`. The log ends `full suite finished`. `bash scripts/check-test.sh 3190` prints `DONE` and that log path.
- Server.Tests passed 775, failed 0, skipped 1 (``Change.upload structure persistence preserves existing file mtime``), total 776, duration 2 m 46 s.
- Shared.Tests passed 1946, failed 0, skipped 3 (``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, ``listForPush without git still returns walk``), total 1949, duration 28 s.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope. The fixes are suggested.

- **Problem.** [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) checks `CoreMsg` as `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
- **Fix.** When that ticket is built, `CoreMsg` is `StartLoadSaveCommand` and `RecordSearchStop`.
- **Problem.** [27 — Dialog cancels or replaces the search actor](../../online-search/issues/27-dialog-cancels-or-replaces-search-actor.md) checks `CoreMailboxApi.cancelByFocus`, `CoreMailboxActors.replyCancel`, and `CoreMailboxActors.dispatchPostEvent` as the Cancel entries.
- **Fix.** The Cancel entry is `placeOrder`'s `CancelActor` arm, `replyPlacedCancel`.
- **Problem.** [core-refinement arch](../arch.md) still lists `PostEvent` as a current `CoreMsg` case in the queue text, in the same sentence that names this ticket as the removal.
- **Fix.** The current cases are `StartLoadSaveCommand` and `RecordSearchStop`.
- **Problem.** [core-refinement project](../project.md) still records this ticket as Status `defined`, and it still names `postEventAccepted` as the placer. The 114 entry still lists `PostEvent` as a current `CoreMsg` case.
- **Fix.** Status is `coded`. `messageForPosted` and `acceptOne` place the client `ActorStart` and the client `Cancel`. `CoreMsg` has no `PostEvent` case.
- **Problem.** ``Cancel before Change rejects later Agent replace`` and ``late duplicate completion after cancel is ignored`` in `tests/Server.Tests/CancelByFocusTests.fs` wait with `Task.Delay`. Those waits were already on `da2f036e`.
- **Fix.** Order those steps with the hang signal the tests already hold.
