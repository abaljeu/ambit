# 16 — Poll for nearby Absent Nodes scope

Project [Client never fails on server data](plan/client-never-fails/project.md), issue [16 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/16-poll-nearby-absent/16-poll-nearby-absent-ticket.md). Architecture path 7 **Poll for nearby Absent Nodes** in [Client never fails on server data architecture](plan/client-never-fails/arch.md) lists Want items 1–3 for this slice.

### Signatures

[ ] `Want.compose` in `src/Shared/Want.fs` has signature `Graph -> SiteMap -> NodeId -> NodeId list`.
[ ] `IncludedDescendantIds.throughChildrenOfExpandedNodes` in `src/Shared/IncludedDescendantIds.fs` has signature `Graph -> SiteMap -> NodeId -> NodeId list`.
[ ] `IncludedDescendantIds.plusChildrenOfEach` in `src/Shared/IncludedDescendantIds.fs` has signature `Graph -> NodeId list -> NodeId list`.
[ ] `GraphChildren.isLoaded` in `src/Shared/Model.fs` has signature `Graph -> NodeId -> bool` and returns `Map.containsKey id graph.childMap`.
[ ] `GraphChildren.status` in `src/Shared/Model.fs` has signature `Graph -> NodeId -> ChildrenStatus`.
[ ] `type ChildrenStatus` in `src/Shared/Model.fs` has cases `Unloaded` and `Loaded`.
[ ] `GraphChildMapHelpers.unload` in `tests/Shared.Tests/GraphChildMapHelpers.fs` has signature `NodeId -> Graph -> Graph`.
[ ] `GraphChildMapHelpers.absentUnloaded` in `tests/Shared.Tests/GraphChildMapHelpers.fs` has signature `NodeId -> Graph -> Graph`.
[ ] `ViewModelSiteMap.expandEntry` in `src/Shared/ViewModelSiteMap.fs` has signature `SiteId -> Graph -> SiteMap -> SiteId -> SiteMap * SiteId`.
[ ] `CommandRequest.nodeHeaderIsAbsent` in `src/Shared/CommandRequest.fs` has signature `Graph -> NodeId -> bool`.
[ ] `UpdateHelpers.currentWant` in `src/Client/UpdateHelpers.fs` has signature `VM -> NodeId list` and calls `Want.compose`.

### Match sites

