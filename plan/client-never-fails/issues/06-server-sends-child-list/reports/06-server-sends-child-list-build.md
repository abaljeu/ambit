# Build — [06 — Server sends each Node with its child list](../06-server-sends-child-list-ticket.md)

Alan's clarification of 2026-11-02 is the return for a requested NodeId.

Was: the Server returns the children of that Node and their content, the children's Headers.

Is: the Server returns that Node's own Header and its children as a list of child ids. The children's Headers stay out. A child id with no Header in the Browser is Absent. Every returned Node carries its child list, so it is Loaded.

`ChildrenStatus` and the `childrenStatus` wire field stay. `includeWorkspace` stays.

Story [Remove Loaded and Unloaded](../../../spec.md) step 13 and architecture path 6.1 and path 6.2 in [Client never fails on server data architecture](../../../arch.md) match this return. Those files stay as they are.

The size limit is 4 tests. This run changes more tests. The 2026-11-02 instruction allows each test whose expected answer changes only because of this return.

## Files

[ ] `src/Shared/ResidentProjection.fs` — `wantAnswer` and `visibleClosureWantAnswer` return the requested Header and its child-id list.
[ ] `src/Server/Api.fs` — `nodesForRequest` returns that same package, and `UniversalResponse` carries the list.
[ ] `src/Shared/ApiResponses.fs` — `UniversalResponse` gains `childMap`.
[ ] `src/Shared/ApiResponseSerialization.fs` — encode and decode carry `childMap`.
[ ] `tests/Shared.Tests/WantTests.fs`
[ ] `tests/Shared.Tests/SerializationTests.fs`
[ ] `tests/Shared.Tests/LoadCaptureTests.fs`
[ ] `tests/Server.Tests/BrowserResidencyClientTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandClientTests.fs` — `gitResponse` carries an empty child list so the record builds.
[ ] `tests/Server.Tests/ApiGetStateTests.fs`
[ ] `tests/Server.Tests/ApiPostLoadTests.fs`
[ ] `tests/Server.Tests/StateEndpointTests.fs` — ``GET state default returns ROOT closure excluding nested workspace contents`` reads the default GET. `readState` reads `?scope=full` so a stored Header stays visible to the persistence checks.
[ ] `06-server-sends-child-list-ticket.md` — One job and Process state the Was/Is return. Status is `coded`.

## Tests

[ ] ``wantAnswer returns the requested Header and its child list`` — renamed from ``wantAnswer returns wanted edges and pointed-at Nodes``. The nodes are the requested Header. The child ids stay on the list.
[ ] ``wantAnswer keeps a child id when that Header is absent`` — renamed from ``wantAnswer omits an edge with an absent target Node``. The child id stays in the list. The requested Header is returned.
[ ] ``visibleClosureGraph loads reserved Children and Zoom ancestors`` — `buriedId` is a child id of the directory, so its Header is absent and its status stays `Unloaded`. The Zoom file Header is returned and `Loaded`.
[ ] ``visibleClosureGraph bad Zoom stays inside reserved set`` — the workspace id is a child of Workspaces, so its Header is absent. The id stays on the Workspaces list. `rootBootstrapGraph` stays as it is.
[ ] ``UniversalResponse round-trips nodes events and latestId`` — the record carries a child list, and the round-trip asserts that list.
[ ] ``Poll answer chains the next Included residency Want`` — the Poll returns the parent Header and the child id. The child Header stays absent. The next Want is that child id.
[ ] ``PollDone leaves Sync Idle when the edit box holds new text`` — the edit fixture reads the Server graph so the parent Header is present. The Idle result stays. The closure leaves that child Header out.
[ ] ``wantAnswerForTargets returns the target Header and its child list`` — renamed from ``wantAnswerForTargets returns direct Children and pointed-at Nodes``. The nodes are the target Header. The child id stays on the list. `includeWorkspace` stays.
[ ] ``captureLoadResponse shares event id for Changes and Nodes`` — the target Header is in `nodes`. Its child list is empty.
[ ] ``getState zoom outside ROOT adds owning Workspace`` — the file id is a child of the Zoom directory, so the file Header is absent and the id stays on the directory list.
[ ] ``getState without zoom keeps nested Workspace Unloaded`` — the workspace id is a child of Workspaces, so its Header is absent and the status stays `Unloaded`.
[ ] ``GET state default returns ROOT closure excluding nested workspace contents`` — the workspace Header is absent. The workspace id stays on the Workspaces list. The inner id stays absent.
[ ] ``postLoad Want answer when includeWorkspace true`` — the nodes are the directory Header. The file id stays on the child list. `includeWorkspace` stays.
[ ] ``postLoad shares one event id for Events and Nodes`` — the directory Header is in `nodes`. The file Header stays out. The file id stays on the directory list.
[ ] ``POST with stale base revision and valid SetText succeeds`` — `readState` reads `?scope=full` so the stored child Header is visible.
[ ] ``POST concurrent stale class Changes merge set delta and succeed`` — same `readState` path.
[ ] ``POST changes twice bumps revision to 2`` — same `readState` path.
[ ] ``POST same submissionId twice is idempotent`` — same `readState` path.
[ ] ``DB restart replays log when projection is cleared`` — same `readState` path.
[ ] ``POST Change and inverse Changes return complete confirmations in request order`` — same `readState` path.
[ ] ``POST changes persists in GET state`` — same `readState` path.
[ ] ``POST changes batch with bad second change keeps earlier items`` — same `readState` path.
[ ] ``POST changes SetText changes child text and bumps revision`` — same `readState` path.
[ ] ``DB restart preserves NodeIds`` — same `readState` path.
[ ] ``POST changes batch with two changes bumps revision to 2`` — same `readState` path.
[ ] ``POST changes NewNode+Replace adds child to root`` — same `readState` path.
[ ] ``DB restart keeps duplicate submissionId idempotent`` — same `readState` path.
[ ] ``POST duplicate submissionId with stale revision stays idempotent`` — same `readState` path.
[ ] ``POST unrelated attribute edits with stale revision both succeed`` — same `readState` path.

## Check

[ ] Mapped `scripts/agent-test.sh`: Server 106 passed. Shared 518 passed.
[ ] Named filters: Shared 7 passed. Server 6 passed.
[ ] Full `scripts/agent-test.sh --full`: Server 733 passed, 1 skipped. Shared 1929 passed, 3 skipped.
[ ] Verdict file [06 — Server sends each Node with its child list check](../../../reports/06-server-sends-child-list-check.md). The first line is `pass`.
