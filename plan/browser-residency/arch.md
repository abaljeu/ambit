# Browser residency architecture

Spec: [[spec.md]]
Updated: 2026-09-26
Sequence: expand-contract

Sources: [map.md](map.md) Destination and Notes (2026-09-26 locks; Decisions so far empty); [spec.md](spec.md) Solution and 36 stories. Prefer existing seams. Field names for Want on the wire stay Unsettled ([01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md)). `Graph.childMap` (absent key = Unloaded) is the residency list. Checklist: `[x]` already true of the recorded shape; `[ ]` still to build. Sequence is expand-contract: expand Poll and post-Event with Want plus edges and Nodes; dual-run old Load Fetch `packages`; contract later when [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md) kills the old path.

## 1. Story paths

1. **Open a large Server Graph**
   1. [ ] Person opens the Browser
   2. [x] [src/Client/Program.fs](src/Client/Program.fs) boots via `/state` or boot Poll
   3. [ ] [src/Server/Api.fs](src/Server/Api.fs) `getState` returns a visible-closure Graph, not the whole Server Graph
   4. [ ] Browser first paint uses that scoped Graph only

2. **Included first paint**
   1. [ ] Visible-closure bootstrap includes Included under Zoom
   2. [x] [src/Shared/IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs) walks SiteMap honoring Fold
   3. [x] SiteMap render shows those Nodes

3. **Framing path**
   1. [ ] Bootstrap includes ancestors of the Zoom root
   2. [ ] Those ancestors are Resident so the framing path exists
   3. [x] SiteMap roots at Zoom

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
   1. [ ] `ResidentProjection.bootstrapGraph` stops at visible-closure
   2. [ ] It does not call today's complete-ROOT `rootBootstrapGraph` / extra Workspace package as the production scope
   3. [x] `BootstrapScope.FullGraph` stays a test door (`?scope=full`)

9. **Same package after bootstrap**
   1. [ ] Bootstrap answer is edges plus pointed-at Nodes
   2. [ ] Later Want answers use that same package
   3. [ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) installs both through one install door

10. **Hollow Unloaded Bullet**
    1. [x] [src/Shared/ViewModelChildrenIndicator.fs](src/Shared/ViewModelChildrenIndicator.fs) hollow when Unloaded
    2. [ ] Indicator reads absent `childMap` key as Unloaded
    3. [x] [src/Client/RowView.fs](src/Client/RowView.fs) paints `amb-bullet-hollow`

11. **Hollow Unparsed Bullet**
    1. [x] Indicator hollow when `documentState = Unparsed`
    2. [x] Unloaded and Unparsed stay distinct facts on one glyph
    3. [x] RowView uses the same hollow class

12. **No new loading Status**
    1. [x] No per-Node loading Status on Node
    2. [ ] Residency stays absent/present `childMap` plus Unparsed
    3. [x] SyncInfo `Loading` stays the global Load-command flight, not a Node field

13. **Auto want Included**
    1. [ ] Want module lists Included Nodes that miss Children first
    2. [ ] [src/Client/App.fs](src/Client/App.fs) attaches that Want on Poll and post-Event
    3. [ ] No click and no Load

14. **Auto want those Children**
    1. [ ] Want module lists those Children second
    2. [ ] Same Poll / post-Event doors carry the combined Want
    3. [ ] Server answers their `childMap` edges plus Nodes

15. **No third ongoing tier**
    1. [ ] Want.compose does not add reserved Nodes or Zoom ancestors as an ongoing tier
    2. [ ] Those ids stay bootstrap-only
    3. [ ] Shared test asserts two-tier order only

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
    1. [x] App `runPollServer` GETs `/{file}/poll`
    2. [ ] Request carries the current Want (query or body — Unsettled field names)
    3. [ ] `Api.getPoll` answers Changes plus edges plus Nodes

21. **Edges in the answer**
    1. [ ] Server builds `childMap` entries for each wanted parent
    2. [ ] SyncLogic / ResidentProjection installs those edges
    3. [ ] Wanted parents become Loaded when the key is present

22. **Nodes in the answer**
    1. [ ] Server includes each Child Node the edges point at, as a separate collection
    2. [ ] Install adds those Nodes to `graph.nodes`
    3. [ ] No edge is installed without its target Node

23. **No dangling edges**
    1. [ ] Install refuses an edge whose target is not in the Node collection and not already Resident
    2. [ ] Shared test covers the refuse
    3. [ ] Browser Graph never shows a header-less Child

24. **Absent key stays Unloaded**
    1. [x] Destination rule: absent `childMap` key = Unloaded
    2. [ ] Install does not insert an empty key unless the Server sent `[]`
    3. [ ] Indicator treats absent key as hollow Unloaded

25. **Present key is Loaded**
    1. [x] Destination rule: present key including `[]` = Loaded
    2. [ ] Install of `[]` marks a true leaf Loaded
    3. [ ] A Loaded empty list is not a hollow Unloaded Bullet unless Unparsed

