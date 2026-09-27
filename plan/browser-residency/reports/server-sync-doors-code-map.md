# Server Sync doors code map — [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md)

Sources: [arch.md](plan/browser-residency/arch.md), [spec.md](plan/browser-residency/spec.md), ticket 09, and the current tree (Shared wire from [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) is coded; Browser doors stay on ticket [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md)). This report maps production functions and public test seams only. It does not implement code or tests.

## 1. Ticket scope and success shape

1. **Production Server doors** — [src/Server/Api.fs](src/Server/Api.fs) and [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) become the sole owner of Poll POST, post-Event Want answers, small `/state`, and Load Fetch answers that use `nodes` plus `childMap` (not legacy `packages`).
2. **Core stays large** — [CoreChanges](src/Server/Core/CoreChanges.fs) `getState` / `getEventsSince` / `postEvents` stay the authoritative Graph and EventLog; projection runs after Core reads ([arch.md](plan/browser-residency/arch.md) story 35).
3. **Shared builder gap** — [ResidentProjection.fs](src/Shared/ResidentProjection.fs) has `visibleClosureWantAnswer` (bootstrap) and `installWantAnswer` (Browser merge) but **no** `wantAnswer: Graph * NodeId list -> Map<NodeId, ChildNode list> * Node list` yet ([arch.md](plan/browser-residency/arch.md) Module 3 Interface 4; ticket 09 §3.2.4). Ticket 09 needs that Shared function before Server doors can stay thin.
4. **Load wire** — Ticket 09 §6.2.3 requires `postLoad` to answer with the same package as Poll; [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) says no legacy `packages` in the destination. Type removal waits on [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md); 09 still switches the **Server door** to build and encode the edges-plus-Nodes answer.
5. **Out of scope for 09** — Client Poll GET → POST ([App.fs](src/Client/App.fs), [Program.fs](src/Client/Program.fs)), `applyServerTail` → full Want install (ticket 10), Bullet/`childMap` readers (ticket 11), deleting `packagesForTargets` / `installPackages` symbols (ticket 12).

## 2. Current production: Api doors ([src/Server/Api.fs](src/Server/Api.fs))

### 2.1. `getPoll` (today; ticket names `postPoll`)

1. **Symbol** — `Api.getPoll` (lines 40–65). There is **no** `postPoll` yet.
2. **Inputs** — `CoreChanges`, `buildEpochSec`, `pageBuildEpochSec`, `clientEventId: int` (from query `rev`).
3. **Behavior** — `handle.getEventId ()`, optional `handle.getEventsSince queryEventId`, builds `ChangeSuccessResponse` with `nodes = []`, `childMap = Map.empty`, encodes via `changeSuccessResult` → `ApiResponseSerialization.encodeChangeSuccessResponse`.
4. **Gap vs ticket** — No `PollRequest` decode; no Browser `want`; no `ResidentProjection` answer builder; GET-only route (see §3).

### 2.2. `postEvents`

1. **Symbol** — `Api.postEvents` (lines 173–200).
2. **Decode** — `EventJson.decodeEventBatch` only (`{ events }`). Does **not** use `ApiResponseSerialization.decodeChangeRequestDecoder` / `ChangeRequest.want`.
3. **Core** — `handle.postEvents batch.events`.
4. **Answer** — Same empty `nodes` / `childMap` as Poll today; Changes fields preserved in `changeSuccessResult`.
5. **Gap vs ticket** — Required `want` on request; answer must merge `wantAnswer` from full Server Graph after post (typically `handle.getState ()` after accept).

### 2.3. `getState`

1. **Symbol** — `Api.getState` (lines 144–171).
2. **Query** — `parseBootstrapScope` (`?scope=full` → `BootstrapScope.FullGraph`, else `RootClosure`); `parseSavedZoom` (`?zoom=` GUID → `NodeId option`).
3. **Core** — `handle.getState ()` → full `State.graph` in memory.
4. **Projection** — `ResidentProjection.bootstrapStateResponse scope savedZoom response` → **`bootstrapGraph`** / `rootBootstrapGraph` plus optional `extraZoomWorkspace` merge ([ResidentProjection.fs](src/Shared/ResidentProjection.fs) lines 421–442), **not** `visibleClosureGraph`.
5. **Encode** — `ApiResponseSerialization.encodeStateResponse` → JSON (`graph`, `eventId`, `ready`, `liveFocusIds`).
6. **Gap vs ticket** — Production `/state` must use `visibleClosureGraph savedZoom graph` (or equivalent wrapper calling `visibleClosureWantAnswer`) per [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) and ticket §2–3. Keep `?scope=full` test door on `FullGraph` unchanged.

