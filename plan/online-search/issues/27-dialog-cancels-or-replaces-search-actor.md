# 27 — Dialog cancels or replaces the search actor
**Status:** defined
One job: The Browser cancels the Find dialog's pending server search actor when that dialog closes, and it replaces that actor when a committed search text sends a new search: the earlier actor is cancelled and the new actor starts. The reason is story 13 **Dialog cancels or replaces the search actor** in [Search spec](../spec.md).

Type: coding
Blockers:

[23 — Server search works](23-server-search-works.md) is done. The Server returns Find hits, and the dialog appends the hits the client search did not find.

[24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md) is defined. This ticket starts while that removal is still ahead. The ordinary finish of a search stays `Notice.ActorStopped` through the actor stop. This ticket uses the existing cancel. 24 — Remove CoreMsg.RecordSearchStop owns `CoreMsg.RecordSearchStop`.

[26 — Resumed typing finds server hits locally](26-resumed-typing-finds-server-hits-locally.md) is defined. This ticket leaves the residency list after an installed hit to that ticket.

## Inputs

### Working

[x] `closeSearchDialogOp` in `src/Client/SearchDialog.fs` matches `SearchDialog`. It stores the query, calls `resetSearchResults`, and returns the previous mode with the effect list `CancelSearchQuietGap`.
[x] `App.runEffect` in `src/Client/App.fs` matches `CancelSearchQuietGap` and calls `SearchDialogView.cancelQuietGap`.
[x] `SearchDialogView.cancelQuietGap` in `src/Client/SearchDialogView.fs` clears the quiet-gap timer.
[x] `Update.update` in `src/Client/Update.fs` matches `NodeSearchQuery` when the dialog text differs. It clears `serverHitIds`, sets `selectedIndex` to 0, and returns `ArmSearchQuietGap`.
[x] `SearchDialogView.armQuietGap` waits, then dispatches `ApplyOp` of `SearchDialog.searchPictureEffects` for that text.
[x] `SearchDialog.searchPictureEffects` returns `RequestSearchPicture` of the current query and the zoom root when `SearchPicture.startAfterGap` accepts that text.
[x] `App.runEffect` matches `RequestSearchPicture` and calls `SearchPictureClient.runSearch`.
[x] `SearchPictureClient.runSearch` in `src/Client/SearchPictureClient.fs` posts the encoded `SearchPicture.Request` to `/search`.
[x] `SearchDialog.applyServerReply` calls `SearchPicture.keepReplyExcluding` with `current` set to `SearchPicture.Text` of the dialog query.
[x] `SearchPicture.applyReplyExcluding` in `src/Shared/SearchPicture.fs` returns `None` when `reply.matchKey` differs from `room.current`. `keepReplyExcluding` then returns the ids already kept.
[x] `Api.searchReply` in `src/Server/Api.fs` calls `door.recordStart`, then `SearchActor.reply`.
[x] `RouteRegistration.searchDoor` in `src/Server/RouteRegistration.fs` sets `recordStart` to `CoreMailboxApi.recordSearchStart` for the actor `search`.
[x] `SearchActor.actorStart` in `src/Server/SearchActor.fs` sets `focusId` to the graph root.
[x] `CoreMailboxLoop.scheduleActorOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls `CoreActorPool.startFunction`. A start for an actor node id that is already live returns the error `actor node already has a live Actor`.
[x] `SearchActor.searchActorAt` returns `ActorSucceeded`.
[x] `CoreActorPool.postStopUnlessCancelled` in `src/Server/Core/CoreActorPool.fs` posts `Notice.ActorStopped` of the row's actor node id and the returned result when the row token is open.
[x] `Api.searchReply` leaves `door.recordStop` uncalled. `RouteRegistration.searchDoor.recordStop` calls `CoreMailboxApi.recordSearchStop`.
[x] A client `EventBody.Cancel` is placed by `CoreMailboxApi.messageForPosted` as `Mailbox.PlaceOrder` of `Order.CancelActor`.
[x] `CoreMailboxLoop.placeOrder` matches `Order.CancelActor` and calls `CoreMailboxActors.replyPlacedCancel`.
[x] `replyPlacedCancel` calls `runCancel` when `storedBySubmission` finds no submission.
[x] `CoreMailboxActors.runCancel` reads `CoreActorPool.trySecretForActorNode`. It stores `EventBody.ActorStop` of that id and `ActorCancelled` through `CoreEventDispatch.appendLifecycle`, then calls `CoreActorPool.finish` with `ActorCancelled`.
[x] `EventBody.ActorStop` in `src/Shared/History.fs` builds `EventBody.Notice` of `Notice.ActorStopped`.
[x] `CoreActorPool.finish` cancels that row's token and removes the live row.
[x] `CoreActorPool.postStopUnlessCancelled` posts no second stop when that token is already cancelled.
[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand` and `RecordSearchStop`. `CoreMsg.PostEvent` has left.
[x] ``postSearch records ActorStart at the root`` in `tests/Server.Tests/SearchActorTests.fs` waits for `EventBody.ActorStop` of the root with `ActorSucceeded`.
[x] ``cancel by Focus accepts Cancelled and drops the live row`` in `tests/Server.Tests/CancelByFocusTests.fs` cancels a live actor and expects `ActorCancelled`.
[x] ``stale reply is ignored and a matching reply is kept`` in `tests/Shared.Tests/SearchPictureTests.fs` expects `applyReply` to return `None` for other text, and `keepReply` to return the ids already kept.

