# 26 — Resumed typing finds server hits locally

**Status:** defined

One job: A person who changes the Find search text after a server reply sees matching nodes that reply installed, listed by the residency search, before any new server reply. The reason is story 12 **Resumed typing** in [Search spec](../spec.md).

Type: coding

Blockers:

[23 — Server search works](23-server-search-works.md) is done. The Server returns Find hit Headers on the Want answer `nodes` list. The client adds those Headers to the residency Graph. The dialog appends a server hit that was not Resident before the reply.

[24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md) is defined. This ticket starts while that removal is still ahead. The Find stop stays `Notice.ActorStopped` through the actor stop.

## Inputs

### Working

[x] `installWantAnswer` in `src/Shared/ResidentProjection.fs` takes `edges`, `nodes`, and `graph`.
[x] Each node in `nodes` is added to `graph.nodes` by `node.id`. A carried Header replaces a node with the same id.
[x] Each entry in `edges` is added to `graph.childMap` by parent id. A supplied parent replaces that parent's child list.
[x] The function returns `Graph.fromNodes` of `graph.root`, the merged nodes, and the merged child map.
[x] `Graph.fromNodes` in `src/Shared/GraphBuild.fs` builds `parentByChild` and `ownerParentByChild` from the child map.
[x] `applyOwnerField` sets `node.owner` from `ownerParentByChild` when that map contains the id. The Header's owner field stays when the map has no entry for that id.
[x] `Graph.fromNodes` keeps the canonical TRASH, SYSTEM, and Workspaces nodes and their owner edges. That rebuild adds no parent edge for a hit Header.
[x] `SearchDialog.installCarried` in `src/Client/SearchDialog.fs` calls `installWantAnswer` with an empty edge map and the carried Header list.
[x] `Api.searchReply` in `src/Server/Api.fs` selects a node for each id in `answer.ids` when the server Graph contains that id.
[x] `answer.ids` are the hit ids from `SearchActor.reply`. An ancestor Header is on that `nodes` list when the ancestor id is one of those hit ids.
[x] `SearchPicture.encodeReplyWithNodes` in `src/Shared/SearchPicture.fs` writes that list on the Want answer `nodes` field.
[x] `ViewModelSearch.startSearch` in `src/Shared/ViewModelSearch.fs` queues the zoom root. `nextDiscoveryNode` follows `GraphChildren.get`. After the zoom queue is empty, the walk queues `graph.root` and follows children from there.
[x] `SearchDialog.currentSearchResults` reads that walk from the search cache, then appends rows for `serverHitIds`.
[x] `Update.update` for `NodeSearchQuery` in `src/Client/Update.fs` clears `serverHitIds`, sets `selectedIndex` to 0, and calls `SearchDialog.resetSearchResults`. The effect is `ArmSearchQuietGap`.
[x] ``dialog appends a server hit that was not resident`` in `tests/Server.Tests/SearchDialogTests.fs` calls `SearchDialog.applyServerReply` with one absent Header and reads `SearchDialog.currentSearchResults` for that same search text.

### Reference

[x] Story 12 **Resumed typing** in the Search spec named in the One job line.
[x] Alan, 2026-11-15: after a server reply installs hits, a changed search text lists those nodes from the residency search, before any new server reply. The test includes a hit whose parent was not Resident before the reply. That test establishes whether the next local search lists the hit.
[x] [Size limit](../../context/size-limit.md) allows 8 source files and 4 tests.
[x] [Want nodes for hits](../../doc/current/want-nodes.md). Hit Headers ride the Want answer `nodes` list. `installWantAnswer` merges `nodes` and `childMap`.

## Do not load

[ ] `CoreMsg.RecordSearchStop` and the Find stop path on 24 — Remove CoreMsg.RecordSearchStop.
[ ] The query expression and a Run line.
[ ] File search.
[ ] A new reply field. Hit Headers stay on the Want answer `nodes` list.
[ ] `plan/online-search/reports/`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand: add ``changed search text lists an installed hit whose parent was absent`` in `tests/Server.Tests/SearchDialogTests.fs`.
3. [ ] The test builds a hit whose parent is absent from the residency Graph. The reply carries that hit's Header on the Want answer `nodes` list. The parent Header stays off that list. `SearchDialog.applyServerReply` installs the reply.
4. [ ] The test then sends `NodeSearchQuery` for a search text the local search matches on that hit. That message clears `serverHitIds`. The test reads `SearchDialog.currentSearchResults` before any new server reply. With `serverHitIds` empty, that list is `ViewModelSearch.startSearch` over the residency from the zoom root.
5. [ ] That read establishes whether the residency search lists the hit whose parent was not Resident before the reply. The test asserts that the list contains the hit.
6. [ ] Change: when that assertion fails, edit a source file named in Outputs until the residency search lists the hit. The first test result, before that edit, is the reachability fact for the build line.

## Outputs

[ ] `tests/Server.Tests/SearchDialogTests.fs` adds ``changed search text lists an installed hit whose parent was absent``.
[ ] That list is 1 test. The size limit in Reference names 4.
[ ] A failing test may change `src/Client/SearchDialog.fs`.
[ ] A failing test may change `src/Shared/ResidentProjection.fs`.
[ ] A failing test may change `src/Shared/ViewModelSearch.fs`.
[ ] A failing test may change `src/Server/Api.fs`.
[ ] That source list is 4 files. The size limit in Reference names 8.

## Stop when

[ ] Rewrite `26-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The build line states the first result of ``changed search text lists an installed hit whose parent was absent``: the residency search listed that hit, or the residency search omitted that hit.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit adds a reply field other than the Want answer `nodes` list.
[ ] Stop when the edit contradicts story 12 **Resumed typing**.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``changed search text lists an installed hit whose parent was absent``
  - `tests/Server.Tests`: ``dialog appends a server hit that was not resident``
[ ] All executed tests pass.
[ ] Rewrite `26-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
