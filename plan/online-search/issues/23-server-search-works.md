# 23 — Server search works
**Status:** defined
One job: The Server returns the Find hit Node ids and the Headers of those hits on the Want `nodes` list. The Find dialog keeps the client list and appends server hits whose Node ids are absent from that list. Alan, 2026-11-13: the client results appear, and the server response then shows the dialog as empty. The build finds that cause and fixes it until the tests pass. The reason is decision 10 **Globe** and decision 11 **Want nodes** in [Online search](project.md).

Type: bug-fixing
Blockers: none

## Symptom

In interactive use there are no server results. The client results appear. The server response then shows the dialog as empty.

## Inputs

### Working

[ ] `RouteRegistration` maps `POST /ambit/search` to `postSearchRequest`. That function builds `searchDoor` for the browser caller and calls `Api.postSearch`.
[ ] `searchDoor.recordStart` calls `CoreMailboxApi.recordSearchStart` with `install.search` when `persistence.Core.pool.bootActors` is present.
[ ] `CreateBoot` sets `BootActors.search` to `SearchActor.searchActorAt`.
[ ] `recordSearchStart` places `Order.StartActor` through `coreMailboxStartActorFromFunction`. The actor name is `search`. The actor node id comes from `ActorOrderFromStart.focusIdOf`.
[ ] `Api.searchReply` builds `SearchActor.actorStart`, calls `door.recordStart`, and returns `SearchActor.reply`.
[ ] `SearchActor.reply` walks the carrier graph from the root with `ViewModelSearch.startSearch` and `ViewModelSearch.takeResults`.
[ ] `SearchPicture.Reply` is `ids` and `matchKey`. `encodeReply` writes `ids` plus `text` or `generation`.
[ ] Decision 11 **Want nodes** says a Find reply includes the found Nodes as Want-fulfillment. Those Headers ride the existing Want answer `nodes` list. [Want nodes for hits](../../doc/current/want-nodes.md) records that same claim.
[ ] `tests/Server.Tests/SearchActorTests.fs` test `postSearch returns one reply from the server graph` calls `Api.postSearch` with `acceptRecord` as `recordStart`.
[ ] `tests/Server.Tests/TestBackend.fs` `createClientForDir` builds an HTTP client for a data directory with the development cookie. `StateEndpointTests.readFullState` reads `GET /ambit/state?scope=full`.
[ ] The filter is on the client. `SearchDialog.serverOnlyRows` drops reply Node ids that are already in the local list. `SearchDialog.currentSearchResults` appends the remaining rows after the local rows. `SearchDialog.applyServerReply` stores the reply ids on `serverHitIds`.
[ ] `SearchPicture.Request` carries `text`, `startId`, and optional `generation`. `SearchPicture.encodeRequest` writes those fields. The start does not carry shown Node ids.
[ ] `SearchActor.reply` returns the walk of the server Graph. The server does not receive shown Node ids.
[ ] `tests/Server.Tests/SearchDialogTests.fs` test ``same text adds server hits and an empty reply keeps local rows`` calls `SearchDialog.applyServerReply` and reads `SearchDialog.currentSearchResults`.

### Reference

[ ] [Search spec](spec.md) item 3 **Append**, item 7 **Node id**, and item 14 **Start fields**.
[ ] [Search Actor](../../doc/current/search-actor.md) section **Uses**.

## Do not load

[ ] `CoreMsg.RecordSearchStop` and `Notice.SearchStopped`. [24 — Post Notice.SearchStopped](24-post-searchstopped-notice.md) owns that replacement.
[ ] The globe control, the quiet gap, and the query expression.
[ ] A new package for hit Headers.
[ ] A shown-Node-id field on the search start. The client filters the reply.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Add one test in `tests/Server.Tests/SearchActorTests.fs`. Follow `createClientForDir` and the `/ambit/events` post the Server suite already uses.
3. [ ] The test places a node whose text is a known search term, then posts `SearchPicture.encodeRequest` to `POST /ambit/search` through that client.
4. [ ] The test asserts the reply carries that node's id.
5. [ ] The test asserts the reply carries that node's Header on the Want `nodes` list.
6. [ ] Add one test in `tests/Server.Tests/SearchDialogTests.fs`. The Find dialog already shows one client Node id. The reply contains that Node id and a second Node id that is in the Graph and absent from the client list.
7. [ ] The test asserts `SearchDialog.currentSearchResults` keeps the client row and appends the second row.
8. [ ] The test asserts that client Node id appears once.
9. [ ] The client results appear, and the server response then shows the dialog as empty. When a test fails, find the cause and fix it until the tests pass.
10. [ ] The server search path is `postSearchRequest`, `searchDoor`, `recordSearchStart`, `Api.searchReply`, `SearchActor.reply`, and `SearchPicture` reply encoding.
11. [ ] The dialog path is `SearchDialog.applyServerReply`, `SearchDialog.serverOnlyRows`, and `SearchDialog.currentSearchResults`.

## Outputs

[ ] `tests/Server.Tests/SearchActorTests.fs` gains ``server search returns the hits and their Want nodes``.
[ ] `tests/Server.Tests/SearchDialogTests.fs` gains ``dialog appends server hits the client did not find``.
[ ] Source files on the server search path and the dialog path change only as those failing tests require.

## Stop when

[ ] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the cause sits outside the server search path in Process step 10 and the dialog path in Process step 11.
[ ] Stop when the fix adds a package other than the Want `nodes` list.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``server search returns the hits and their Want nodes``
  - `tests/Server.Tests`: ``dialog appends server hits the client did not find``
[ ] All executed tests pass.
[ ] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
