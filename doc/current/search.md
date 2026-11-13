# Search

Category: Capability

See Also:

[Actors](actors.md)
[Browser](browser.md)
[Operations](operations.md)
[Want nodes for hits](want-nodes.md)
[Workspace graph](graph.md)

Find and Move share one server search. The globe requests the server. A query expression inserts Refs under the query line.

Sources: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs), [GraphBuild](../../src/Shared/GraphBuild.fs).

## Job

[ ] The Find dialog opens on the residency search. That search starts at the zoom root.
[ ] When the residency search has fewer than 200 client hits, the globe on the search bar requests the server.
[ ] The server search reads the full database and starts at the zoom root.
[ ] The server results exclude Node ids the client search found, and the dialog appends those server hits.
[ ] The client adds the carried hit nodes to the residency Graph and then lists them, including a hit that was absent from the residency before the reply.
[ ] Move uses this same search.
[ ] A query expression inserts its results as Refs under the query line.

## Keypress

[ ] While the globe is not selected, every keypress recomputes on the client only and updates the Find dialog immediately. Files: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs).
[ ] While the globe is not selected, a keypress sends no server message.
[ ] An edit runs a server search when the globe is selected.
[ ] Move uses this same keypress path when the globe is not selected.

## Globe

[o] One server request fires after the search text is unchanged for a short quiet gap. Find and Move share that request.
[ ] When the residency search has fewer than 200 client hits, the globe on the search bar requests the server. Find and Move share that request.
[o] A text change before that gap ends sends no request.
[ ] An edit follows the globe.
[o] Find and Move send no server request when the client already has 200 hits.
[ ] The globe sends no request when the residency search already has 200 client hits.
[x] A reply for an older search string is ignored. The reply matches the current search text, or an equivalent generation of that text.
[ ] When the search text is unchanged, the server results exclude Node ids the client search found, and the dialog appends those server hits. The client adds the carried nodes to the residency Graph and then lists them.

## Actors

[x] Find and Move share one Actor.
[o] That Actor starts after the quiet gap.
[ ] The globe starts that Actor when the residency search has fewer than 200 client hits.
[ ] A query expression runs on its own Actor. That Actor starts when the line runs.
[ ] The shared Actor returns one result and stops. The query Actor returns one result and stops.

## Node id

[ ] The server list lists each Node id once.
[ ] The request carries the search text and the zoom root. Displayed rows play no part, except that both searches start at the zoom root.
[ ] The dialog excludes Node ids the client search found when it appends the server hits.

## Bound

[x] Find and Move share a cap of 200.
[o] Client hits and server hits together stop at 200.
[ ] The residency search stops at 200 client hits. The full-database search stops at 200 server hits.
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

The cap of 200 keeps one search from returning the whole Graph. The globe requests the server when the residency search has fewer than 200 client hits. Find and Move share that request. Both searches start at the zoom root.