### Reference

[x] Story 13 **Dialog cancels or replaces the search actor** in the Search spec named in the One job line.
[x] Alan, 2026-11-15: closing the Find dialog cancels its pending server search actor. A committed change of the search text cancels the pending actor for the earlier text and starts the actor for the new text. The cancel is the existing actor cancel. The stored row is `Notice.ActorStopped` of `ActorCancelled`. `CoreMsg` stays the cases named in Working.
[x] [Size limit](../../context/size-limit.md) allows 8 source files and 4 tests.

## Do not load

[ ] `CoreMsg.RecordSearchStop` and `CoreMailboxApi.recordSearchStop`. 24 — Remove CoreMsg.RecordSearchStop owns that removal.
[ ] The residency list after an installed hit. 26 — Resumed typing finds server hits locally owns that list.
[ ] The Run line, `ActorQuery`, and the Focus node's children. [25 — The server evaluates Run](25-server-evaluates-run.md) owns that evaluation.
[ ] A new `Notice` case.
[ ] A new `CoreMsg` case.
[ ] File search.
[ ] `plan/online-search/reports/`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `closeSearchDialogOp` in `src/Client/SearchDialog.fs` so closing the dialog while a search actor is pending reaches `CoreMailboxActors.runCancel` for that actor. The focus id is the graph root from `SearchActor.actorStart`.
3. [ ] Change `searchPictureEffects` in `src/Client/SearchDialog.fs` so a quiet-gap commit that returns `RequestSearchPicture` cancels the pending actor for the earlier text through `runCancel`, then starts the actor for the new text.
4. [ ] Change the in-flight search so that the live row stays until the reply is produced or `runCancel` cancels the row. `SearchActor.searchActorAt` is that actor. `Api.searchReply` starts it through `recordSearchStart`. Today `searchActorAt` returns `ActorSucceeded` and `SearchActor.reply` runs on the request after that return.
5. [ ] `runCancel` stores `Notice.ActorStopped` of `ActorCancelled` and `CoreActorPool.finish` cancels the row token. `postStopUnlessCancelled` posts no second stop. The reachable entry is `placeOrder`'s `CancelActor` arm, `replyPlacedCancel`. Add no `Notice` case. Add no `CoreMsg` case.

## Outputs

[ ] Source files: `src/Client/SearchDialog.fs`, `src/Client/App.fs`, `src/Client/SearchPictureClient.fs`, `src/Server/Api.fs`, `src/Server/SearchActor.fs`, and `src/Server/RouteRegistration.fs`.
[ ] That list is 6 source files. The size limit in Reference names 8.
[ ] `tests/Server.Tests/SearchActorTests.fs` adds ``closing the dialog cancels the pending search actor``. A search actor is live at the root. After the close path, the log contains `EventBody.ActorStop` of the root with `ActorCancelled`, and the pool has no live row for that root.
[ ] The same file adds ``committed search text cancels the earlier actor and shows the new results``. A search actor for the earlier text is live. After the quiet-gap commit of a new text, that actor's stop is `ActorCancelled`, and `SearchDialog.currentSearchResults` lists the new search. A reply whose match key is the earlier text leaves that list.
[ ] That list is 2 tests. The size limit in Reference names 4.

## Stop when

[ ] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit adds a `Notice` case or a `CoreMsg` case.
[ ] Stop when the edit changes `CoreMsg.RecordSearchStop` or `CoreMailboxApi.recordSearchStop`.
[ ] Stop when the edit contradicts story 13 **Dialog cancels or replaces the search actor**.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``closing the dialog cancels the pending search actor``
  - `tests/Server.Tests`: ``committed search text cancels the earlier actor and shows the new results``
  - `tests/Server.Tests`: ``postSearch records ActorStart at the root``
  - `tests/Server.Tests`: ``cancel by Focus accepts Cancelled and drops the live row``
[ ] All executed tests pass.
[ ] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
