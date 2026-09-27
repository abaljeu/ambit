# Browser residency architecture

Spec: [[spec.md]]
Updated: 2026-09-26
Sequence: expand-contract

Sources: [map.md](map.md) Destination, Notes, and Decisions so far (2026-09-26 locks); [spec.md](spec.md) Solution and 36 stories. Prefer existing seams. `Graph.childMap` (absent key = Unloaded) is the residency list. Checklist: `[x]` already true of the recorded shape; `[ ]` still to build. Poll, post-Event, bootstrap, and explicit Load Fetch converge on one current-version edges-plus-Nodes answer. The Browser computes Want fields from its ViewModel. Saved Zoom scopes only the initial base Graph. There is no cross-version interoperation, compatibility form, or legacy `packages` API in the destination.

## 1. Story paths

1. **Open a large Server Graph**
   1. [ ] Person opens the Browser
   2. [x] [src/Client/Program.fs](src/Client/Program.fs) boots via `/state` or boot Poll
   3. [ ] [src/Server/Api.fs](src/Server/Api.fs) `getState` returns a visible-closure Graph, not the whole Server Graph
   4. [ ] Browser first paint uses that scoped Graph only

2. **Zoom first paint**
   1. [x] `/state` request carries best-effort saved Zoom
   2. [ ] Visible-closure bootstrap includes the Zoom root and its direct Children
   3. [ ] Browser restores Zoom and Fold locally after State loads
   4. [x] [src/Shared/IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs) walks the restored SiteMap honoring Fold

3. **Framing path**
   1. [ ] Bootstrap includes the ancestor path to saved Zoom
   2. [ ] Those ancestors are Loaded so the path edges and headers are Resident
   3. [x] Browser restores the UI Zoom independently

4. **ROOT Children**
   1. [x] [src/Shared/GraphBuild.fs](src/Shared/GraphBuild.fs) `rootId` is ROOT
   2. [ ] Bootstrap installs `childMap` for ROOT (direct Children Resident)
   3. [ ] A ROOT Child outside Included is still Resident as a header

5. **TRASH Children**
   1. [x] GraphBuild `trashId` is TRASH
   2. [ ] Bootstrap installs `childMap` for TRASH
   3. [ ] TRASH first-rank Nodes are Resident

6. **Workspaces Node Children**
   1. [x] GraphBuild `workspacesId` is the Workspaces Node
   2. [ ] Bootstrap installs `childMap` for the Workspaces Node
   3. [ ] Named Workspace headers are Resident

7. **SYSTEM Children**
   1. [x] GraphBuild `systemId` is SYSTEM
   2. [ ] Bootstrap installs `childMap` for SYSTEM
   3. [ ] SYSTEM first-rank Nodes are Resident

8. **Not a complete Workspace**
   1. [ ] Production `/state` uses `ResidentProjection.visibleClosureGraph`
   2. [ ] It does not call today's complete-ROOT `rootBootstrapGraph` / extra Workspace package as the production scope
   3. [x] `BootstrapScope.FullGraph` stays a test door (`?scope=full`)

9. **Same package after bootstrap**
   1. [ ] Bootstrap answer is edges plus pointed-at Nodes
   2. [ ] Later Want answers use that same package
   3. [ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) installs both through one install door

10. **Hollow Unloaded Bullet**
    1. [x] [src/Shared/ViewModelChildrenIndicator.fs](src/Shared/ViewModelChildrenIndicator.fs) hollow when Unloaded
    2. [x] Indicator reads absent `childMap` key as Unloaded
    3. [x] [src/Client/RowView.fs](src/Client/RowView.fs) paints `amb-bullet-hollow`

11. **Hollow Unparsed Bullet**
    1. [x] Indicator hollow when `documentState = Unparsed`
    2. [x] Unloaded and Unparsed stay distinct facts on one glyph
    3. [x] RowView uses the same hollow class

12. **No new loading Status**
    1. [x] No per-Node loading Status on Node
    2. [x] Residency stays absent/present `childMap` plus Unparsed
    3. [x] SyncInfo `Loading` stays the global Load-command flight, not a Node field

13. **Auto want Included**
    1. [x] Want module lists Unloaded Included Nodes first, then two ranks of Unloaded Children
    2. [ ] [src/Client/App.fs](src/Client/App.fs) attaches that Want on Poll and post-Event
    3. [ ] No click and no Load

14. **Children and grandchildren**
    1. [x] One compose includes Unloaded Children of the Included set and the Unloaded grandchildren, including under Fold
    2. [ ] After install, Browser recomputes the Want
    3. [ ] A later Poll / post-Event carries that recomputed Want

