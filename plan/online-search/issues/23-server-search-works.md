# 23 — Server search works
**Status:** done
One job: The Server returns the Find hit Node ids and the Headers of those hits on the Want `nodes` list. The client adds those nodes to the residency Graph and then lists them. The dialog keeps the residency-search hits and appends the server hits whose Node ids the client search did not find, including a hit that was not resident before the reply. Alan, 2026-11-13: the residency-search hits appear, and the dialog is empty after the server response. The build finds that cause and fixes it until the tests pass. The reason is decision 11 **Want nodes** in [Online search](project.md).

Type: bug-fixing
Blockers: none

## Symptom

In interactive use the dialog lists the residency-search hits, and the dialog is empty after the server response. Server hits do not remain in the dialog.

## Code facts

[x] `SearchDialog.applyServerReply` installs the nodes on the Want `nodes` list, keeps the residency-search hits, and appends the server hits whose Node ids the client search did not find. Each id is listed once.
[x] `SearchDialog.serverOnlyRows` reads each appended id from `model.graph.nodes` after that install.
[x] `SearchPicture.keepReplyExcluding` removes excluded Node ids, then keeps the room under `Request.count`. Decode uses 200 when the request omits `count`.
[x] `SearchActor.reply` walks from `request.startId`. `SearchActor.actorStart` records the workspace root for ActorStop pairing. `SearchPictureClient.runSearch` sets `startId` to the zoom root.

## Inputs

### Working

[x] `RouteRegistration` maps `POST /ambit/search` to `postSearchRequest`. That function builds `searchDoor` for the browser caller and calls `Api.postSearch`.
[x] `searchDoor.recordStart` calls `CoreMailboxApi.recordSearchStart` with `install.search` when `persistence.Core.pool.bootActors` is present.
[x] `CreateBoot` sets `BootActors.search` to `SearchActor.searchActorAt`.
[x] `recordSearchStart` places `Order.StartActor` through `coreMailboxStartActorFromFunction`. The actor name is `search`. The actor node id comes from `ActorOrderFromStart.focusIdOf`.
[x] `Api.searchReply` builds `SearchActor.actorStart`, calls `door.recordStart`, and returns `SearchActor.reply`.
[x] `SearchPicture.Reply` is `ids` and `matchKey`. `encodeReply` writes `ids` plus `text` or `generation`.
[x] Decision 11 **Want nodes** says a Find reply includes the found Nodes as Want-fulfillment. Those Headers ride the existing Want answer `nodes` list. [Want nodes for hits](../../doc/current/want-nodes.md) records that same claim.
[x] `tests/Server.Tests/SearchActorTests.fs` test `postSearch returns one reply from the server graph` calls `Api.postSearch` with `acceptRecord` as `recordStart`.
[x] `tests/Server.Tests/TestBackend.fs` `createClientForDir` builds an HTTP client for a data directory with the development cookie. `StateEndpointTests.readFullState` reads `GET /ambit/state?scope=full`.
[x] The dialog path is `SearchDialog.applyServerReply`, `SearchDialog.serverOnlyRows`, and `SearchDialog.currentSearchResults`. The code facts above state what that path does today.
[x] `SearchPicture.Request` carries `text`, `startId`, and optional `generation`. `SearchPicture.encodeRequest` writes those fields.
[x] `tests/Server.Tests/SearchDialogTests.fs` test ``same text adds server hits and an empty reply keeps local rows`` calls `SearchDialog.applyServerReply` and reads `SearchDialog.currentSearchResults`.

### Reference

[x] [Search spec](spec.md) item 2 **Append**, item 5 **Node id**, and item 13 **Start fields**.
[x] [Search Actor](../../doc/current/search-actor.md) section **Uses**.

## Do not load

[x] `CoreMsg.RecordSearchStop`. [24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md) removes that case. The search stop is `Notice.ActorStopped` through the actor stop.
[x] The quiet gap and the query expression.
[x] A new package for hit Headers.
[x] A field of client-hit Node ids on the search start. The dialog excludes Node ids the client search found.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Add one test in `tests/Server.Tests/SearchActorTests.fs`. Follow `createClientForDir` and the `/ambit/events` post the Server suite already uses.
3. [x] The test places a node whose text is a known search term, then posts `SearchPicture.encodeRequest` to `POST /ambit/search` through that client.
4. [x] The test asserts the reply carries that node's id.
5. [x] The test asserts the reply carries that node's Header on the Want `nodes` list.
6. [x] Add one test in `tests/Server.Tests/SearchDialogTests.fs`. The residency search has one client hit. The reply contains that Node id and a second Node id. The client search did not find the second id. The second node is absent from the residency Graph before the reply. The reply carries that second node on the Want `nodes` list.
7. [x] The test asserts `SearchDialog.currentSearchResults` keeps the client hit and appends the second hit, so a server hit that is not resident before the reply appears in the dialog after the reply.
8. [x] The test asserts that client Node id appears once.
9. [x] Change: install the nodes the reply carries on the Want `nodes` list into the residency Graph, then append the server hits whose Node ids the client search did not find.
10. [x] The dialog is empty after the server response. When a test fails, find the cause and fix it until the tests pass.
11. [x] The server search path is `postSearchRequest`, `searchDoor`, `recordSearchStart`, `Api.searchReply`, `SearchActor.reply`, and `SearchPicture` reply encoding.
12. [x] The dialog path is `SearchDialog.applyServerReply`, `SearchDialog.serverOnlyRows`, and `SearchDialog.currentSearchResults`.

## Outputs

[x] `tests/Server.Tests/SearchActorTests.fs` gains ``server search returns the hits and their Want nodes``.
[x] `tests/Server.Tests/SearchDialogTests.fs` gains ``dialog appends a server hit that was not resident``.
[x] Source files on the server search path and the dialog path change only as those failing tests require.

## Stop when

[x] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the cause sits outside the server search path in Process step 11 and the dialog path in Process step 12.
[ ] Stop when the fix adds a package other than the Want `nodes` list.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``server search returns the hits and their Want nodes``
  - `tests/Server.Tests`: ``dialog appends a server hit that was not resident``
[x] All executed tests pass.
[x] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
