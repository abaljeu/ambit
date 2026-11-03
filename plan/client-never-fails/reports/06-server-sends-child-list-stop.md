# Stop — [06 — Server sends each Node with its child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md)

The build stops before source edits. Ticket status is `build`.

Story [Remove Loaded and Unloaded](plan/client-never-fails/spec.md) step 13 and architecture path 6.1 in [Client never fails on server data architecture](plan/client-never-fails/arch.md) are the source of the two stops below.

## File outside Inputs

[ ] `UniversalResponse` in `src/Shared/ApiResponses.fs` is `{ nodes; events; latestId }`.

[ ] Process step 3 adds the child list to that record.

[ ] `gitResponse` in `tests/Server.Tests/LoadSaveCommandClientTests.fs` builds `Some { nodes = []; events = []; latestId = EventId.zero }`.

[ ] An F# record construction lists every field. `gitResponse` must list the new child-list field.

[ ] `tests/Server.Tests/LoadSaveCommandClientTests.fs` is outside Inputs and outside Outputs.

[ ] The other `UniversalResponse` constructions are `Api.universalResponseFromHandle` in `src/Server/Api.fs`, `decodeUniversalResponseDecoder` in `src/Shared/ApiResponseSerialization.fs`, and ``UniversalResponse round-trips nodes events and latestId`` in `tests/Shared.Tests/SerializationTests.fs`. Those three paths are Inputs or Outputs.

## Unloaded tests

[ ] Path 6.1 sends each returned Node with its Header and its child list of ids. A child id with no Header stays in the list.

[ ] `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs` returns that package. `ResidentProjection.visibleClosureGraph` stores the returned child lists as the Graph child map.

[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors`` in `tests/Shared.Tests/WantTests.fs` keeps `buriedId` in the node set and asserts `Graph.childrenStatus scoped buriedId` is `Unloaded`.

[ ] `buriedId` is a returned Node. Its child list in the package makes that status `Loaded`.

[ ] ``visibleClosureGraph bad Zoom stays inside reserved set`` in `tests/Shared.Tests/WantTests.fs` keeps the workspace id in the node set and asserts `GraphChildren.isLoaded scoped wsId` is false.

[ ] The workspace id is a returned Node. Its child list in the package makes that parent Loaded.

[ ] Both tests build an Unloaded Node. The stop condition leaves them as they are.

## Ask

[ ] Add `tests/Server.Tests/LoadSaveCommandClientTests.fs` to Inputs so `gitResponse` can carry the child list?

[ ] Approve a child-list shape that leaves ``visibleClosureGraph loads reserved Children and Zoom ancestors`` and ``visibleClosureGraph bad Zoom stays inside reserved set`` unchanged?
