# 18 — Read stored state on the client path (scope)

Alan’s decision on 2026-11-03 restores the shared `readState` helper in `tests/Server.Tests/StateEndpointTests.fs` to the Browser client’s natural pull path: default GET `/{file}/state`, Poll with `Want.compose`, `installWantAnswer`, and a `continueAutoWant`-style loop with a safety cap. The slice moves every `GET state …` endpoint test into a new `tests/Server.Tests` file registered in `Gambol.Server.Tests.fsproj`; `StateEndpointTests.fs` keeps Change-submission, Poll, concurrency, dedup, Reject, and DB-restart tests. Test assertions stay the same; only state read mechanics and file placement change. Alan approved the multi-file sweep on 2026-11-03 against the usual per-ticket file count in `plan/context/size-limit.md`. Branch `cursor/read-state-client-path-f359` matches staging for this inventory.

### Signatures

[ ] `let private decode decoder json` in `tests/Server.Tests/StateEndpointTests.fs` runs `Decode.fromString` and fails on error.
[ ] `let private decodeEventId json` decodes `{ eventId }` via `EventJson.decodeEventId`.
[ ] `let private decodeGraph json` decodes `{ graph }` via `Serialization.decodeGraph`.
[ ] `let private decodePoll json` decodes a Poll body via `ApiResponseSerialization.decodeChangeSuccessResponseDecoder`.
[ ] `let private getStateJson (client: HttpClient)` returns the default GET `/ambit/state` body string after status and content-type checks.
[ ] `let private getStateJsonFull (client: HttpClient)` returns GET `/ambit/state?scope=full` body string after status checks.
[ ] `let private readFullState (client: HttpClient)` returns `Task<EventId * Graph>` from `getStateJsonFull` plus `decodeEventId` and `decodeGraph`.
[ ] `let private readState (client: HttpClient)` is currently `readFullState client` (redirect from [06 — Server sends child list](plan/client-never-fails/issues/06-server-sends-child-list/06-server-sends-child-list-ticket.md)).
[ ] `let private readClosure (client: HttpClient)` returns `Task<EventId * Graph>` from `getStateJson` plus `decodeEventId` and `decodeGraph`.
[ ] `let private stateRoot (client: HttpClient)` returns `Task<NodeId>` from `readState` then `graph.root`.
[ ] `let private postPoll (client: HttpClient) eventId` POSTs `/ambit/poll` with `ApiResponseSerialization.encodePollRequest { eventId; want = [] }`.
[ ] `Want.compose (graph: Graph) (siteMap: SiteMap) (zoomRoot: NodeId) : NodeId list` in `src/Shared/Want.fs` walks Included expanded rows, adds two ranks of children, and keeps ids where `not (GraphChildren.isLoaded graph id)`.
[ ] `ResidentProjection.installWantAnswer (edges: Map<NodeId, ChildNode list>) (nodes: Node list) (graph: Graph) : Result<Graph, string>` in `src/Shared/ResidentProjection.fs` merges Poll answer Headers and child-id lists into the Graph.
[ ] `currentWant (model: VM) : NodeId list` in `src/Client/UpdateHelpers.fs` is `Want.compose model.graph model.siteMap model.zoomRoot`.
[ ] `currentPollRequest (eventId: EventId) (model: VM) : PollRequest` sets `want = currentWant model`.
[ ] `continueAutoWant (response: SyncResponse) (model: VM, effects: Effect list)` in `src/Client/Update.fs` starts another Poll when the answer installed nodes or childMap and `currentWant` stays non-empty.
[ ] `ApiResponseSerialization.decodeChangeSuccessResponseDecoder` decodes Poll and Change success fields `r` (eventId), `nodes`, and `childMap` among others (`src/Shared/ApiResponseSerialization.fs`).
[ ] `ApiResponseSerialization.encodePollRequest (request: PollRequest)` encodes `{ eventId = int; want = NodeId list }`.
[ ] `Api.postPoll` in `src/Server/Api.fs` decodes `request.want` and answers through `wantAnswerFromHandle handle request.want`.

### Match sites

[ ] `readState` delegates to `readFullState` at `StateEndpointTests.fs` line 99.
[ ] `stateRoot` calls `readState` at `StateEndpointTests.fs` lines 106–108.
[ ] `readPoll` calls `postPoll client EventId.zero` at lines 136–140.
[ ] `postPoll` hard-codes `want = []` at lines 116–123.
[ ] `encodeChangeRequestBody` sets `want = []` on Change POST bodies at lines 111–114.
[ ] Poll HTTP route is `POST /ambit/poll` in tests (`postPoll`) and in `src/Client/App.fs` `runPollServer` (lines 213–216).
[ ] Default boot GET URL is `stateUrl` in `src/Client/Program.fs` lines 71–75 (`/{file}/state` or `?zoom=`; no `scope` query).
[ ] Server `wantAnswerFromHandle` matches `ResidentProjection.wantAnswer state.graph want` at `src/Server/Api.fs` lines 42–56 and 94–95.
[ ] `userRootChildren` indexes `graph.nodes.[c.id]` at `tests/Shared.Tests/SpecialNodeTestHelpers.fs` lines 45–47.
[ ] `userTreeShape` recurses through `graph.nodes.[nodeId]` at lines 50–61.