26. **Children arrive on the Bullet**
    1. [ ] After install, `childMap` is present for that parent
    2. [ ] Indicator leaves HollowCircle when children exist (chevron) or when Loaded and not Unparsed (solid)
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

31. **Load may dual-run Fetch**
    1. [x] `SyncPlanner.tryStartLoad` emits `LoadServer`
    2. [x] App `runLoadServer` POSTs `/load`; `LoadResponse.packages` remain
    3. [ ] Auto and bootstrap do not use `packages`; they use edges plus Nodes
    4. [ ] Death of `packages` stays [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md)

32. **Commands that name Nodes later**
    1. [ ] No new command that names Nodes
    2. [x] Auto Want is not a command
    3. [x] Load remains the existing user-facing command

33. **SiteMap honors Fold**
    1. [x] IncludedDescendantIds stops at folded children
    2. [ ] Want.compose uses that Included list, not a deep unfold
    3. [x] Folded Nodes are not treated as a deep visible tree

34. **Unloaded is not empty**
    1. [ ] Absent `childMap` key never renders as a Loaded leaf
    2. [x] Hollow Bullet means Children are not here or Unparsed
    3. [ ] Person can tell Unloaded from a Loaded empty list

35. **Server stays large**
    1. [x] Server `getState` handle still holds the full Core Graph
    2. [ ] Visible-closure is a projection for the Browser answer only
    3. [ ] Later Wants read the same large Server Graph

36. **Reserved spelling**
    1. [x] GraphBuild `systemId` and SYSTEM name stay SYSTEM
    2. [ ] Bootstrap reserved set cites ROOT, TRASH, Workspaces Node, SYSTEM
    3. [x] No Trash / System spelling in this Project's doors

Shared segments:
1. [ ] Want.compose (Included missing Children, then those Children)
2. [ ] Poll and post-Event carry Want with Changes
3. [ ] Server answers edges plus pointed-at Nodes (no dangling edges)
4. [ ] ResidentProjection installs that package into `Graph.childMap`
5. [x] Bullet from Unloaded / Unparsed via ViewModelChildrenIndicator
6. [ ] Visible-closure bootstrap (reserved Children + Zoom ancestors + Included)

Narrowest shared test seam:
1. [ ] Want.compose + installWantAnswer in Shared (no HTTP): given Graph, SiteMap, Zoom, and an answer package, the next Graph has Loaded wanted parents, Resident Children, and no dangling edges

## 2. Module map

1. **Graph childMap**
   File: [src/Shared/Model.fs](src/Shared/Model.fs) (`Graph`); helpers in [src/Shared/GraphBuild.fs](src/Shared/GraphBuild.fs) / Graph lookup.
   1. State
      1. [ ] `childMap: Map<NodeId, ChildNode list>` — absent key = Unloaded; present key including `[]` = Loaded
      2. [ ] `nodes` holds Resident headers; Children lists do not live on Node
   2. Interface
      1. [ ] Lookup Children only through `childMap` (or a derived `ChildrenStatus`)
      2. [ ] Install never writes an edge whose target Node is absent
   3. Uses
      1. [x] NodeId, ChildNode, Ownership

2. **Want**
   File: new [src/Shared/Want.fs](src/Shared/Want.fs) (name may match the locked term Want).
   1. State
      1. [ ] None durable — a Want is a list of parent Node ids whose Children are desired
   2. Interface
      1. [ ] `compose: Graph * SiteMap * zoomRoot -> NodeId list` — (1) Included that miss Children, (2) those Children; no third tier
      2. [ ] Empty list is allowed; encoding on the wire is Unsettled
   3. Uses
      1. [x] IncludedDescendantIds
      2. [ ] Graph childMap (miss = absent key)

3. **ResidentProjection**
   File: [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs).
   1. State
      1. [ ] None beyond the Graph it returns
   2. Interface
      1. [ ] `bootstrapGraph` becomes Zoom-scoped visible-closure: `childMap` for ROOT, TRASH, Workspaces Node, SYSTEM; ancestors of Zoom root; Included. Not complete Workspace
      2. [ ] `installWantAnswer: edges * nodes * Graph -> Result<Graph, string>` — edges and Nodes separately; refuse dangling edges
      3. [x] `packagesForTargets` / `installPackages` stay for dual-run Load Fetch
   3. Uses
      1. [x] GraphBuild reserved ids
      2. [ ] Graph childMap
      3. [x] GraphQuery enclosing / ancestors

4. **Included**
   File: [src/Shared/IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs).
   1. State
      1. [x] None
   2. Interface
      1. [x] `expand: Graph * SiteMap * startId -> NodeId list` honoring Fold
      2. [ ] Walk `childMap`, not Node.children, once childMap is the list
   3. Uses
      1. [x] SiteMap, Graph