15. **No third ongoing tier**
    1. [x] Want.compose does not add reserved Nodes or the Zoom framing path as an ongoing tier
    2. [x] Those ids stay bootstrap-only
    3. [x] Shared test asserts Included first, then two Children ranks, and no reserved tier

16. **No click**
    1. [x] RowView Bullet click is fold / select, not Want
    2. [ ] Want attaches from Included, not from a click handler
    3. [x] Hollow glyph is a signal

17. **No command**
    1. [x] [src/Client/Commands.fs](src/Client/Commands.fs) Load stays a command
    2. [ ] Auto Want does not call `loadOp` or `tryStartLoad`
    3. [ ] Poll and post-Event are the only auto doors

18. **Hollow-click Load may remain**
    1. [x] `loadOp` in [src/Client/UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs) remains
    2. [ ] Do not remove a hollow-circle → Load wiring if present
    3. [ ] Auto path does not use that wiring

19. **Wants on post-Event**
    1. [x] App POSTs `/{file}/changes` (post-Event)
    2. [ ] Request carries the current Want with the Ev batch
    3. [ ] [src/Server/Api.fs](src/Server/Api.fs) `postEvents` answers Changes plus edges plus Nodes

20. **Wants on Poll**
    1. [x] `PollRequest` has required `eventId` and `want` fields
    2. [ ] App `runPollServer` POSTs `PollRequest` to `/{file}/poll`
    3. [ ] `Api.postPoll` answers Changes plus edges plus Nodes

21. **Edges in the answer**
    1. [ ] Server builds `childMap` entries for each wanted parent
    2. [x] ResidentProjection.installWantAnswer installs those edges
    3. [x] Wanted parents become Loaded when the key is present
      4. [x] SyncLogic.applySyncResponse installs Want-answer edges after Events ([08 — Migrate Shared wire](issues/08-migrate-shared-wire.md))

22. **Nodes in the answer**
    1. [ ] Server includes each Child Node the edges point at, as a separate collection
    2. [x] Install adds those Nodes to `graph.nodes`
    3. [x] No edge is installed without its target Node

23. **No dangling edges**
    1. [x] Install refuses an edge whose target is not in the Node collection and not already Resident
    2. [x] Shared test covers the refuse
    3. [ ] Server `wantAnswer` includes every target Node for every emitted edge
    4. [ ] Browser Graph never shows a header-less Child

24. **Absent key stays Unloaded**
    1. [x] Destination rule: absent `childMap` key = Unloaded
    2. [x] Install does not insert an empty key unless the Server sent `[]`
    3. [x] Indicator treats absent key as hollow Unloaded

25. **Present key is Loaded**
    1. [x] Destination rule: present key including `[]` = Loaded
    2. [x] Install of `[]` marks a true leaf Loaded
    3. [x] A Loaded empty list is not a hollow Unloaded Bullet unless Unparsed

26. **Children arrive on the Bullet**
    1. [ ] After install, `childMap` is present for that parent
    2. [x] Indicator leaves HollowCircle when children exist (chevron) or when Loaded and not Unparsed (solid)
    3. [x] RowView re-renders from the indicator

27. **Growth while I work**
    1. [x] [src/Shared/SyncPlanner.fs](src/Shared/SyncPlanner.fs) `tryStartPoll` after Idle
    2. [ ] Each later Poll / post-Event recomputes Want from current Included
    3. [ ] Install is additive; residency stays monotonic this session

28. **Find in residence**
    1. [x] [src/Shared/ViewModelSearch.fs](src/Shared/ViewModelSearch.fs) `searchNodes` walks the Browser Graph only
    2. [x] No Server Find door on this path
    3. [ ] Do not add a Want or Fetch from Find in this architecture

29. **Find commit stays Zoom**
    1. [x] `searchPickSetRoot` re-roots like Zoom
    2. [x] It does not call Load or Fetch
    3. [ ] A not-Resident hit is not a first-slice path (see Unsettled / [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md))

30. **Load command still there**
    1. [x] Commands.fs binds Load to `loadOp`
    2. [x] `loadOp` still plans Upload / Parse / Fetch
    3. [x] File-transit stages stay on [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) (out of this Project)

31. **Load uses the same package**
    1. [x] `SyncPlanner.tryStartLoad` emits `LoadServer`
    2. [ ] App `runLoadServer` POSTs `/load` and receives edges plus Nodes
    3. [ ] Load Fetch, auto wants, and bootstrap use the same install door
    4. [ ] `LoadResponse.packages`, `packageChildMap`, and `installPackages` are removed