### 2.4. `postLoad`

1. **Symbol** — `Api.postLoad` (lines 87–128); helper `loadPackages` (lines 67–85).
2. **Decode** — `ApiResponseSerialization.decodeLoadRequestDecoder` → `LoadRequest` (`eventId`, `targets: LoadTarget list`).
3. **Package build** — `loadPackages` → `handle.getState ()` → `ResidentProjection.packagesForTargets stateResponse.graph targets` (workspace subgraph, `LoadRefuse.MultiWorkspace`).
4. **Events** — `getEventsSince request.eventId` when server revision ahead.
5. **Encode** — `LoadResponse` with `packages` and `packageChildMap`; wire keys `"packages"` and `"childMap"` via `encodeLoadResponse`.
6. **Gap vs ticket** — Replace package builder with Want-style `nodes` + `childMap` from Server Graph (likely `wantAnswer` over derived parent ids from `targets`, plus rules for `includeWorkspace` / multi-Workspace refuse retained via `selectionSpansMultipleWorkspaces`). Response shape should match Poll/post-Event answer fields, not workspace `packages`.

### 2.5. Shared private helpers on Api (reuse or extend)

1. `changeSuccessResult` / `encodeChangeSuccessResponse` — already emits required `nodes` and `childMap` on answers.
2. `decodeQueryEventId` — Poll POST can reuse `EventId.fromJson` on body's `eventId` instead of query `rev`.
3. `agentErrorResult` / `internalError` — unchanged error paths.

## 3. Route registration ([src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs))

1. **`registerStateRoutes`** (lines 165–304) binds cookie-admitted `CoreChanges` via `withBrowserChanges`.
2. **GET `/ambit/state`** → `Api.getState handle req` (lines 169–186). Preserves query `scope` and `zoom`.
3. **GET `/ambit/poll`** → `Api.getPoll handle deployEpoch pageEpoch (parseClientEventId req)` (lines 187–198). `parseClientEventId` reads query `rev` (lines 145–151).
4. **POST `/ambit/load`** → `Api.postLoad` with body (lines 199–211).
5. **POST `/ambit/changes`** and **POST `/ambit/events`** → same `Api.postEvents` (lines 212–239).
6. **Surgical route work for ticket 20.2** — Replace `MapGet("/ambit/poll", …)` with `MapPost("/ambit/poll", …)` reading body, calling new `Api.postPoll` (or renamed handler). Remove GET Poll door; no compatibility Poll GET. HTTP tests that use `client.GetAsync("/ambit/poll?rev=…")` must move to POST + `PollRequest` JSON ([StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs), [BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs)).

## 4. ResidentProjection ([src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs))

### 4.1. Bootstrap / visible closure (ticket §2, story 8)

1. **`visibleClosureWantAnswer`** (lines 115–138) — `savedZoom option * Graph` → `childMap * Node list`. Parents: reserved ROOT/TRASH/workspacesId/systemId, owner-ancestor path to resolved Zoom, Zoom node. Edges only when `GraphChildren.tryGet` is `Some` on the **Server** Graph (Loaded parent on Server). Nodes: distinct parent and child ids present in `graph.nodes`.
2. **`visibleClosureGraph`** (lines 142–150) — wraps want answer into a scoped `Graph` via `Graph.fromNodes`.
3. **`resolveZoom`** (lines 87–90) — stale/missing Zoom → `graph.root`.
4. **`bootstrapGraph` / `bootstrapStateResponse`** (lines 421–442) — **current production path** for `/state`; uses `rootBootstrapGraph` and optional whole nested Workspace slice. **Not** the destination bootstrap.

### 4.2. Want answer builder (ticket §3 — to add)

