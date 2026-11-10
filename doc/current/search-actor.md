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
[ ] The server list lists each Node id once. The reply replaces the client list. The start does not carry shown Node ids.
[x] Cap: the client search algorithm stops at 200 hits. The reply holds at most 200 Node ids. Find and Move share that cap.

## Job

[x] Shared backend for Find and Move. One running Actor. Move does not start a second Actor.
[o] A keypress does not start this Actor. Client recompute stays on [Workspace graph](graph.md).
[ ] A keypress does not start this Actor when the globe is not selected. An edit starts a server search when the globe is selected.

## Interface

[o] Start: Find and Move start this Actor after the quiet gap. Start supplies the root and the full server Graph. The zoom is the root. Search does not take a focus.
[ ] Start: the globe starts this Actor when the local list is under 200. Start supplies the root and the full server Graph. The zoom is the root. Search does not take a focus. The start does not carry shown Node ids.
[o] Result: the Actor uses the client search algorithm and returns at most 200 hits, then stops. Client hits plus this reply stop at the cap. One reply. No continuation cursor.
[ ] Result: the Actor returns at most 200 hits, then stops. When the search text is unchanged, the reply replaces the client list. One reply. No continuation cursor.

## Messages

[x] Start fields: `text` (string). Optional `generation` (number) for the reply match. `startId` (string, Node id) is on the wire. The walk does not use `startId` as a zoom or a focus. The zoom is the root. The full server Graph comes from State. Search does not take a focus. The start does not carry shown Node ids.

```json
{
  "text": "quarterly",
  "startId": "550e8400-e29b-41d4-a716-446655440000"
}
```

```json
{
  "text": "quarterly",
  "generation": 3,
  "startId": "550e8400-e29b-41d4-a716-446655440000"
}
```

[x] Result fields: Node ids, and the reply-match search text or generation. The response has `ids` (array of Node id strings, at most 200) and the reply-match `text` (string) or `generation` (number).

```json
{
  "ids": [ "550e8400-e29b-41d4-a716-446655440001" ],
  "text": "quarterly"
}
```

```json
{
  "ids": [ "550e8400-e29b-41d4-a716-446655440001" ],
  "generation": 3
}
```

[x] Reply match: the caller drops the result when that search text or generation is not current.

The Browser posts the start JSON to `POST /ambit/search`. The `200` body is the result JSON.

## Uses

[x] Server Graph: the walk uses [startSearch](../../src/Shared/ViewModelSearch.fs) and [takeResults](../../src/Shared/ViewModelSearch.fs) on the full server Graph. Those functions take the search text, the zoom, and the Graph. The zoom is the root. Detail: [Server](server.md).
[x] Find and Move: `recordSearchStart` places `Order.StartActor` of `ActorOrder`. The actor name is `search`. The actor node id is the root, taken from `ActorStart.focusId`. The actor is `BootActors.search`.
[x] Install: `RouteRegistration.CreateBoot` sets `BootActors.search` to `SearchActor.searchActorAt`. `searchDoor` passes that value into `recordSearchStart`. A pool with no install replies `search actor is not installed`.
[x] Reply: `Api.searchReply` places the start, then returns `SearchActor.reply`. The Search Actor posts `ActorStop` of `ActorSucceeded` at the root. The walk reads the full server Graph from `Query.State`. Search does not read a focus.
[x] Door: [Api](../../src/Server/Api.fs) `SearchActorDoor` has `changes`, `recordStart`, and `recordStop`. `changes` asks `Query.State`. `CoreMsg` has `RecordSearchStop`.
[ ] Door: `recordStop` posts `Notice.SearchStopped` of the root id through the notice function. It records the stop as `ActorSucceeded` for that root. [Message shape](../../plan/core-refinement/cqrs-messages.md) §2 StartActor and §2 Notice.
[x] A refused Find and Move start returns `400`. The body is `{ "error": "<failure>" }`.

## Seams

[o] Quiet gap: one Start after the search text is unchanged. The gap has no millisecond value. A text change before the gap ends sends no Start. No Start when the client already has 200 hits.
[ ] Globe: the globe on the search bar requests the server when the local list is under 200. When the search text is unchanged, the reply replaces the client list. An edit follows the globe.
[ ] Cap: Find and Move share 200. [Query Actor](query-actor.md) stops at that same cap.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The client algorithm stops at 200 first. The Actor uses that same algorithm. The globe requests the server when the local list is under 200. Hit Headers for that reply ride [Want nodes for hits](want-nodes.md).
