# Sequence

This file lists a potential run order for the remaining tickets of [Actor as client](project.md).

The aim is to keep `Order.StartActor` as the only actor start.

Each ticket converts the uses of one message case, and a later ticket deletes that case.

A ticket file's status can lag behind staging, and landing on staging counts as done. Status `done` means review approved the ticket.

A dependency marked Inference is an order this file chooses from the aim and the ticket texts.

| Order | Ticket | Status now | Depends on | Why it goes here |
| --- | --- | --- | --- | --- |
| 1 | **[09 — Start the query command](issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)** | `doc`. Folder stage `doc`. | 07 — Start the test command. That dependency is met. | This ticket posts Find and Move and the query as `Order.StartActor` and deletes `CoreMsg.RecordSearchStart`, and that delete removes `CoreEventDispatch.actorStart`, which logs `Order.StartActorDeprecated`. |
| 2 | **[10 — Start git Load](issues/10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | Git Load already posts `Order.StartActor` through `GithubTransportActor.start`. This ticket keeps that post. A Desk Load stays on `CoreMsg.StartLoadSaveCommand` for 20 — Move Desk Load and Desk Save. A Save caller stays for 19 — Start git Save. |
| 3 | **[19 — Start git Save](issues/19-dispatch-git-save/19-dispatch-git-save-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. 10 — Start git Load. | Git Save already posts `Order.StartActor` through `GithubTransportActor.start`. This ticket keeps that post. A Desk Save stays on `CoreMsg.StartLoadSaveCommand` for 20 — Move Desk Load and Desk Save. |
| 4 | **[16 — Delete the old Order cases](issues/16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md)** | `defined`. Folder stage `defined`. | 15 — Delete the old Actor starts. The ticket checks that blocker, and it checks [112 — Place Cancel through PlaceOrder](../core-refinement/issues/112-place-cancel-through-placeorder.md). Inference: 09 — Start the query command. | This ticket deletes `Order.StartActorDeprecated` and keeps `Order.StartActor` once nothing writes the old case. |
| 5 | **[111 — Place search start as an order, and post SearchStopped](../core-refinement/issues/111-place-search-start-as-an-order-and-post-searchstopped.md)** | `defined` in that ticket. This project has no folder stage for it. | The ticket blocks on [81 — Log Actor stops as Notices](../core-refinement/issues/81-log-actor-and-search-stops-as-notices.md) and [110 — Log ActorStart and Cancel as EventBody.Order](../core-refinement/issues/110-log-actorstart-and-cancel-as-eventbody-order.md). Both of those statuses are `coded`. Inference: after 09 — Start the query command. | 09 — Start the query command deletes `CoreMsg.RecordSearchStart`. This ticket then posts `Notice.SearchStopped` and removes `CoreMsg.RecordSearchStop`. |
| 6 | **Desk Save decision** | Open in 20 — Move Desk Load and Desk Save and under **Load and save** in [Arch](arch.md). | None. | Alan names where Desk Save goes, and the next ticket uses that name. |
| 7 | **[20 — Move Desk Load and Desk Save](issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md)** | `defined`. Folder stage `defined`. | None on the ticket. Inference: the Desk Save decision, 10 — Start git Load, and 19 — Start git Save. | This ticket moves Desk off `CoreMsg.StartLoadSaveCommand` and deletes that case at the end, after Alan has named where Desk Save goes. |
| 8 | **[21 — Convert CoreMsg.ActorStop](issues/21-convert-coremsg-actorstop/21-convert-coremsg-actorstop-ticket.md)** | `defined`. Folder stage `defined`. | None on the ticket. | 15 — Delete the old Actor starts left `CoreMsg.ActorStop` as a live case. The stop body is already `Notice.ActorStopped`, and `CoreMailboxApi.postNotice` already posts `CoreQueue.Notice`. This ticket moves those uses onto that notice and deletes the case. It follows 20 — Move Desk Load and Desk Save so `CoreMsg.StartLoadSaveCommand` is already gone, and it comes before 22 — Convert CoreMsg.PostEvent because `dispatchPostEvent` still refuses a client `EventBody.ActorStop`. |
| 9 | **[22 — Convert CoreMsg.PostEvent](issues/22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md)** | `defined`. Folder stage `defined`. | 09 — Start the query command. 111 — Place search start as an order, and post SearchStopped. 20 — Move Desk Load and Desk Save. 21 — Convert CoreMsg.ActorStop. | `Mailbox.PlaceOrder` and `placeOrder` already run an `Order`. `CoreQueue.Notice` already carries a `Notice`. This ticket moves each `CoreMsg.PostEvent` use onto those two paths and deletes the case. It is last among the `CoreMsg` cases in this file, so the empty union leaves with the case. It comes before the test-move tickets, which call `postEvent`. |
| 10 | **[11 — Move door and case tests](issues/11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 07 — Start the test command. |
| 11 | **[12 — Move pool and issue tests](issues/12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md)** | `defined`. Folder stage `defined`. | 06 — Log ActorOrder. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 06 — Log ActorOrder. |
| 12 | **[13 — Move persist and command tests](issues/13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md)** | `defined`. Folder stage `defined`. | 06 — Log ActorOrder. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 06 — Log ActorOrder. |
| 13 | **[14 — Move runtime and session tests](issues/14-move-runtime-and-session-tests/14-move-runtime-and-session-tests-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 07 — Start the test command. |
| 14 | **[01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md)** | `defined`. Deferred 2026-09-27. | None. | Inference: the file defers this grill, so it follows the message-case tickets. |
| 15 | **[02 — Graph handoff](issues/02-graph-handoff.md)** | `defined`. Deferred 2026-09-27. | None. | Inference: the file defers this grill, so it follows the message-case tickets. |

## Notes

- **111 — Place search start as an order, and post SearchStopped** still adds `Order.StartSearch` and still removes `CoreMsg.RecordSearchStart`. This file uses that ticket to post `Notice.SearchStopped` and to remove `CoreMsg.RecordSearchStop`. The ticket stays in core-refinement.

## Done

- **[03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md)** — status `done`.
- **[04 — Start surface](issues/04-start-surface.md)** — status `done`.
- **[05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md)** — status `done`.
- **[06 — Log ActorOrder](issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)** — landed on staging in PR 527.
- **[07 — Start the test command](issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)** — landed on staging in PR 540.
- **[08 — Start the AI Actor](issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)** — status `coded`, folder stage `refactor`, review file and refactor file present, landed on staging.
- **[15 — Delete the old Actor starts](issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md)** — status `coded`, folder stage `coded`, review file present, landed on staging.
- **[17 — Rename commandId to actorNodeId](issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md)** — landed on staging in PR 524.
- **[18 — Place ActorOrder through PlaceOrder](issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md)** — landed on staging in PR 535.