1. **Missing `wantAnswer`** — Arch and ticket require `Graph * NodeId list -> Map<NodeId, ChildNode list> * Node list` with: empty `want` → empty map and list; unknown parent ids skipped; for each wanted id, if Server parent is Loaded (`GraphChildren.tryGet`), copy child list; include every child **Node** header; **omit** an edge entry rather than emit a child id without a Node (pairs with `installWantAnswer` refuse on Browser).
2. **Pattern to mirror** — `visibleClosureWantAnswer` edge/node collection (lines 120–137) but parents = explicit `want` list, not bootstrap set.
3. **Legacy Load (until 12)** — `packagesForTargets`, `projectWorkspaceSlice`, `workspaceSubgraph`, `captureLoadResponse` (lines 260–352) remain compiled; 09 **`postLoad`** should stop calling `packagesForTargets` at the door even if symbols stay for 12.

### 4.3. Browser-side install (reference only; Shared already done)

1. **`installWantAnswer`** (lines 56–79) — public; Server tests can round-trip Server-built packages through this in Shared.Tests; Server.Tests should assert HTTP JSON decodes to nonempty `nodes`/`childMap` when Want is nontrivial.

## 5. Wire encode and decode ([ApiResponses.fs](src/Shared/ApiResponses.fs), [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs))

### 5.1. Version and answer shape

1. **`ApiVersion.current = 13`** — Want plus edges/Nodes on Poll and post-Event ([ApiResponses.fs](src/Shared/ApiResponses.fs) lines 8–10).
2. **`ChangeSuccessResponse`** — Required `nodes`, `childMap`; short keys on wire `r`, `b`, `p`, `v`, `ready`, `externalChanges`, `c` ([ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs) lines 42–95). Server already can encode nonempty Want answers once Api fills the record.
3. **`PollRequest`** — `{ eventId: int; want: NodeId[] }`; required `want` ([encodePollRequest](src/Shared/ApiResponseSerialization.fs) lines 115–127). Shared.Tests: ``PollRequest always encodes want including empty``, ``PollRequest fails decode when want is missing`` ([SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs)).
4. **`ChangeRequest`** — `{ events; want }`; decode **not** wired in Server `postEvents` yet ([decodeChangeRequestDecoder](src/Shared/ApiResponseSerialization.fs) lines 137–146). Shared.Tests round-trip with Changes beside want.

### 5.2. Load (dual shape today)

1. **`LoadResponse`** — F# fields `packages`, `packageChildMap`; wire `"packages"` + `"childMap"` ([encodeLoadResponse](src/Shared/ApiResponseSerialization.fs) lines 179–230).
2. **`SyncLogic.loadResponseToSync`** — maps `packages` → `SyncResponse.nodes`, `packageChildMap` → `childMap` ([SyncLogic.fs](src/Shared/SyncLogic.fs) lines 115–120). Ticket 09 Server answer should populate the **same** logical fields the Browser will install after ticket 10/12 align types.
3. **`StateResponse`** — Full `graph` JSON via `Serialization.encodeGraph`; no Want fields on bootstrap.

### 5.3. Event batch (legacy post-Event body)

1. **`EventJson.decodeEventBatch`** — still what [Api.postEvents](src/Server/Api.fs) uses. Migrate door must switch to `ChangeRequest` and reject bodies without `want` (Shared codec already strict).

## 6. Core contract (unchanged surface for 09)

1. **`CoreChanges`** ([CoreChanges.fs](src/Server/Core/CoreChanges.fs)) — `getState`, `getEventId`, `getEventsSince`, `postEvents : Ev list -> …`, `isReady`. No Want parameter on Core.
2. **`CoreMailbox.coreChanges`** — production handle behind routes ([RouteRegistration.fs](src/Server/RouteRegistration.fs) `boundChanges`).
3. **Server projection rule** — After `getState` or successful `postEvents`, run `wantAnswer graph want` on the **large** Graph returned from Core; never shrink Core cache.

## 7. Existing Server tests and public seams

### 7.1. Direct Api unit tests (primary 09 targets)

