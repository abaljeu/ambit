# Search Actor
Category: Capability
See Also:
- [Actors](actors.md)
- [Mailbox](mailbox.md)
- [Server](server.md)
- [Query Actor](query-actor.md)
- [Want nodes for hits](want-nodes.md)
- [Workspace graph](graph.md)

Find and Move share one Actor. Query does not use this Actor.

## Sources

[GraphBuild](../../src/Shared/GraphBuild.fs) — TRASH id `trashId`
[History](../../src/Shared/History.fs) — `Order.StartActor` of `ActorOrder`
[ViewModelSearch](../../src/Shared/ViewModelSearch.fs) — `startSearch` and `takeResults`

## Data

[x] One walk, off the mailbox, then stop. No continuation cursor.
[ ] Trash: the walk skips trash unless the start Node is TRASH. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[x] The server list lists each Node id once. `SearchDialog.applyServerReply` installs the Want answer `nodes` list, keeps the residency hits, and appends the server hits whose Node ids the client search did not find.
[x] Cap: the client search stops at 200 hits. The server walk stops at `SearchPicture.Request.count`. The client reply room is the space under `count` after excluded Node ids leave. `SearchDialog.searchPictureCount` is that cap on the client request and on the quiet-gap gate. Find and Move share that cap.

## Job

[x] Shared backend for Find and Move. One running Actor. Move does not start a second Actor.
[o] A keypress does not start this Actor. Client recompute stays on [Workspace graph](graph.md).
[ ] A keypress does not start this Actor when the globe is not selected. An edit starts a server search when the globe is selected.

## Interface

[o] Start: Find and Move start this Actor after the quiet gap. Start supplies the full server Graph.
[ ] Start: the globe starts this Actor when the residency search has fewer than 200 client hits.
[x] Result: one reply of at most `SearchPicture.Request.count` hits, then stop. No continuation cursor.

## Messages

[x] Start fields: `text` (string). Optional `generation` (number) for the reply match. `startId` (string, Node id) is the zoom root. Optional `count` (number) is the result cap. Decode uses 200 when `count` is absent.
[x] The client search reads the residency Graph from the zoom root. The server search reads the full database from `request.startId`.
[x] `SearchActor.actorStart` records the workspace root. `zoomId`, `focusId`, `runDefinitionId`, and `graphIds` are that root. ActorStop pairs on that id.

```json
{
  "text": "quarterly",
  "startId": "550e8400-e29b-41d4-a716-446655440000",
  "count": 200
}
```

```json
{
  "text": "quarterly",
  "generation": 3,
  "startId": "550e8400-e29b-41d4-a716-446655440000",
  "count": 200
}
```

[x] Result fields: `ids` (array of Node id strings), hit Headers on the Want answer `nodes` list, and the reply-match `text` (string) or `generation` (number).

```json
{
  "ids": [ "550e8400-e29b-41d4-a716-446655440001" ],
  "text": "quarterly",
  "nodes": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "text": "quarterly report",
      "name": null,
      "cssClasses": [],
      "kind": "normal",
      "documentState": "current",
      "parseState": "parsed",
      "persistState": "persisted",
      "persistedVersion": 0,
      "changedVersion": 0,
      "updateTime": 0
    }
  ]
}
```

```json
{
  "ids": [ "550e8400-e29b-41d4-a716-446655440001" ],
  "generation": 3,
  "nodes": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "text": "quarterly report",
      "name": null,
      "cssClasses": [],
      "kind": "normal",
      "documentState": "current",
      "parseState": "parsed",
      "persistState": "persisted",
      "persistedVersion": 0,
      "changedVersion": 0,
      "updateTime": 0
    }
  ]
}
```

[x] Reply match: the caller drops the result when that search text or generation is not current.

The Browser posts the start JSON to `POST /ambit/search`. The `200` body is the result JSON.

## Uses

[x] Server Graph: the walk uses [startSearch](../../src/Shared/ViewModelSearch.fs) and [takeResults](../../src/Shared/ViewModelSearch.fs) on the full server Graph from `request.startId`. Those functions take the search text, that zoom, and the Graph. Detail: [Server](server.md).
[x] Find and Move: `recordSearchStart` places `Order.StartActor` of `ActorOrder`. The actor name is `search`. The actor node id is the root, taken from `ActorStart.focusId`. The actor is `BootActors.search`.
[x] Install: `RouteRegistration.CreateBoot` sets `BootActors.search` to `SearchActor.searchActorAt`. `searchDoor` passes that value into `recordSearchStart`. A pool with no install replies `search actor is not installed`.
[x] Reply: `Api.searchReply` places the start, then returns `SearchActor.reply`. The Search Actor posts `ActorStop` of `ActorSucceeded` at the root. The walk reads the full server Graph from `Query.State`. Search does not read a focus.
[x] Door: [Api](../../src/Server/Api.fs) `SearchActorDoor` has `changes`, `recordStart`, and `recordStop`. `changes` asks `Query.State`. `CoreMsg` has `RecordSearchStop`.
[x] Stop: `SearchActor.searchActorAt` calls `changes.actorStop` with `ActorSucceeded`. `CoreEventDispatch.actorStop` logs that result as `Notice.ActorStopped`. Find logs the stop from that actor call.
[ ] Stop: [24 — Remove CoreMsg.RecordSearchStop](../../plan/online-search/issues/24-remove-coremsg-recordsearchstop.md) removes `CoreMsg.RecordSearchStop`. `searchDoor.recordStop` becomes a function that returns unit.
[x] A refused Find and Move start returns `400`. The body is `{ "error": "<failure>" }`.

## Seams

[o] Quiet gap: one Start after the search text is unchanged. The gap has no millisecond value. A text change before the gap ends sends no Start. No Start when the client already has 200 hits.
[ ] Globe: the globe on the search bar requests the server when the residency search has fewer than 200 client hits. An edit follows the globe.
[ ] Cap: Find and Move share 200. [Query Actor](query-actor.md) stops at that same cap.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The client algorithm stops at 200 first. The Actor uses that same algorithm. The globe requests the server when the residency search has fewer than 200 client hits. Hit Headers for that reply ride the Want answer `nodes` list.