[ ] `Want.compose` in `src/Shared/Want.fs` calls `IncludedDescendantIds.throughChildrenOfExpandedNodes graph siteMap zoomRoot`.
[ ] `Want.compose` in `src/Shared/Want.fs` pipes through `IncludedDescendantIds.plusChildrenOfEach graph` twice.
[ ] `Want.compose` in `src/Shared/Want.fs` filters with `List.filter (fun id -> not (GraphChildren.isLoaded graph id))`.
[ ] `IncludedDescendantIds.plusChildrenOfEach` in `src/Shared/IncludedDescendantIds.fs` matches `GraphChildren.tryGet graph parentId` with `None` skipping children and `Some children` folding child ids.
[ ] `GraphChildren.isLoaded` in `src/Shared/Model.fs` uses `Map.containsKey id graph.childMap` with no `nodes` lookup.
[ ] `ViewModelSiteMap.expandEntry` in `src/Shared/ViewModelSiteMap.fs` reads child ids via `GraphChildren.get graph entry.nodeId` when expanding a collapsed entry.
[ ] `tests/Shared.Tests/WantTests.fs` opens `GraphChildMapHelpers` and calls `unload` in the four ticket-named compose facts.
[ ] ``compose wants a placeholder id with no child list after parent expand`` in `tests/Shared.Tests/WantTests.fs` calls `absentUnloaded`, then `expandEntry`, then `Want.compose`.
[ ] ``installWantAnswer keeps absent key Unloaded and [] Loaded`` in `tests/Shared.Tests/WantTests.fs` matches `Graph.childrenStatus` to `Unloaded` and `Loaded`.
[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors`` in `tests/Shared.Tests/WantTests.fs` matches `Graph.childrenStatus` to `Loaded` and `Unloaded`.
[ ] `UpdateHelpers.currentWant` in `src/Client/UpdateHelpers.fs` is a caller of `Want.compose`.
[ ] `UpdateHelpers.currentPollRequest` and `UpdateHelpers.currentChangeRequest` in `src/Client/UpdateHelpers.fs` embed `currentWant model`.
[ ] `Update.fs` in `src/Client` calls `List.isEmpty (currentWant model)` in PollDone handling.
[ ] ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` calls `Want.compose` twice on the fixture graph and post-`Update` model.
[ ] `StateEndpointTests.fs` function `absentChildWant` calls `Want.compose graph (siteMapAtRoot graph) graph.root`.
[ ] `StateEndpointTests.fs` function `pullAbsentChildren` loops on `absentChildWant graph` until empty or cap.
[ ] ``compose after Loaded split does not want the parent`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` calls `Want.compose split.graph siteMap zoomId`.

### Existing tests

[ ] ``compose lists Included that miss Children first`` in `tests/Shared.Tests/WantTests.fs` builds a zoom with children `"a"` and `"b"`, runs `unload` on the first child id, calls `Want.compose`, and expects want `[ childIds.[0] ]` with the second child and zoom root absent.
[ ] ``compose wants a placeholder id with no child list after parent expand`` in `tests/Shared.Tests/WantTests.fs` runs `absentUnloaded` on the first child, `expandEntry` on that instance, calls `Want.compose`, asserts the placeholder id is wanted, and asserts `not (graph.nodes.ContainsKey placeholderId)`.
[ ] ``compose then installWantAnswer loads wanted parents and children`` in `tests/Shared.Tests/WantTests.fs` runs `unload` on the zoom child `"a"`, expects first want `[ parentId ]`, installs a grandchild via `installWantAnswer`, expands the parent, and expects second want `[ grandId ]`.
[ ] ``compose wants unloaded children of folded members`` in `tests/Shared.Tests/WantTests.fs` builds a folded mid node with hidden child `"hidden"`, calls `Want.compose (unload hiddenId graph) siteMap zoomId`, and expects want `[ hiddenId ]`.
[ ] ``compose wants one rank past a folded loaded child`` in `tests/Shared.Tests/WantTests.fs` builds a deeper chain, calls `Want.compose (unload grandId graph) siteMap zoomId`, and expects want `[ grandId ]` without `hiddenId`.
[ ] ``compose is empty when Included is Loaded`` in `tests/Shared.Tests/WantTests.fs` uses `zoomWithChildren [ "leaf" ]` with a fully loaded graph and expects `Want.compose` to return `[]`.

### Copy-from

[ ] `CommandRequest.nodeHeaderIsAbsent` in `src/Shared/CommandRequest.fs` already returns true when `Map.tryFind id graph.nodes` is `None` and false when a Header is present.

### implement

[ ] Ticket Input claim: the filter keeps ids whose Header is absent via `Map.tryFind id graph.nodes`. Code still filters with `not (GraphChildren.isLoaded graph id)` in `Want.compose`.
[ ] Ticket Process claim: stop using `GraphChildren.isLoaded` in `Want.compose`. Code still calls `GraphChildren.isLoaded` in the filter line.
[ ] Ticket Process claim: change the four compose facts so an Absent id is wanted and an Unloaded id that still has a Header is not. The four facts still use `unload`, which keeps the Header and removes only the `childMap` key, and they still expect those ids in want.
[ ] Ticket Input claim: the walk is `throughChildrenOfExpandedNodes` then `plusChildrenOfEach` twice. Code matches; no edit required for the walk.
[ ] Ticket Input claim: `Want.compose` type `Graph -> SiteMap -> NodeId -> NodeId list`. Code matches.
[ ] Ticket Input claim: `ChildrenStatus`, `Loaded`, and `Unloaded` stay. Code still defines `type ChildrenStatus` with both cases in `src/Shared/Model.fs`.
[ ] Ticket Input claim: Expand puts child ids on the walk through `ViewModelSiteMap.expandEntry`. Code and ``compose wants a placeholder id with no child list after parent expand`` already use `expandEntry`; that fact expects an Absent placeholder id in want today because `isLoaded` is false and the Header is absent.
[ ] Ticket Do not load claim: ``compose is empty when Included is Loaded`` stays unchanged. Code test expects `[]` when every walked id has a Header and a loaded child list; an absent-Header filter keeps those ids off want, so the fact should still pass without edit.
[ ] ``compose lists Included that miss Children first`` observes `Want.compose graph siteMap zoomId` at the public seam. After the filter change, the same call with `unload` should return `[]` or omit the Unloaded id; with `absentUnloaded` on that child it should return `[ childIds.[0] ]`.
[ ] ``compose then installWantAnswer loads wanted parents and children`` observes first and second `Want.compose` results. After the filter change, the `unload parentId` setup should not put `parentId` in want; an Absent parent on the walk (for example `absentUnloaded parentId` while the id stays on the SiteMap from expand) should appear in first want before `installWantAnswer`.
[ ] ``compose wants unloaded children of folded members`` observes `Want.compose (unload hiddenId graph) siteMap zoomId`. After the filter change, `unload hiddenId` should yield `[]`; `absentUnloaded hiddenId` should yield `[ hiddenId ]` when that id is on the two-rank walk.
[ ] ``compose wants one rank past a folded loaded child`` observes `Want.compose (unload grandId graph) siteMap zoomId`. After the filter change, `unload grandId` should yield `[]`; `absentUnloaded grandId` should yield `[ grandId ]` when that id is on the walk past the folded loaded child.
[ ] ``compose is empty when Included is Loaded`` observes `Want.compose` on a graph where Included members have Headers and loaded child lists. Absent ids are not on that graph, so want stays `[]` under either filter; Unloaded-with-Header ids are also excluded once the filter checks `nodes` only.
[ ] Caller `absentChildWant` in `tests/Server.Tests/StateEndpointTests.fs` depends on `Want.compose` returning ids with no Header so `pullAbsentChildren` can Poll until empty; it does not require Unloaded-with-Header ids in want.
[ ] ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` expects first want `[ fixture.parentId ]` where the browser graph lists `parentId` on root’s child list without a Header, and second want `[ fixture.childId ]` after install leaves `childId` off `nodes`; both expectations align with an absent-Header filter, not with wanting Unloaded Headers.
[ ] ``compose after Loaded split does not want the parent`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` only asserts `zoomId` is not in want; a parent with a loaded child list and a Header should stay out of want under an absent-Header filter.
[ ] `UpdateHelpers.currentWant` in `src/Client/UpdateHelpers.fs` forwards to `Want.compose`; Poll and post-Event payloads change when compose stops listing Unloaded Headers and lists Absent ids only. No test file edit is named in ticket Inputs for `src/Client` or `tests/Server.Tests`.
