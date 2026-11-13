Check: pass

# 23 — Server search works

Ticket: [[../23-server-search-works.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff 6c9ada45^` on commit `6c9ada45` exited 1. The measure lines are `installCarried` at 13 lines, `clientFoundIds` at 5, `wantNodes` at 9, `applyReplyExcluding` at 18, `applyReply` at 7, `keepReplyExcluding` at 13, `keepReply` at 8, `encodeIds` at 5, `encodeNodes` at 5, `encodeMatch` at 5, `encodeReplyWithNodes` at 6, and `decodeWantNodes` at 7. Each is under the 40-line limit. No `LONG`, `TAB`, `MUTABLE`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Hard.** `SearchPicture.keepReplyExcluding` takes five positional arguments: `clientHitCount`, `current`, `kept`, `reply`, and `exclude`. `.agents/rules/fsharp-source.md` says to extend an existing type when a new parameter belongs with the ones already there.
- **Scan names.** The same scan printed `BARE_ID` for `item 3`, `item 7`, and `item 14` on line 40 of [23 — Server search works](../23-server-search-works.md). That line names **Append**, **Node id**, and **Start fields**. Those names satisfy `.agents/rules/refer-by-name.md`.
- **Judgement.** Duplicated Code. `SearchDialog.applyServerReply` calls `clientHitCount` and then `clientFoundIds`. Each calls `ViewModelSearch.searchNodes` with the same query, the same zoom root, and the same graph.

```fsharp
let hits = clientHitCount model s.query
let ids =
    SearchPicture.keepReplyExcluding
        hits
        (SearchPicture.Text s.query)
        s.serverHitIds
        reply
        (clientFoundIds model s.query)
```

- **Rules.** The source diff matches `.agents/rules/core-api.md` and `.agents/rules/writing-markdown.md`.

## 2. Spec

- **Append.** `SearchDialog.applyServerReply` calls `installCarried` before it reads hits. `installCarried` calls `ResidentProjection.installWantAnswer` with an empty edge map and the carried nodes. `SearchPicture.keepReplyExcluding` drops Node ids from `clientFoundIds`. `currentSearchResults` keeps the residency rows. `serverOnlyRows` appends each remaining id that `Map.tryFind` finds in `model.graph.nodes`.
- **Empty dialog.** `serverOnlyRows` drops a hit that is absent from `model.graph.nodes`. The install runs before that read. ``dialog appends a server hit that was not resident`` keeps the residency hit and shows the hit that was absent before the reply.
- **Zoom root.** `SearchActor.reply` sets the walk zoom to `request.startId`. `SearchActor.actorStart` still sets `zoomId`, `focusId`, `runDefinitionId`, and `graphIds` to `graph.root`.
- **Full database.** The walk starts at that zoom, then the root phase continues from `graph.root`. A hit under the workspace root stays in the result when the zoom is a different node.
- **Cap.** `applyReplyExcluding` removes the excluded ids, then `List.truncate` keeps the room under 200. `SearchActor.oneReply` still stops the walk at `ViewModelSearch.searchHitCap`, which is 200.
- **Want nodes.** `Api.searchReply` sends `SearchPicture.encodeReplyWithNodes` with the headers for `answer.ids`. `SearchPictureClient.wantNodes` reads that list with `decodeWantNodes` and passes it to `applyServerReply`.

## 3. Arch

- **Find and Move reply.** Section 3 **Find and Move reply** requires the full-database walk to start at the zoom root on `request.startId`. `SearchActor.reply` passes `request.startId`. ``reply walks from the request start`` expects the zoom hit, then the hit under the workspace root.
- **Root-only start.** Section 3 **Root-only start** keeps `actorStart` on the workspace root so ActorStop pairs with that id. `actorStart` is unchanged. ``postSearch records ActorStart at the root`` posts a `startId` other than the workspace root and expects ActorStart and ActorStop on `graph.root`.
- **Search dialog.** Section 3 **Search dialog** requires `applyServerReply` to store the server ids, install the carried nodes, and append the remaining hits. The function does that. The signature block in that section still shows `SearchPicture.Reply -> VM -> VM * Effect list`. The function also takes the carried `Node list`.
- **Module map.** Module map 9 **Find and Move reply** uses Local search. `reply` calls `oneReply`, which calls `startSearch` and `takeResults`. Module map 5 **Search transport** is `runSearch`, which sets `startId` to the zoom root.
- **Seam note.** Section 6 seam 1 still names ``reply walks from the root``. The test on the branch is ``reply walks from the request start``.

## 4. Tests

- **Suite command.** `bash scripts/check-test.sh 3035` printed `DONE` and `/tmp/ambit-full-suite-20261113-185735.log`. That launch stayed inside `GitSaveEndpointTests` at `ServerTestReset.stopAll`. The completed runs below are the counts.
- **Server.** `dotnet test tests/Server.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1` passed 774, failed 0, skipped 1. Total 775. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `dotnet test tests/Shared.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1` passed 1944, failed 0, skipped 3. Total 1947. The skips are ``listForPush without git still returns walk``, ``reuses existing owned path without duplicating``, and ``planServerFilePresentOps transitions only matching absent files``.
- **HTTP path.** ``server search returns the hits and their Want nodes`` creates a node, posts `SearchPicture.encodeRequest` to `POST /ambit/search`, and reads that id plus the header text and id from the `nodes` field. `startId` on that request is the workspace root.
- **Dialog path.** ``dialog appends a server hit that was not resident`` calls `SearchDialog.applyServerReply` with the client id, an absent id, and that absent node. `currentSearchResults` returns the client hit, then the absent hit. The client id appears once. `serverOnlyRows` performs that append. The absent node stays detached, so the residency search leaves it for the append.
- **Zoom.** ``reply walks from the request start`` calls `SearchActor.reply` with `startId` set to the zoom node. The ids are the zoom hit, then the hit under the workspace root. Outputs leaves this test unnamed.
- **Cap.** ``client hits plus the reply stop at the shared cap`` calls `applyReply`. That path uses an empty exclude set. A room filled only after a non-empty exclude set is outside the named tests.
- **Duplicates.** The dialog test checks that the client id appears once. The walk visits each node once. A reply that repeats a server-only id is outside the named tests.
- **Outputs.** Both named tests exist. Each checks the Outputs line that names it.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Accepted.** **Problem.** [Search Actor](../../../../doc/current/search-actor.md), section **Messages**, still says the walk starts at the workspace root and that `SearchActor.reply` does not read `request.startId`. The result example has no `nodes` list. **Fix.** Record that both searches start at the zoom root, and that the result carries hit Headers on the Want answer `nodes` list.
- **Rejected.** **Problem.** [Online search](../../project.md) still marks [23 — Server search works](../23-server-search-works.md) as `defined`. The ticket Status line is still `defined`. **Fix.** Set the frontier line and the ticket Status to the stage this build leaves. **Weigh.** The frontier entry is one sentence. It has no checkbox. This review leaves that sentence and the ticket file as they stand.