32. **Commands that name Nodes later**
    1. [x] No new command that names Nodes
    2. [x] Auto Want is not a command
    3. [x] Load remains the existing user-facing command

33. **SiteMap honors Fold**
    1. [x] IncludedDescendantIds stops at folded children
    2. [x] Want.compose uses that Included list, then two Children ranks, not a deep unfold
    3. [x] Folded Nodes are not a deep visible tree. Their Children and grandchildren may still be wanted

34. **Unloaded is not empty**
    1. [x] Absent `childMap` key never renders as a Loaded leaf
    2. [x] Hollow Bullet means Children are not here or Unparsed
    3. [x] Person can tell Unloaded from a Loaded empty list

35. **Server stays large**
    1. [x] Server `getState` handle still holds the full Core Graph
    2. [ ] Visible-closure is a projection for the Browser answer only
    3. [ ] Later Wants read the same large Server Graph

36. **Reserved spelling**
    1. [x] GraphBuild `systemId` and SYSTEM name stay SYSTEM
    2. [ ] Bootstrap reserved set cites ROOT, TRASH, Workspaces Node, SYSTEM
    3. [x] No Trash / System spelling in this Project's doors

Shared segments:
1. [x] Want.compose (Unloaded Included Nodes, then two ranks of Unloaded Children; recompute after install)
2. [ ] Poll and post-Event carry Want with Changes
3. [ ] Server answers edges plus pointed-at Nodes (no dangling edges)
4. [x] ResidentProjection installs that package into `Graph.childMap`
5. [x] Bullet from Unloaded / Unparsed via ViewModelChildrenIndicator
6. [ ] Small bootstrap base (reserved Children + Zoom ancestor path + Zoom Children)

Narrowest shared test seam:
1. [x] Want.compose + installWantAnswer in Shared (no HTTP): given Graph, SiteMap, Zoom, and an answer package, the next Graph has Loaded wanted parents, Resident Children, and no dangling edges

## 2. Module map

1. **Graph childMap**
   File: [src/Shared/Model.fs](src/Shared/Model.fs) (`Graph`); helpers in [src/Shared/GraphBuild.fs](src/Shared/GraphBuild.fs) / Graph lookup.
   1. State
      1. [x] `childMap: Map<NodeId, ChildNode list>` — absent key = Unloaded; present key including `[]` = Loaded
      2. [x] `nodes` holds Resident headers; Children lists do not live on Node
   2. Interface
      1. [x] Lookup Children only through `childMap` (or a derived `ChildrenStatus`)
      2. [ ] Install never writes an edge whose target Node is absent
   3. Uses
      1. [x] NodeId, ChildNode, Ownership

2. **Want**
   File: new [src/Shared/Want.fs](src/Shared/Want.fs) (name may match the locked term Want).
   1. State
      1. [x] None durable — a Want is a list of parent Node ids whose Children are desired
   2. Interface
      1. [x] `compose: Graph * SiteMap * zoomRoot -> NodeId list` — Unloaded Included Nodes, then their Unloaded Children, then the Unloaded grandchildren; recompute after each install; no bootstrap tier
      2. [x] Empty list is allowed; request field is `want` (`[]` when empty)
   3. Uses
      1. [x] IncludedDescendantIds
      2. [x] Graph childMap (miss = absent key)

3. **ResidentProjection**
   File: [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs).
   1. State
      1. [ ] None beyond the Graph it returns
   2. Interface
      1. [x] `visibleClosureGraph: NodeId option * Graph -> Graph` computes `childMap` for ROOT, TRASH, Workspaces Node, and SYSTEM; the saved Zoom ancestor path; and the Zoom root. It includes each Loaded parent's direct Children and is not a complete Workspace. [09 — Migrate Server Sync doors](issues/09-migrate-server-sync-doors.md) owns the production `/state` switch
      2. [x] `installWantAnswer: edges * nodes * Graph -> Result<Graph, string>` — edges and Nodes separately; refuse dangling edges
      3. [ ] Remove `packagesForTargets` / `installPackages`; explicit Load Fetch uses `wantAnswer` / `installWantAnswer`
      4. [ ] `wantAnswer: Graph * NodeId list -> childMap * Node list` — include every pointed-at Resident Child Node; do not emit dangling edges
   3. Uses
      1. [x] GraphBuild reserved ids
      2. [ ] Graph childMap
      3. [x] GraphQuery enclosing / ancestors

