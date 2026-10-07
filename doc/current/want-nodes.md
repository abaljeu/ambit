# Want nodes for hits

Category: Capability

See Also:

[Search](search.md)
[Search Actor](search-actor.md)
[HTTP contract](http-contract.md)

A Find or Move server reply includes the found Nodes on the Want answer `nodes` list.

## Sources

[ResidentProjection](../../src/Shared/ResidentProjection.fs) — `installWantAnswer`

## Job

[ ] A server reply with Find results includes the found Nodes as Want-fulfillment.
[ ] Move uses that same reply.
[ ] Hit Headers ride the existing Want answer `nodes` list.
[ ] There is no new package.

## Data

[ ] A hit Node is Resident after install.
[ ] A hit with no new `childMap` key stays Unloaded for its Children.

## Interface

[ ] The result's Nodes are added to the Want answer `nodes` list.
[x] [installWantAnswer](../../src/Shared/ResidentProjection.fs) merges `nodes` and `childMap`.
[ ] Ordinary Want edges stay `childMap`.
[ ] The result does not add a `childMap` key only to carry a hit Header.

## Uses

[ ] Post-Event and Poll carry the Want answer.
[ ] The one reply uses that carrier.

## Explanation

The existing `nodes` list already installs Headers. A second answer type is a wider interface.
