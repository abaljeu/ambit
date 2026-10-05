# Search Actor
Category: Capability
See Also:
- [Actors](actors.md)
- [Mailbox](mailbox.md)
- [Server](server.md)
- [Query Actor](query-actor.md)
- [Workspace graph](workspace-graph.md)

Find and Move share one Actor. Query does not use this Actor.

## Sources

[GraphBuild](../../src/Shared/GraphBuild.fs) — TRASH id `trashId`
[History](../../src/Shared/History.fs) — `ActorStart`
[Mailbox](mailbox.md) — the event source records that `ActorStart`

## Data

[ ] One walk, off the mailbox, then stop. No continuation cursor.
[ ] Trash: the walk skips trash unless the start Node is TRASH. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[ ] Dedup: the walk drops a Node id that is in the shown Node ids.
[ ] Cap: the result holds at most 200 Node ids. Find and Move share that cap.

## Job

[ ] Shared backend for Find and Move. One running Actor. Move does not start a second Actor.
[ ] A keypress does not start this Actor. Client recompute stays on [Workspace graph](workspace-graph.md).

## Interface

[ ] Start: Find and Move start this Actor after the quiet gap.
[ ] Result: the Actor returns one result and stops. Client hits plus this result stop at the cap.

## Messages

[ ] Start fields: search text or an equivalent generation, the start Node, and the shown Node ids.
[ ] Result fields: Node ids, and the reply-match search text or generation.
[ ] Reply match: the caller drops the result when that search text or generation is not current.

## Uses

[ ] Server Graph: the Actor reads the server Graph. Detail: [Server](server.md).
[ ] ActorStart: the running Actor is recorded on the event source as `ActorStart`. Detail: [Mailbox](mailbox.md).

## Seams

[ ] Quiet gap: one Start after the search text is unchanged. The gap has no millisecond value. A text change before the gap ends sends no Start. No Start when the client already has 200 hits.
[ ] Cap: Find and Move share 200. [Query Actor](query-actor.md) stops at that same cap.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The quiet gap keeps Start off the keypress.
