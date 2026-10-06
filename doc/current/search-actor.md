# Search Actor
Category: Capability
See Also:
- [Actors](actors.md)
- [Mailbox](mailbox.md)
- [Server](server.md)
- [Query Actor](query-actor.md)
- [Workspace graph](graph.md)

Find and Move share one Actor. Query does not use this Actor.

## Sources

[GraphBuild](../../src/Shared/GraphBuild.fs) — TRASH id `trashId`
[History](../../src/Shared/History.fs) — `ActorStart`
[ViewModelSearch](../../src/Shared/ViewModelSearch.fs) — `startSearch` and `takeResults`
[Mailbox](mailbox.md) — the event source records that `ActorStart`

## Data

[x] One walk, off the mailbox, then stop. No continuation cursor.
[ ] Trash: the walk skips trash unless the start Node is TRASH. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[ ] Dedup: the walk drops a Node id that is in the shown Node ids.
[x] Cap: the client search algorithm stops at 200 hits. The reply holds at most 200 Node ids. Find and Move share that cap.

## Job

[x] Shared backend for Find and Move. One running Actor. Move does not start a second Actor.
[x] A keypress does not start this Actor. Client recompute stays on [Workspace graph](graph.md).

## Interface

[x] Start: Find and Move start this Actor after the quiet gap. Start supplies root, focus, and the full server Graph.
[x] Result: the Actor uses the client search algorithm and returns at most 200 hits, then stops. Client hits plus this reply stop at the cap. One reply. No continuation cursor.

## Messages

[ ] Start fields: root, focus, the full server Graph, search text or an equivalent generation, the start Node, and the shown Node ids.
[ ] The request carries `text` (string) or `generation` (number), one of the two, plus `startId` (string, Node id) and `shownIds` (array of Node id strings).

```json
{
  "text": "quarterly",
  "startId": "550e8400-e29b-41d4-a716-446655440000",
  "shownIds": []
}
```

```json
{
  "generation": 3,
  "startId": "550e8400-e29b-41d4-a716-446655440000",
  "shownIds": [ "550e8400-e29b-41d4-a716-446655440001" ]
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

[x] Server Graph: the walk uses [startSearch](../../src/Shared/ViewModelSearch.fs) and [takeResults](../../src/Shared/ViewModelSearch.fs) on the full server Graph. Those functions take the search text, the zoom, and the Graph. The walk may ignore focus. Detail: [Server](server.md).
[x] ActorStart: the running Actor is recorded on the event source as `ActorStart`. That record supplies root and focus. `graphIds` is the root. The walk reads the full server Graph from State at that start. One reply, then `ActorStop`. Detail: [Mailbox](mailbox.md).

## Seams

[x] Quiet gap: one Start after the search text is unchanged. The gap has no millisecond value. A text change before the gap ends sends no Start. No Start when the client already has 200 hits.
[ ] Cap: Find and Move share 200. [Query Actor](query-actor.md) stops at that same cap.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The client algorithm stops at 200 first. The Actor uses that same algorithm. The quiet gap keeps Start off the keypress.
