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

[ ] The Find dialog opens on a local search and shows N hits.
[ ] When the local list is under 200, the globe on the search bar requests the server.
[ ] When the search text is unchanged, the server reply replaces the client list.
[ ] Move uses this same search.
[ ] A query expression inserts its results as Refs under the query line.

## Keypress

[ ] While the globe is not selected, every keypress recomputes on the client only and updates the Find dialog immediately. Files: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs).
[ ] While the globe is not selected, a keypress sends no server message.
[ ] An edit runs a server search when the globe is selected.
[ ] Move uses this same keypress path when the globe is not selected.

## Globe

[o] One server request fires after the search text is unchanged for a short quiet gap. Find and Move share that request.
[ ] When the local list is under 200, the globe on the search bar requests the server. Find and Move share that request.
[o] A text change before that gap ends sends no request.
[ ] An edit follows the globe.
[o] Find and Move send no server request when the client already has 200 hits.
[ ] The globe sends no request when the local list already has 200 hits.
[x] A reply for an older search string is ignored. The reply matches the current search text, or an equivalent generation of that text.
[ ] When the search text is unchanged, the server reply replaces the client list.

## Actors

[x] Find and Move share one Actor.
[o] That Actor starts after the quiet gap.
[ ] The globe starts that Actor when the local list is under 200.
[ ] A query expression runs on its own Actor. That Actor starts when the line runs.
[ ] The shared Actor returns one result and stops. The query Actor returns one result and stops.

## Node id

[ ] The server list lists each Node id once.
[ ] The start does not carry shown Node ids.
[ ] The reply replaces the client list. It does not add the server hits onto the client list.

## Bound

[x] Find and Move share a cap of 200.
[o] Client hits and server hits together stop at 200.
[ ] The local list stops at 200. The server list stops at 200.
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

The cap of 200 keeps one search from returning the whole Graph. The globe requests the server when the local list is under 200. Find and Move share that request.
