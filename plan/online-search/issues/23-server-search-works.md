# 23 — Server search works
**Status:** defined
One job: The Server answers `POST /ambit/search` with the expected hit Node ids and the Headers of those hits on the Want `nodes` list. Server search is reported broken, so the build finds the cause and fixes it until that test passes. The reason is decision 11 **Want nodes** in [Online search](project.md).

Type: bug-fixing
Blockers: none

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

### Reference

[ ] [Search spec](spec.md) item 7 **Want nodes** and item 14 **Start fields**.
[ ] [Search Actor](../../doc/current/search-actor.md) section **Uses**.

## Do not load

[ ] `CoreMsg.RecordSearchStop` and `Notice.SearchStopped`. [24 — Post Notice.SearchStopped](24-post-searchstopped-notice.md) owns that replacement.
[ ] The globe, the quiet gap, and the query expression.
[ ] A new package for hit Headers.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Add one test in `tests/Server.Tests/SearchActorTests.fs`. Follow `createClientForDir` and the `/ambit/events` post the Server suite already uses.
3. [ ] The test places a node whose text is a known search term, then posts `SearchPicture.encodeRequest` to `POST /ambit/search` through that client.
4. [ ] The test asserts the reply carries that node's id.
5. [ ] The test asserts the reply carries that node's Header on the Want `nodes` list.
6. [ ] Server search is reported broken. When the test fails, find the cause on the server search path and fix it until the test passes.
7. [ ] The server search path is `postSearchRequest`, `searchDoor`, `recordSearchStart`, `Api.searchReply`, `SearchActor.reply`, and `SearchPicture` reply encoding.

## Outputs

[ ] `tests/Server.Tests/SearchActorTests.fs` gains ``server search returns the hits and their Want nodes``.
[ ] Source files on the server search path change only as that failing test requires.

## Stop when

[ ] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the cause sits outside the server search path named in Process step 7.
[ ] Stop when the fix adds a package other than the Want `nodes` list.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``server search returns the hits and their Want nodes``
[ ] All executed tests pass.
[ ] Rewrite `23-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