### Existing tests

[ ] File `tests/Server.Tests/StateEndpointTests.fs` holds Server HTTP Change-submission and state endpoint coverage; no module comment states intent beyond helper comments at lines 77–99.

#### (a) GET state tests that move to the new file

[ ] ``GET state returns revision 0 for fresh server`` (line 395): `readState`; asserts `eventId` is revision 0.
[ ] ``GET state returns valid graph with root node`` (line 402): `readState`; asserts empty user node count, ROOT text, empty `userRootChildren`.
[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` (line 1166): `readClosure`; asserts workspace and inner ids stay Absent on default GET while workspace id appears under Workspaces child list.
[ ] ``GET state scope full returns canonical graph for tests`` (line 1180): `readFullState`; asserts workspace and inner Headers exist under `?scope=full` (stays on full scope in the new file).

#### (b) Change-submission and related tests that stay in StateEndpointTests.fs

[ ] ``unavailable Database serves read-only file fallback and ignores Persistence:Mode`` (373): raw GET `/ambit/state` with `decodeEventId` and `decodeGraph`; read-only POST rejection.
[ ] ``POST Change and inverse Changes return complete confirmations in request order`` (428): `stateRoot`, `readState` (452), `readPoll`.
[ ] ``Edit reply with only a persist Notice has no external changes`` (467): `stateRoot`, `readPoll`.
[ ] ``large paste inverse total response is measured`` (487): timing only; no state read helper.
[ ] ``POST explicit Undo JSON is rejected`` (507): `readState` (517).
[ ] ``POST unchanged submission is rejected`` (522): `readState` (529).
[ ] ``POST changes accepts X-Gambol-Client header`` (534): `stateRoot` only.
[ ] ``POST changes SetText changes child text and bumps revision`` (551): `stateRoot`, `readState` (560).
[ ] ``POST changes NewNode+Replace adds child to root`` (565): `stateRoot`, `readState` (576).
[ ] ``POST changes with invalid JSON returns 400`` (585): `getStateJson` (587).
[ ] ``POST changes with bad op returns 400`` (594): `getStateJson` (596).
[ ] ``POST changes twice bumps revision to 2`` (607): `stateRoot`, `readState` (616).
[ ] ``POST changes batch with two changes bumps revision to 2`` (621): `stateRoot`, `readState` (637).
[ ] ``POST changes batch with bad second change keeps earlier items`` (642): `stateRoot`, `readState` (651), `readPoll`.
[ ] ``POST changes persists in GET state`` (664): `stateRoot`, `readState` (674).
[ ] ``POST same submissionId twice is idempotent`` (680): `stateRoot`, `readState` (705).
[ ] ``POST with stale base revision and valid SetText succeeds`` (711): `stateRoot`, `readState` (721).
[ ] ``POST unrelated attribute edits with stale revision both succeed`` (727): `stateRoot`, `readState` (733, 754).
[ ] ``POST concurrent stale text Changes amend second as amb-conflict child`` (761): `stateRoot`, `readFullState` (780).
[ ] ``POST concurrent stale name Changes amend second as amb-conflict child`` (787): `stateRoot`, `readFullState` (803).
[ ] ``POST concurrent stale class Changes merge set delta and succeed`` (811): `stateRoot`, `readState` (835).
[ ] ``POST unrelated structural edits with stale revision both succeed`` (842): `stateRoot`, `readFullState` (873).
[ ] ``POST same-parent structural collision amends and succeeds`` (880): `stateRoot`, `readFullState` (909).
[ ] ``POST duplicate submissionId with stale revision stays idempotent`` (918): `stateRoot`, `readState` (936).
[ ] ``POST changes creates log file`` (943): CoreDoor path; no HTTP state helper.
[ ] ``Snapshot writes amb artifacts asynchronously after change`` (956): file snapshot; no HTTP state helper.
[ ] ``Log contains valid change data after POST`` (968): log file; no HTTP state helper.
[ ] ``DB is present in db mode`` (982): `stateRoot`.
[ ] ``DB rows persist after agent cache reset`` (993): `stateRoot`.
[ ] ``DB rows survive second server startup without DB reset`` (1005): `stateRoot`.
[ ] ``DB authority does not import files when database is empty`` (1018): `readState` (1023), `userTreeShape`.
[ ] ``legacy Persistence:Mode file still uses Database persistence`` (1029): `readState` (1034), `userTreeShape`.
[ ] ``DB restart preserves NodeIds`` (1043): `stateRoot`, `readState` (1050).
[ ] ``DB restart replays log when projection is cleared`` (1057): `stateRoot`, `readState` (1078).
[ ] ``DB restart keeps duplicate submissionId idempotent`` (1085): `stateRoot`, `readState` (1103).
[ ] ``DB restart keeps inverse Change in EventLog`` (1113): `stateRoot`, `readPoll`, `readState` (1128).
[ ] ``New server uses snapshot + log replay`` (1133): `CoreDoor.getState`; no HTTP `readState`.
[ ] ``user css is served from canonical SYSTEM path`` (413): static asset GET; stays in StateEndpointTests.fs (not a `GET state` named test).

#### (c) ``GET state scope full returns canonical graph for tests``

[ ] Listed under (a); it remains the sole test that must keep `readFullState` / `?scope=full` because it asserts the FullGraph bootstrap scope itself.

### Copy-from

[ ] `Want.compose` in `src/Shared/Want.fs` lines 11–23 keeps Absent (Unloaded) ids in the Want list via `GraphChildren.isLoaded`.
[ ] `ResidentProjection.installWantAnswer` in `src/Shared/ResidentProjection.fs` lines 207–221 installs Poll `nodes` and `childMap` into the Graph the tests should mirror after each Poll round.
[ ] `BrowserResidencyClientTests.residencyFixture` in `tests/Server.Tests/BrowserResidencyClientTests.fs` lines 52–78 builds `browser` via `ResidentProjection.visibleClosureGraph`, then `ViewModel.buildSiteMapFrom` and `ViewModel.expandEntry` for a faithful SiteMap when the slice uses option (b).
[ ] `currentWant`, `currentPollRequest`, and `continueAutoWant` in `src/Client` define the Poll chain the new `readState` loop copies at the HTTP layer.
[ ] `runPollServer` in `src/Client/App.fs` lines 213–216 POSTs JSON from `encodePollRequestBody (currentPollRequest eventId model)`.
[ ] `postPoll` in `StateEndpointTests.fs` is the HTTP Poll caller to extend with non-empty `want`; today its `want` is hard-coded `[]`.
[ ] Desktop upload keeps `AmbitSession.getFullState` (`/state?scope=full`) unchanged per slice; out of scope for this ticket.

### write-ticket

[ ] Source files this slice would change: none under `src/` (see item 8).
[ ] `tests/Server.Tests/StateEndpointTests.fs`: replace `readState` body with client-path GET + Poll loop; switch four `readFullState` read-backs to `readState` except the scope-full GET test; move GET-state tests out; share helpers or duplicate minimal decode/Poll glue in the new file as the implementer chooses.
[ ] New file under `tests/Server.Tests/` (name chosen at implement time) holding the four ``GET state …`` tests plus any shared GET helpers they need.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj`: add a `<Compile Include="…">` entry for the new file adjacent to the existing `StateEndpointTests.fs` entry (currently line 95).

### Helper bodies (StateEndpointTests.fs)

[ ] `getStateJson` (78–83): `GetAsync("/ambit/state")`, assert OK and JSON content type, return body string.
[ ] `getStateJsonFull` (86–91): `GetAsync("/ambit/state?scope=full")`, assert OK, return body string.
[ ] `readFullState` (93–96): `getStateJsonFull` then `(decodeEventId json, decodeGraph json)`.
[ ] `readState` (99): alias to `readFullState`.
[ ] `readClosure` (101–104): `getStateJson` then `(decodeEventId json, decodeGraph json)`.
[ ] `stateRoot` (106–108): `readState` then `graph.root`.
[ ] `postPoll` (116–123): POST `/ambit/poll` with `{ eventId; want = [] }`.
[ ] `decodeGraph` (67–70): required field `"graph"` with `Serialization.decodeGraph`.
[ ] `decodeEventId` (23–26): required field `"eventId"` with `EventJson.decodeEventId`.
[ ] `decodePoll` (72–75): full `ChangeSuccessResponse` including `nodes` and `childMap` for installing Poll answers.

### Four older readFullState read-backs (audit lines vs current branch)

[ ] ``POST concurrent stale text Changes amend second as amb-conflict child`` line 780 (audit ~777): asserts stored text on `nodeX` and amb-conflict child under `nodeX`; client path fits because Poll round one loads ROOT child Header and round two loads the conflict child Header.
[ ] ``POST concurrent stale name Changes amend second as amb-conflict child`` line 803 (audit ~800): asserts stored name and amb-conflict child; client path for the same nested-Header reason.
[ ] ``POST unrelated structural edits with stale revision both succeed`` line 873 (audit ~870): asserts grandchildren under two ROOT children; client path fits because a second Poll round loads child Headers under each parent.
[ ] ``POST same-parent structural collision amends and succeeds`` line 909 (audit ~906): asserts two children under one parent; client path fits because Poll loads parent then both child Headers; none of the four asserts the `?scope=full` query itself.

### Gambol.Server.Tests.fsproj registration

[ ] Path: `tests/Server.Tests/Gambol.Server.Tests.fsproj`.
[ ] `<Compile Include="StateEndpointTests.fs" />` appears at line 95 inside the main `<ItemGroup>` of test modules (lines 85–98 show neighboring includes such as `DbAgentFailureTests.fs` before and `SearchActorTests.fs` after).

### userRootChildren and userTreeShape

[ ] `userRootChildren` filters ROOT children with `graph.nodes.[c.id]` and `isUserNode` (`SpecialNodeTestHelpers.fs` 45–47); an Absent child id throws on index.
[ ] `userTreeShape` walks the tree through `graph.nodes.[nodeId]` (50–61); Absent Headers throw during traversal.

### Decode types for GET state and Poll

[ ] GET state tests decode `eventId` with `decodeEventId` and `graph` with `decodeGraph` from the GET JSON envelope (not the full `StateResponse` record type).
[ ] Poll and Change success share `ChangeSuccessResponse` with `nodes: Node list` and `childMap: Map<NodeId, ChildNode list>` (`src/Shared/ApiResponses.fs` 27–42).
[ ] Tests call `decodePoll` / `decodeSuccess` wrapping `ApiResponseSerialization.decodeChangeSuccessResponseDecoder` for Poll bodies (`StateEndpointTests.fs` 28–35, 72–75, 136–140).

### src/ change expectation

[ ] No `src/` file must change for this slice: Browser boot, Poll handler, `Want.compose`, and `installWantAnswer` already implement the client path; only test helpers and file placement change.

### readState callers by test name (audit confirmation on `cursor/read-state-client-path-f359`)

#### Fifteen content read-backs (stored nested Headers or ROOT child fields)

[ ] ``POST Change and inverse Changes return complete confirmations in request order``: `readState` line 452 (audit 452).
[ ] ``POST changes SetText changes child text and bumps revision``: line 560 (audit 560).
[ ] ``POST changes NewNode+Replace adds child to root``: line 576 (audit 576).
[ ] ``POST changes twice bumps revision to 2``: line 616 (audit 616).
[ ] ``POST changes batch with two changes bumps revision to 2``: line 637 (audit 637).
[ ] ``POST changes batch with bad second change keeps earlier items``: line 651 (audit 651).
[ ] ``POST changes persists in GET state``: line 674 (audit 674).
[ ] ``POST same submissionId twice is idempotent``: line 705 (audit 705).
[ ] ``POST with stale base revision and valid SetText succeeds``: line 721 (audit 721).
[ ] ``POST unrelated attribute edits with stale revision both succeed``: lines 733 and 754 (audit 733, 754).
[ ] ``POST concurrent stale class Changes merge set delta and succeed``: line 835 (audit 835).
[ ] ``POST duplicate submissionId with stale revision stays idempotent``: line 936 (audit 936).
[ ] ``DB restart preserves NodeIds``: line 1050 (audit 1050).
[ ] ``DB restart replays log when projection is cleared``: line 1078 (audit 1078).
[ ] ``DB restart keeps duplicate submissionId idempotent``: line 1103 (audit 1103).

#### Eight incidental callers (eventId, ROOT, or empty tree only on default GET today)

[ ] ``GET state returns revision 0 for fresh server``: `readState` line 397 (audit cited revision read at ~278; current direct call is 397).
[ ] ``GET state returns valid graph with root node``: `readState` line 404 (audit 404).
[ ] ``POST explicit Undo JSON is rejected``: line 517 (audit 517).
[ ] ``POST unchanged submission is rejected``: line 529 (audit 529).
[ ] ``DB authority does not import files when database is empty``: line 1023 (audit 1023).
[ ] ``legacy Persistence:Mode file still uses Database persistence``: line 1034 (audit 1034).
[ ] ``DB restart keeps inverse Change in EventLog``: line 1128 (audit 1128).
[ ] `stateRoot` (lines 106–108): every `stateRoot client` call site inherits `readState`; audit listed `stateRoot` at ~105 (current definition 106–108).

### Issue number 18

[ ] `plan/client-never-fails/issues/` contains numbered issue folders 01 through 17 and no folder 18; issue number 18 is free for **18 — Read stored state on the client path**.
