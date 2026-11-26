# 20 — Move Desk Load and Desk Save build

Ticket: [20 — Move Desk Load and Desk Save](20-move-desk-load-and-save-ticket.md)

## Edits

[x] Alan decided on 2026-11-25 that the desk path moves directly onto the order queue, with no WebDAV redesign first. Ticket 20, ticket 23, Arch, and the project notes say so.
[x] `LoadSaveStart.startDesk` replaces the Desk send. A Desk Load of a valid subject places `Order.Load` through `CoreDoor.loadNode`. Desk Save, a held stack, and a non-subject place nothing and return `Ok ()`.
[x] `DeskLoadPush.subjectToLoad` is internal so `startDesk` reuses it. `DeskLoadPush.runLoadSave` is unchanged and now has no caller. `CoreActorPool.startLoadSaveCommand` is reached only from `runLoadSave`.
[x] `CoreMsg.StartLoadSaveCommand`, `CoreMailboxApi.startLoadSaveCommand`, and `CoreMailboxLoad.dispatchStartLoadSaveCommand` are deleted.
[x] `CoreMsg` is empty and removed, with `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, `CoreMailboxEvents.operationContext`, `CoreMailboxEvents.replyFailure`, and the `CoreMailboxLoop` dispatch of it. `src/Server/Core/CoreMsg.fs` stays because it holds `Query`, `Mailbox`, `CoreQueue`, `CoreMailboxAccess`, and `PersistFilling`.
[x] Tests send Desk through `LoadSaveStart.startDesk`: `LoadSaveCommandFixtures.actorRouter` (so `productionRouter` and `routeMatrixRouter`), `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, and `ParseThreadLoadTests`.
[x] New test ``Desk Load of a Workspace places Order.Load and Desk Save places nothing`` in `LoadSaveCommandTests`.
[x] `MailboxExpandTests` and `CoreMailboxDoorTests` drop the `CoreMsg` assertions. The mailbox kind test now covers `PlaceOrder` and `Ask`.

## Behaviour change

A Desk Load now logs an `EventBody.Load` event for the subject. Before, the Desk path pushed Parse inside the mailbox without logging an order.

## Check

[x] `scripts/agent-test.sh --full`: Server.Tests passed 777, failed 0, skipped 1, total 778. Shared.Tests passed 1948, failed 0, skipped 4, total 1952.
[x] `LoadSaveCommandTests`: passed 18, failed 0, skipped 0.
[x] Verdict: pass.

## Left

[ ] `DeskLoadPush.runLoadSave` and `CoreActorPool.startLoadSaveCommand` have no production caller. Deleting them is a separate edit.
[ ] The Desk Save has no actor. The Save actor and the Desktop_mapped WebDAV branch are not built.
[ ] Pull request 638 (Save refuses an Unparsed focus) may touch Load and Save tests. This build edits `LoadSaveCommandTests`, `LoadSaveSaveCommandTests`, and `LoadSaveCommandFixtures` only at the Desk send.