5. **Sync wire**
   File: [src/Shared/ApiResponses.fs](src/Shared/ApiResponses.fs).
   1. State
      1. [x] `ChangeSuccessResponse` Events, `apiVersion`, Poll stamps
      2. [x] `LoadResponse.packages` for dual-run Fetch
      3. [ ] Additive Want-answer fields (edges + Nodes). Exact names Unsettled
   2. Interface
      1. [ ] Encode / decode Want on Poll and post-Event without dropping Changes
      2. [x] `ApiVersion.current` remains the incompatibility marker; when the Want + edges/Nodes package ships, bump minor on the existing major ((old major).(minor + 1))
   3. Uses
      1. [x] Ev, EventId, Graph types

6. **Server Sync doors**
   File: [src/Server/Api.fs](src/Server/Api.fs); routes in [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs).
   1. State
      1. [x] None — doors read Core Graph / EventLog
   2. Interface
      1. [ ] `getPoll` and `postEvents` accept Want and return Changes plus edges plus Nodes
      2. [ ] `getState` returns visible-closure via ResidentProjection
      3. [x] `postLoad` still returns `packages` (dual-run)
   3. Uses
      1. [x] CoreChanges `getState` / `getEventsSince` / `postEvents`
      2. [ ] ResidentProjection bootstrap + Want answer
      3. [x] ApiResponses encode

7. **Browser HTTP**
   File: [src/Client/App.fs](src/Client/App.fs).
   1. State
      1. [x] In-memory VM Graph and `eventId`
   2. Interface
      1. [ ] `runPollServer` sends current Want with Poll
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
      2. [ ] First `/state` Graph is visible-closure; boot Poll may carry Want after that Graph exists
   3. Uses
      1. [x] ChangeSuccessResponse decode
      2. [ ] SyncLogic install when a Want answer is present

9. **SyncLogic**
   File: [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs).
   1. State
      1. [x] None
   2. Interface
      1. [x] `applySyncResponse` applies Event tail then packages
      2. [ ] After Events, install Want-answer edges plus Nodes through ResidentProjection
      3. [x] `getPollOutcome` still keys on `apiVersion` and event id
   3. Uses
      1. [x] ResidentProjection.applyOps
      2. [ ] ResidentProjection.installWantAnswer
      3. [x] LoadResponse dual-run via `loadResponseToSync`

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
       2. [ ] Unloaded means absent `childMap` key
       3. [x] RowView class `amb-bullet-hollow`
    3. Uses
       1. [ ] Graph childMap
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
       2. [x] `tryStartLoadFetch` still POSTs `/load` packages when targets need a Workspace
       3. [ ] Auto Want does not enter this module
    3. Uses
       1. [x] SyncPlanner.tryStartLoad
       2. [x] ResidentProjection multi-Workspace refuse

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
   1. [ ] Interface on **Want** — Shared tests for priority and Fold
2. **installWantAnswer**
   1. [ ] Interface on **ResidentProjection** — edges plus Nodes; no dangling edges
3. **bootstrapGraph**
   1. [ ] Interface on **ResidentProjection** — visible-closure set
4. **ChangeSuccessResponse**
   1. [ ] Interface on **Sync wire** — Poll and post-Event share it
5. **getPoll / postEvents / getState**
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
    1. [x] Interface on **Load command** — dual-run old Fetch

## 4. Alternative considered

1. **Expand Poll and post-Event (chosen)** — Want and the edges-plus-Nodes answer ride the doors that already carry Changes ([src/Server/Api.fs](src/Server/Api.fs) `getPoll` / `postEvents`, [src/Client/App.fs](src/Client/App.fs) Poll and `/{file}/changes`). One flight, one apply order (Events then residency). Matches the Destination lock. Sequence expand-contract fits: additive fields now; old `LoadResponse.packages` stay until [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md).
2. **New Want door** — A separate GET/POST for Want. Splits Changes from residency, adds a second flight, and fights SyncPlanner single-flight. Rejected for this Project.
3. **Auto growth through Load** — Reuse `tryStartLoad` / `/load` packages for Included Unloaded Nodes. Contradicts silent wants (no command) and would keep complete-Workspace packages. Rejected.

Expand-contract won over module-build: the new depth (Want.compose + installWantAnswer) is small Shared surface, but the product change is a wire dual-run on existing doors, not a green-field module stack.

## 5. Unsettled

1. **Want wire fields** — Exact request and response field names, edge-versus-Node JSON layout, empty-Want encoding. [01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md). The ApiVersion bump rule is locked on the map; this ticket still locks field shape.
2. **Zoom-restore edge cases** — Missing or stale saved Zoom. [02 — Lock bootstrap visible-closure set](issues/02-lock-bootstrap-visible-closure.md).
3. **Want cadence** — Whether every Poll and every post-Event always carry Want. [03 — Lock ongoing want priority and when wants are attached](issues/03-lock-ongoing-want-priority.md).
4. **Server-mode Find** — Ask Server, receive found Nodes, Fetch before navigate. [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md).
5. **Load Fetch death** — When `packages` die. [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md).