| File | Seam | What it proves today | Likely 09 delta |
|------|------|----------------------|-----------------|
| [ApiGetStateTests.fs](tests/Server.Tests/ApiGetStateTests.fs) | `handleWithGetState` + `Api.getState` | JSON success, `liveFocusIds`, `?scope=full`, **ROOT bootstrap + extra Workspace on zoom** | Rewrite zoom cases for **visible-closure** (dir Resident under zoom path; **no** full Workspace load). Add reserved-Children / ancestor-path assertions aligned with [WantTests.fs](tests/Shared.Tests/WantTests.fs) ``visibleClosureGraph …``. |
| [ApiPostLoadTests.fs](tests/Server.Tests/ApiPostLoadTests.fs) | `handleForLoad` + `Api.postLoad` | `packages`, `packageChildMap`, MultiWorkspace 400 | Assert `nodes`/`childMap` (or decoded Load answer mapped like Poll); drop expectations on `packages` when wire switches. |
| [CoreChangesTests.fs](tests/Server.Tests/CoreChangesTests.fs) | `Api.getPoll`, `Api.postEvents` | Poll Changes tail; postEvents accepts `EventBatch` only | Replace `getPoll` with `postPoll` + `PollRequest`; postEvents body via `encodeChangeRequest`; assert decoded `nodes`/`childMap` when `want` nonempty. |
| [CoreRuntimeTests.fs](tests/Server.Tests/CoreRuntimeTests.fs) | `Api.postEvents` HTTP adapter | Auth and enqueue | Update POST body to `ChangeRequest` with `want: []`. |
| [CoreCredentialsTests.fs](tests/Server.Tests/CoreCredentialsTests.fs) | `Api.postEvents` | EventBatch bodies | Same ChangeRequest migration. |

### 7.2. HTTP integration tests (Poll route and bootstrap)

1. **[StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs)** — Many flows use `GET /ambit/poll?rev=0` and `decodeChangeSuccessResponseDecoder`; `/ambit/state` shape assertions on user tree. Will need POST Poll and possibly revised bootstrap tree expectations when `getState` switches to `visibleClosureGraph`.
2. **[BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs)** — `GET /ambit/poll` auth smoke → POST Poll.
3. **[TestBackend.fs](tests/Server.Tests/TestBackend.fs)** — Comment lines 206–207 document scoped ROOT bootstrap; update comment when visible-closure lands.

### 7.3. Shared tests (builder proof; not Server.Tests but same seams)

1. **[WantTests.fs](tests/Shared.Tests/WantTests.fs)** — `visibleClosureGraph`, `visibleClosureWantAnswer`, `installWantAnswer` (bootstrap and dangling refuse).
2. **[SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs)** — Poll/Change request and ChangeSuccess Want answer round-trips.
3. **[SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs)** — `changeSuccessToSync`, `applySyncResponse` with Want answer (Browser apply; reference for answer shape Server must emit).
4. **[LoadCaptureTests.fs](tests/Shared.Tests/LoadCaptureTests.fs)** — `packagesForTargets`, `captureLoadResponse` (legacy; 09 adds parallel tests for Load **`wantAnswer`** path when builder exists).

### 7.4. Recording handle pattern (new Poll/post-Event proofs)

1. **Pattern** — [CoreChangesTests.fs](tests/Server.Tests/CoreChangesTests.fs) `recordingHandle` (lines 117–138): stub `CoreChanges` with fixed graph, spy `postEvents`. Extend with a graph large enough that `want` on one Unloaded Included parent returns nonempty `childMap` and matching `nodes` in JSON.

## 8. Surgical change list (by file, minimal diff intent)

### 8.1. [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs)

1. Add **`wantAnswer`** (and optionally **`wantAnswerOrEmpty`** wrapper for `[]`) using `GraphChildren.tryGet`, child Node collection, no dangling edges.
2. Optionally add **`bootstrapStateResponseVisibleClosure`** or change **`bootstrapStateResponse`** `RootClosure` branch to call **`visibleClosureGraph`** instead of **`bootstrapGraph`** (ticket prefers visible closure; keep `FullGraph` branch).

### 8.2. [src/Server/Api.fs](src/Server/Api.fs)

