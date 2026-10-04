# Search

Category: Capability

See Also:

[Actors](actors.md)
[Browser](browser.md)
[Operations](operations.md)
[Workspace graph](workspace-graph.md)

Find completes client hits from the server, and a query expression inserts Refs under the query line.

Sources: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs), [GraphBuild](../../src/Shared/GraphBuild.fs).

## Job

[ ] The Find dialog shows client hits and the hits the server returns.
[ ] Move shows that same list when Move recomputes on each keypress.
[ ] A query expression inserts its results as Refs under the query line.

## Keypress

[ ] Every keypress recomputes on the client only and updates the Find dialog immediately. Files: [ViewModelSearch](../../src/Shared/ViewModelSearch.fs), [Search dialog](../../src/Client/SearchDialog.fs).
[ ] A keypress sends no server message.
[ ] Move uses this same keypress path when Move recomputes on each keypress.

## Quiet gap

[ ] One server request fires after the search text is unchanged for a short quiet gap.
[ ] A text change before that gap ends sends no request.
[ ] Find sends no server request when the client already has 200 hits.
[ ] Move sends no server request on that same rule when the client already has 200 hits.
[ ] The request asks only for hits the client does not already have. The start message carries those Node ids.
[ ] A reply for an older search string is ignored. The reply matches the current search text, or an equivalent generation of that text.

## Actors

[ ] Find, Move, and a query expression each run on their own Actor.
[ ] Find starts its Actor after the quiet gap. Move starts its Actor on that same rule when Move recomputes per keypress.
[ ] A query expression starts its Actor when the line runs.
[ ] Each Actor returns one result and stops.

## Dedup

[ ] A client hit and a server hit are the same when they share a Node id. The server result omits that id.

## Bound

[ ] Find stops at 200 hits. Move stops at 200 hits. Client hits and server hits together stop at 200.
[ ] A query expression stops at 200 results. The query function may set a lower limit. A request above 200 stops at 200.
[ ] There is no next page.

## Trash

[ ] Find skips TRASH unless the start Node is TRASH. Move follows that same rule. TRASH id: [GraphBuild](../../src/Shared/GraphBuild.fs) `trashId`.
[ ] An ordinary query expression skips TRASH.
[ ] The function named `trash` reaches TRASH the way `root` reaches ROOT.

## Query

[ ] The server evaluates a query expression once, when the line runs.
[ ] A keypress does not start that evaluation.

## Explanation

The cap of 200 keeps one search from returning the whole Graph. The quiet gap keeps the server request off the keypress.