4. **Included**
   File: [src/Shared/IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs).
   1. State
      1. [x] None
   2. Interface
      1. [x] `throughChildrenOfExpandedNodes: Graph * SiteMap * startId -> NodeId list` honoring Fold
      2. [x] `plusChildrenOfEach: Graph * NodeId list -> NodeId list` — found ids, then each node's direct Children that are not already listed. `Want.compose` applies it twice
      3. [x] Walk `childMap`, not Node.children, once childMap is the list
   3. Uses
      1. [x] SiteMap, Graph

5. **Sync wire**
   File: [src/Shared/ApiResponses.fs](src/Shared/ApiResponses.fs).
   1. State
      1. [x] `ChangeSuccessResponse` Events, `apiVersion`, Poll stamps
      2. [x] Want-answer fields `nodes` + `childMap`. Request field `want`
   2. Interface
      1. [x] Encode / decode required current-version fields on `PollRequest`, `ChangeRequest`, and `ChangeSuccessResponse`
      2. [x] `ApiVersion.current` is 13 (wire 1.3) with the Want + edges/Nodes package
      3. [x] Do not decode missing current-version fields and do not keep an old Poll or post-Event form
   3. Uses
      1. [x] Ev, EventId, Graph types

6. **Server Sync doors**
   File: [src/Server/Api.fs](src/Server/Api.fs); routes in [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs).
   1. State
      1. [x] None — doors read Core Graph / EventLog
   2. Interface
      1. [ ] `postPoll` and `postEvents` accept required Want and return Changes plus edges plus Nodes
      2. [ ] `getState` uses saved Zoom and returns the small base visible-closure via ResidentProjection
      3. [ ] `postLoad` returns the same edges-plus-Nodes answer; no legacy `packages`
   3. Uses
      1. [x] CoreChanges `getState` / `getEventsSince` / `postEvents`
      2. [ ] ResidentProjection visible-closure + `wantAnswer`
      3. [x] ApiResponses encode

7. **Browser HTTP**
   File: [src/Client/App.fs](src/Client/App.fs).
   1. State
      1. [x] In-memory VM Graph and `eventId`
   2. Interface
      1. [ ] `runPollServer` POSTs current-version `PollRequest` with the current Want
      2. [ ] POST `/{file}/changes` sends current Want with the Ev batch
      3. [x] `runLoadServer` still POSTs `/load` with `LoadRequest.targets`
   3. Uses
      1. [ ] Want.compose
      2. [x] SyncLogic decode / outcome
      3. [x] SyncPlanner Poll / Load effects

8. **Boot Poll**
   File: [src/Client/Program.fs](src/Client/Program.fs).
   1. State
      1. [x] Boot cache / first Poll
   2. Interface
      1. [x] `runBootPoll` / `handleBootPoll` stay the first-paint Poll
      2. [x] Bootstrap reads best-effort saved Zoom and sends it on `/state`
      3. [ ] First `/state` Graph is the small bootstrap visible-closure
      4. [ ] Browser restores Zoom and Fold locally, then boot Poll may compute Want from restored Included
   3. Uses
      1. [x] ChangeSuccessResponse decode
      2. [ ] SyncLogic install when a Want answer is present

9. **SyncLogic**
   File: [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs).
   1. State
      1. [x] None
   2. Interface
      1. [x] `applySyncResponse` applies Event tail then the edges-plus-Nodes answer
      2. [x] After Events, install Want-answer edges plus Nodes through ResidentProjection
      3. [x] `getPollOutcome` keys on event id; does not branch on `apiVersion`
   3. Uses
      1. [x] ResidentProjection.applyOps
      2. [x] ResidentProjection.installWantAnswer
      3. [x] Load response uses the same answer fields and install path

10. **SyncPlanner**
    File: [src/Shared/SyncPlanner.fs](src/Shared/SyncPlanner.fs).
    1. State
       1. [x] SyncInfo flight (Idle / Polling / Loading / Sending)
    2. Interface
       1. [x] `tryStartPoll` / `tryStartLoad` / `tryStartSubmit` unchanged as flight doors
       2. [ ] Want is payload on those doors, not a new flight state
    3. Uses
       1. [x] Effect `PollServer` / `LoadServer` / `SubmitPendingBatch`

11. **Bullet**
    File: [src/Shared/ViewModelChildrenIndicator.fs](src/Shared/ViewModelChildrenIndicator.fs); paint in [src/Client/RowView.fs](src/Client/RowView.fs).
    1. State
       1. [x] None
    2. Interface
       1. [x] HollowCircle for Unloaded or Unparsed; FoldChevron when children present; SolidCircle otherwise
      2. [x] Unloaded means absent `childMap` key
       3. [x] RowView class `amb-bullet-hollow`
    3. Uses
      1. [x] Graph childMap
       2. [x] `documentState`

