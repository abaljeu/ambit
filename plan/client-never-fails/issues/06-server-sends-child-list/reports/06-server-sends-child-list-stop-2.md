# Stop 2 — [06 — Server sends each Node with its child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md)

The build stops before source edits. Ticket status is `build`.

Alan's decision of 2026-11-01 is on the ticket. The first stop record stays [06 — Server sends each Node with its child list stop](plan/client-never-fails/reports/06-server-sends-child-list-stop.md).

## Child list on each returned Node

[ ] `ResidentProjection.visibleClosureWantAnswer` returns parents from `loadedParentIds` plus the Headers of those parents' children.

[ ] A returned Node whose id is only in that child rank has its Header in `nodes` and no key in the returned child map.

[ ] `ResidentProjection.visibleClosureGraph` stores that map through `Graph.fromNodes`. A missing key is `Unloaded`. A present key is `Loaded`.

[ ] Alan's two tests expect the child list, so each returned Node's list goes into that map.

[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors`` then sees `buriedId` as `Loaded`.

[ ] ``visibleClosureGraph bad Zoom stays inside reserved set`` then sees the workspace id as Loaded.

## File outside Inputs

[ ] ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` builds the browser with `visibleClosureGraph (Some root)`.

[ ] The parent is a returned Node. Its child list makes that parent `Loaded`.

[ ] `Want.compose` keeps ids whose child-list key is absent. The parent leaves that list. The child id remains.

[ ] The test expects the first Want to be `[parentId]`.

[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs` is outside Inputs and outside Outputs.

[ ] That test builds an Unloaded Node. The One job exception names only the two tests in `tests/Shared.Tests/WantTests.fs`.

## Probe

[ ] A probe put each returned Node's child list into the map from `visibleClosureWantAnswer`, then ran the three facts below. The probe is removed. Branch source matches the rebased head.

[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors`` failed: `Graph.childrenStatus` for `buriedId` was `Loaded`. The test expects `Unloaded`.

[ ] ``visibleClosureGraph bad Zoom stays inside reserved set`` failed: `GraphChildren.isLoaded` for the workspace id was true. The test expects false.

[ ] ``Poll answer chains the next Included residency Want`` failed at the first Want. Expected `[parentId]`. Actual was one other id, the child.

[ ] ``PollDone leaves Sync Idle when the edit box holds new text`` passed on that same probe.

## Ask

[ ] Add `tests/Server.Tests/BrowserResidencyClientTests.fs` to Inputs so ``Poll answer chains the next Included residency Want`` can expect the child list?

[ ] Let that test expect the child id as the first Want, once the parent carries its child list?
