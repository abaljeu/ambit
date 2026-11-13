# Search

Category: Capability

See Also:

[Actors](actors.md)
[Browser](browser.md)
[Operations](operations.md)
[Want nodes for hits](want-nodes.md)
[Workspace graph](graph.md)

Find and Move share one server search. A query expression inserts Refs under the query line.

Sources: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs), [GraphBuild](../../src/Shared/GraphBuild.fs).

## Job

[ ] The Find dialog opens on the residency search. That search starts at the zoom root.
[x] The server search reads the full database and starts at the zoom root.
[x] The server results exclude Node ids the client search found, and the dialog appends those server hits.
[x] The client adds the carried hit nodes to the residency Graph and then lists them, including a hit that was absent from the residency before the reply.
[ ] Move uses this same search.
[ ] A query expression inserts its results as Refs under the query line.
[x] A reply for an older search string is ignored. The reply matches the current search text, or an equivalent generation of that text.

## Actors

[x] Find and Move share one Actor.
[o] That Actor starts after the quiet gap.
[o] One server request fires after the search text is unchanged for a short quiet gap. Find and Move share that request.
[o] A text change before that gap ends sends no request.
[o] Find and Move send no server request when the client already has 200 hits.
[ ] A query expression runs on its own Actor. That Actor starts when the line runs.
[ ] The shared Actor returns one result and stops. The query Actor returns one result and stops.

## Node id

[x] The server list lists each Node id once.
[x] The request carries the search text and the zoom root. Displayed rows play no part, except that both searches start at the zoom root.
[x] The dialog excludes Node ids the client search found when it appends the server hits.

## Bound

[x] Find and Move share a cap of 200.
[x] The cap is `SearchPicture.Request.count` (200 when omitted). That cap applies to the server walk, the client reply room, and the quiet-gap gate.
[ ] A query expression stops at 200 results. The query function may set a lower limit. A request above 200 stops at 200.
[ ] There is no next page.

## Trash

[ ] Find and Move skip TRASH unless the start Node is TRASH. They share that rule. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[ ] An ordinary query expression skips TRASH.
[ ] The function named `trash` reaches TRASH the way `root` reaches ROOT.

## Query

[ ] The server evaluates a query expression once, when the line runs.
[ ] A keypress does not start that evaluation.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. Both searches start at the zoom root.