12. **Find**
    File: [src/Shared/ViewModelSearch.fs](src/Shared/ViewModelSearch.fs).
    1. State
       1. [x] None
    2. Interface
       1. [x] `searchNodes` / `searchPickSetRoot` stay residence-only and Zoom-like
       2. [ ] No Server Find and no Fetch on commit in this architecture
    3. Uses
       1. [x] Browser Graph, SiteMap

13. **Load command**
    File: [src/Client/UpdateWorkspaceLoad.fs](src/Client/UpdateWorkspaceLoad.fs); Fetch start in [src/Client/UpdateHelpers.fs](src/Client/UpdateHelpers.fs) `tryStartLoadFetch`.
    1. State
       1. [x] QueuedLoad / Loading on SyncInfo
    2. Interface
       1. [x] `loadOp` remains the user-facing Load
      2. [ ] `tryStartLoadFetch` POSTs `/load` and receives edges plus Nodes
       3. [ ] Auto Want does not enter this module
    3. Uses
       1. [x] SyncPlanner.tryStartLoad
      2. [x] ResidentProjection multi-Workspace refuse
      3. [ ] ResidentProjection want-answer builder and installer

14. **Reserved ids**
    File: [src/Shared/GraphBuild.fs](src/Shared/GraphBuild.fs).
    1. State
       1. [x] `rootId`, `trashId`, `workspacesId`, `systemId`
    2. Interface
       1. [x] Canonical ids and SYSTEM spelling
       2. [ ] Bootstrap reserved set is those four Nodes' Children
    3. Uses
       1. [x] NodeId constants

## 3. Seams

1. **Want.compose**
   1. [x] Interface on **Want** — Shared tests for priority and Fold
2. **installWantAnswer**
   1. [x] Interface on **ResidentProjection** — edges plus Nodes; no dangling edges
3. **visibleClosureGraph**
   1. [x] Interface on **ResidentProjection** — saved Zoom produces reserved Children, its ancestor path, and its Children; [09 — Migrate Server Sync doors](issues/09-migrate-server-sync-doors.md) owns the production switch
4. **ChangeSuccessResponse**
   1. [x] Interface on **Sync wire** — current-version Poll and post-Event require `want`; answer requires `nodes` + `childMap`
5. **postPoll / postEvents / getState**
   1. [ ] Interface on **Server Sync doors**
6. **runPollServer / POST changes**
   1. [ ] Interface on **Browser HTTP**
7. **tryStartPoll / tryStartLoad**
   1. [x] Interface on **SyncPlanner** — flight only; Want rides along
8. **rowChildrenIndicator**
   1. [x] Interface on **Bullet**
9. **searchNodes**
   1. [x] Interface on **Find** — residence only
10. **loadOp / tryStartLoadFetch**
    1. [ ] Interface on **Load command** — explicit Fetch uses the same edges-plus-Nodes answer; no legacy API

## 4. Alternative considered

1. **One current residency answer (chosen)** — Want and the edges-plus-Nodes answer ride Poll and post-Event ([src/Server/Api.fs](src/Server/Api.fs) `postPoll` / `postEvents`, [src/Client/App.fs](src/Client/App.fs) Poll and `/{file}/changes`). Bootstrap and explicit Load Fetch use the same answer shape and install door. There is no old/new wire interoperation: request and answer fields are required for `ApiVersion.current = 13`, and the legacy Load `packages` API is removed.
2. **New Want door** — A separate GET/POST for Want. Splits Changes from residency, adds a second flight, and fights SyncPlanner single-flight. Rejected for this Project.
3. **Auto growth through Load** — Reuse `tryStartLoad` / `/load` packages for Included Unloaded Nodes. Contradicts silent wants (no command) and would keep complete-Workspace packages. Rejected.

Expand-contract won over module-build as an internal edit sequence: the new depth (`Want.compose`, `wantAnswer`, and `installWantAnswer`) is small Shared surface, callers migrate to it, then [12 — Contract old Load Fetch packages](issues/12-contract-old-load-fetch-packages.md) removes the legacy code. No deployed compatibility branch remains.

## 5. Unsettled

1. **Server-mode Find** — Ask Server, receive found Nodes, Fetch before navigate. Postponed on [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md); it does not gate residency migration.
