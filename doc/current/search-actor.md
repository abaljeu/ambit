# Search Actor

Category: Capability

See Also:

[Actors](actors.md)
[Mailbox](mailbox.md)
[Server](server.md)
[Query Actor](query-actor.md)
[Workspace graph](workspace-graph.md)

Find and Move share one Actor. Query does not use this Actor.

## Sources

[ViewModelSearch](../../src/Shared/ViewModelSearch.fs)
[Search dialog](../../src/Client/SearchDialog.fs)
[GraphBuild](../../src/Shared/GraphBuild.fs)

## State

[ ] One walk: the Actor holds one walk, off the mailbox, and then stops. It keeps no continuation cursor.
[ ] Trash: the walk skips trash unless the start Node is TRASH. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[ ] Dedup: the Actor drops a Node id the client already showed.
[ ] Cap: the result stops at 200 hits. Find and Move share that cap.

## Interface

[ ] Keypress: every keypress recomputes on the client only and updates the Find dialog immediately. Move uses this same path when Move recomputes on each keypress. A keypress sends no server message and does not start this Actor.
[ ] Start: Find and Move start this same Actor after the quiet gap. Move does not start a second Actor. The start message carries the Node ids the client already showed.
[ ] Stop: the Actor returns one result and stops. Client hits plus this result stop at the cap. When the server finds N items, the Find dialog shows them. Move shows that same N.
[ ] Reply match: the result carries the search text it was computed for, or an equivalent generation of that text. The dialog applies the result only when that text is current.

## Uses

[ ] Server Graph: the Actor reads the server Graph. Detail: [Server](server.md).
[ ] ActorStart: the running Actor is recorded on the event source as `ActorStart`. Detail: [Mailbox](mailbox.md).

## Seams

[ ] Quiet gap: one shared Find and Move start after the search text is unchanged for a short quiet gap. The gap has no millisecond value. A text change before the gap ends sends no start. Find and Move send no start when the client already has the cap.
[ ] Cap: Find and Move share the State cap. Query uses that same number on [Query Actor](query-actor.md).

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The quiet gap keeps the server request off the keypress.