1. Replace **`getPoll`** with **`postPoll`**: decode `PollRequest`, `getEventsSince`, `getState` for graph, `wantAnswer`, fill `ChangeSuccessResponse`.
2. **`postEvents`**: decode `ChangeRequest`; post `request.events`; build Want answer from post-state graph and `request.want`.
3. **`getState`**: call visible-closure bootstrap (§8.1) instead of `bootstrapStateResponse` ROOT-closure path.
4. **`postLoad`**: replace `loadPackages`/`packagesForTargets` with Want-style builder + same revision/events envelope; encode **`nodes`/`childMap`** on Load response per ticket (coordinate `LoadResponse` / `encodeLoadResponse` if wire keys change before ticket 12).
5. Extract small private **`wantAnswerForRequest graph want`** to avoid duplicating Poll/post-Event/Load tail logic.

### 8.3. [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs)

1. **`MapPost "/ambit/poll"`** with body; delete **`MapGet "/ambit/poll"`**.
2. Stamps (`DeployEpochSec`, `PageBuildEpochSec`) unchanged.

### 8.4. Shared wire (only if Load JSON must match Poll before ticket 12)

1. [ApiResponses.fs](src/Shared/ApiResponses.fs) / [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs) — align `LoadResponse` encode with `nodes` + `childMap` field names on wire; keep `loadResponseToSync` mapping until 12 deletes `packages`.

### 8.5. Tests (ticket §4 Server proof)

1. New or extended facts in **ApiGetStateTests**, **ApiPostLoadTests**, **CoreChangesTests**; fix **StateEndpointTests** / **BrowserCredentialTests** Poll HTTP method.
2. Register new test modules in [Gambol.Server.Tests.fsproj](tests/Server.Tests/Gambol.Server.Tests.fsproj) if split files.
3. Add **Shared.Tests** for **`wantAnswer`** (Server-shape package, empty want, omit dangling edge).

## 9. Focused commands (likely green iteration)

1. **Shared builder and wire (pre-Server or with Server)** — `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter "FullyQualifiedName~WantTests|FullyQualifiedName~SerializationTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~LoadCaptureTests"`
2. **Server Api doors** — `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --filter "FullyQualifiedName~ApiGetStateTests|FullyQualifiedName~ApiPostLoadTests|FullyQualifiedName~CoreChangesTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreCredentialsTests"`
3. **Poll/state HTTP** — `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --filter "FullyQualifiedName~StateEndpointTests|FullyQualifiedName~BrowserCredentialTests"`
4. **Full Server suite (before merge)** — `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj`
5. **Build** — `dotnet build src/Server/Gambol.Server.fsproj`

## 10. Dependency order for implementers

1. Implement **`ResidentProjection.wantAnswer`** + Shared.Tests (narrowest seam; mirrors ticket §3 and arch story 23 Server hop).
2. Switch **`getState`** projection to **`visibleClosureGraph`**; fix **ApiGetStateTests** and selective **StateEndpointTests** bootstrap expectations.
3. Add **`postPoll`**, route POST, remove GET; decode **`PollRequest`**; wire **`wantAnswer`** on answer.
4. Migrate **`postEvents`** to **`ChangeRequest`**; same answer builder.
5. Migrate **`postLoad`** off **`packagesForTargets`** to edges-plus-Nodes answer; update **ApiPostLoadTests** and Load encode.
6. Do **not** remove legacy **`packages`** types until ticket 12; do **not** change Client Poll URL until ticket 10 (Server proof uses direct Api tests and HTTP tests with new POST Poll).

## 11. Risk notes

1. **`getState zoom outside ROOT adds owning Workspace`** ([ApiGetStateTests.fs](tests/Server.Tests/ApiGetStateTests.fs)) encodes **old** `extraZoomWorkspace` behavior; visible-closure **intentionally** does not download a full nested Workspace (story 8). Expect test rewrites, not production parity with old test names.
2. **Load `includeWorkspace`** — Today expands to full workspace subgraph via `packagesForTargets`. Destination Load Fetch must still refuse multi-Workspace and honor Fetch intent; **`wantAnswer`** alone may need a small Shared helper to map `LoadTarget list` → parent ids (or temporary bridge until 12 deletes workspace package code).
3. **post-Event decode break** — Any client still posting raw `EventBatch` to `/ambit/changes` will fail once Server requires `want`; acceptable per “no compatibility door” for version 13 migrate batch; Browser fix is ticket 10.
