# Sequence

This file lists a potential run order for the remaining tickets of [Actor as client](project.md).

The aim is to keep `Order.StartActor` as the only actor start.

Each ticket converts the uses of one message case, and a later ticket deletes that case.

A ticket file's status can lag behind staging, and landing on staging counts as done. Status `done` means review approved the ticket.

A dependency marked Inference is an order this file chooses from the aim and the ticket texts.

The order list in [Map](map.md) is the earlier order. This file follows the aim above.

| Order | Ticket | Status now | Depends on | Why it goes here |
| --- | --- | --- | --- | --- |
| 1 | **[09 — Start the query command](issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | This ticket posts Find and Move and the query as `Order.StartActor` and deletes `CoreMsg.RecordSearchStart`, and that delete removes `CoreEventDispatch.actorStart`, which logs `Order.StartActorDeprecated`. |
| 2 | **[10 — Start git Load](issues/10-dispatch-git-load-and-save/10-dispatch-git-load-and-save-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | This ticket converts each Load caller of `CoreMsg.StartLoadSaveCommand`, and each Save caller stays for 19 — Start git Save. |
| 3 | **[19 — Start git Save](issues/19-dispatch-git-save/19-dispatch-git-save-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. 10 — Start git Load. | This ticket converts each remaining Save caller of `CoreMsg.StartLoadSaveCommand` after the Load callers move. |
| 4 | **[16 — Delete the old Order cases](issues/16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md)** | `defined`. Folder stage `defined`. | 15 — Delete the old Actor starts. The ticket checks that blocker, and it checks [112 — Place Cancel through PlaceOrder](../core-refinement/issues/112-place-cancel-through-placeorder.md). Inference: 09 — Start the query command. | This ticket deletes `Order.StartActorDeprecated` and keeps `Order.StartActor` once nothing writes the old case. |
| 5 | **[111 — Place search start as an order, and post SearchStopped](../core-refinement/issues/111-place-search-start-as-an-order-and-post-searchstopped.md)** | `defined` in that ticket. This project has no folder stage for it. | The map places it after 15 — Delete the old Actor starts. The ticket blocks on [81 — Log Actor stops as Notices](../core-refinement/issues/81-log-actor-and-search-stops-as-notices.md) and [110 — Log ActorStart and Cancel as EventBody.Order](../core-refinement/issues/110-log-actorstart-and-cancel-as-eventbody-order.md). Both of those statuses are `coded`. Inference: after 09 — Start the query command. | The map uses this ticket to remove `CoreMsg.RecordSearchStop` after 15 — Delete the old Actor starts. |
| 6 | **Desk Save decision** | Open in 20 — Move Desk Load and Desk Save and under **Load and save** in [Arch](arch.md). | None. | Alan names where Desk Save goes, and the next ticket uses that name. |
| 7 | **[20 — Move Desk Load and Desk Save](issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md)** | `defined`. Folder stage `defined`. | None on the ticket. Inference: the Desk Save decision, 10 — Start git Load, and 19 — Start git Save. | This ticket moves Desk off `CoreMsg.StartLoadSaveCommand` and deletes that case at the end, after Alan has named where Desk Save goes. |
| 8 | **[11 — Move door and case tests](issues/11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 07 — Start the test command. |
| 9 | **[12 — Move pool and issue tests](issues/12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md)** | `defined`. Folder stage `defined`. | 06 — Log ActorOrder. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 06 — Log ActorOrder. |
| 10 | **[13 — Move persist and command tests](issues/13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md)** | `defined`. Folder stage `defined`. | 06 — Log ActorOrder. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 06 — Log ActorOrder. |
| 11 | **[14 — Move runtime and session tests](issues/14-move-runtime-and-session-tests/14-move-runtime-and-session-tests-ticket.md)** | `defined`. Folder stage `defined`. | 07 — Start the test command. That dependency is met. | Inference: this test batch follows the message-case tickets, and the file's blocker is 07 — Start the test command. |
| 12 | **[01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md)** | `defined`. Deferred 2026-09-27. | None. | Inference: the file defers this grill, so it follows the message-case tickets. |
| 13 | **[02 — Graph handoff](issues/02-graph-handoff.md)** | `defined`. Deferred 2026-09-27. | None. | Inference: the file defers this grill, so it follows the message-case tickets. |

## Notes

- 10 — Start git Load says `startLoadSaveCommand` calls `startPeerActor` when the path is Git. 15 — Delete the old Actor starts says that function returns `Ok None` for Git and for Desk. 10 stops at its first process step while that Git input differs from 15.
- 19 — Start git Save says `registerPeer` is on `CoreActorPool`. 15 — Delete the old Actor starts says `registerPeer` is deleted. 19 stops at its first process step while that input differs from 15.
- 19 — Start git Save says that after it and 10 — Start git Load, nothing sends `StartLoadSaveCommand`, and it stops when a send remains. 20 — Move Desk Load and Desk Save keeps the Desk send until its last step. Inference: 19 converts the Git Save callers and leaves the Desk send for 20.
- 111 — Place search start as an order, and post SearchStopped still adds `Order.StartSearch` and still removes `CoreMsg.RecordSearchStart`. The map shrinks that ticket to posting `Notice.SearchStopped` and removing `CoreMsg.RecordSearchStop`. The map says this project records that order and leaves the ticket in its own project.
- 15 — Delete the old Actor starts lists the live `CoreMsg` cases as `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`. This order covers `StartLoadSaveCommand`, `RecordSearchStart`, and `RecordSearchStop`. `CoreMsg.ActorStop` and `CoreMsg.PostEvent` have no converting ticket in this project.
- The map lists 16 — Delete the old Order cases before 09 — Start the query command. Inference: this file runs 09 first, because 16's one job says nothing writes `Order.StartActorDeprecated` before that ticket, and 09 deletes `CoreEventDispatch.actorStart`, which logs that order.

## Done

- **[03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md)** — status `done`.
- **[04 — Start surface](issues/04-start-surface.md)** — status `done`.
- **[05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md)** — status `done`.
- **[06 — Log ActorOrder](issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)** — landed on staging in PR 527.
- **[07 — Start the test command](issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)** — landed on staging in PR 540.
- **[08 — Start the AI Actor](issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)** — status `coded`, folder stage `refactor`, review file and refactor file present, landed on staging.
- **[15 — Delete the old Actor starts](issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md)** — status `coded`, folder stage `coded`, review file present, the map checks this ticket, landed on staging.
- **[17 — Rename commandId to actorNodeId](issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md)** — landed on staging in PR 524.
- **[18 — Place ActorOrder through PlaceOrder](issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md)** — landed on staging in PR 535.
