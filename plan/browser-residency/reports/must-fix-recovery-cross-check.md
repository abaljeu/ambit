# Must-fix recovery cross-check: 09 — Migrate Server Sync doors

Subject: the three must-fixes in [Independent code review: 09 — Migrate Server Sync doors](independent-review-09.md). Inspection of the committed 09 code. [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md) Status and [Browser residency](plan/browser-residency/project.md) Stage stay unchanged.

Unpublished working-tree edits already apply this same recovery. Do not invent a second shape. The symbols below are the committed tip.

## 1. Verdict — Three recoveries, all local

1. **Orphan bootstrap door** — Delete unused `ResidentProjection.bootstrapStateResponse`. Keep `bootstrapGraph`, `rootBootstrapGraph`, and `visibleClosureGraph`.
2. **One-off snapshot tuples** — Replace the new Server helper tuples with one private named record reused by `wantAnswerFromHandle` and `loadPackages`.
3. **Load package type contraction** — Restore `LoadResponse.packages` and `packageChildMap` on the Shared type, codec mapping, `loadResponseToSync`, and tests. Keep JSON and `postLoad` on `nodes` plus `childMap`. Leave `packagesForTargets` and `installPackages` for [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md).

## 2. Remove orphan `ResidentProjection.bootstrapStateResponse`

Finding [2.1 Remove the orphaned bootstrap response door](independent-review-09.md). [Server Api](src/Server/Api.fs) `getState` already scopes with `ResidentProjection.visibleClosureGraph`. [ResidentProjection](src/Shared/ResidentProjection.fs) still defines `bootstrapStateResponse` (wraps `bootstrapGraph` onto `StateResponse`) with no F# caller.

Minimal change:

1. **Delete the orphan** — Remove `bootstrapStateResponse` only.
2. **Keep live helpers** — Leave `bootstrapGraph` and `rootBootstrapGraph` (used by [BootCache](src/Shared/BootCache.fs) and Shared tests) and leave `visibleClosureGraph` as the production State projection.

## 3. Name the snapshot answer used by both Server helpers

Finding [2.2 Name the new snapshot answer results](independent-review-09.md). [Server Api](src/Server/Api.fs) `wantAnswerFromHandle` returns `State * Map<NodeId, ChildNode list> * Node list`. `loadPackages` returns `State * Result<Node list * Map<NodeId, ChildNode list>, LoadRefuse>`. Callers: `postPoll`, `postEvents`, `postLoad`.

Minimal change, in [Server Api](src/Server/Api.fs) only:

1. **Add one private record** — `SnapshotAnswer` with `state`, `nodes`, and `childMap`. Do not lift it to Shared. Do not reuse `ChangeSuccessResponse` or `LoadResponse` (those carry stamp fields this helper does not own).
2. **Retype the helpers** — `wantAnswerFromHandle`: `Async<Result<SnapshotAnswer, string>>`. `loadPackages`: `Async<Result<Result<SnapshotAnswer, LoadRefuse>, string>>`. Map `packagesForTargets` into that record; leave the Shared helper’s own tuple.
3. **Match on the record** — `postPoll` and `postEvents` read `answer.state`, `answer.nodes`, `answer.childMap`. `postLoad` success fills the Load record from `answer` and still encodes JSON `nodes` plus `childMap`.

Do not rename `packagesForTargets` or `wantAnswer`. Those Shared tuples are not this finding.

## 4. Restore `LoadResponse.packages` / `packageChildMap` without moving the Server wire

Finding [3.1 Leave legacy Load package type contraction to 12 — Contract old Load Fetch packages](independent-review-09.md). [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) keeps `LoadResponse.packages` for the later contract. [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md) item 31.4 owns removal. Committed 09 already renamed the type fields to `nodes` / `childMap`.

Keep Server wire: [Server Api](src/Server/Api.fs) `postLoad` still encodes JSON `nodes` plus `childMap`. Do not encode JSON `packages`.

Minimal change — restore F# field names; alias them to the current JSON keys:

1. **Type** — [ApiResponses](src/Shared/ApiResponses.fs) `LoadResponse`: replace `nodes` / `childMap` with `packages` / `packageChildMap`. Do not add a second F# field pair.
2. **Codec** — [ApiResponseSerialization](src/Shared/ApiResponseSerialization.fs) `encodeLoadResponse` / `decodeLoadResponseDecoder`: keep required JSON keys `nodes` and `childMap`; read and write `response.packages` and `response.packageChildMap`.
3. **Apply map** — [SyncLogic](src/Shared/SyncLogic.fs) `loadResponseToSync`: `nodes = response.packages`, `childMap = response.packageChildMap`, and keep Sync `packages` / `packageChildMap` empty so apply stays on `installWantAnswer`.
4. **Capture** — [ResidentProjection](src/Shared/ResidentProjection.fs) `captureLoadResponse`: assign `packages` / `packageChildMap` from `packagesForTargets`.
5. **Server fill** — [Server Api](src/Server/Api.fs) `postLoad`: fill `packages` / `packageChildMap` from the snapshot answer; still call `encodeLoadResponse` (JSON `nodes` / `childMap`).
6. **Tests** — Restore F# field names on Load record construction and asserts: [SerializationTests](tests/Shared.Tests/SerializationTests.fs) (`LoadResponse round-trip with nodes and childMap`, `LoadResponse round-trip keeps Unloaded Node Children absent`, keep `LoadResponse decoder requires nodes and childMap` as a JSON-key test), [LoadCaptureTests](tests/Shared.Tests/LoadCaptureTests.fs) (`captureLoadResponse shares event id for Changes and Nodes`, `LoadResponse toSyncResponse preserves Changes and Nodes`), [ApiPostLoadTests](tests/Server.Tests/ApiPostLoadTests.fs) (five `postLoad` asserts that currently read `response.nodes` / `response.childMap`).

Do not change [App](src/Client/App.fs) (decode only), `loadResponseToPoll`, `applyLoadResponse`, or `changeSuccessToSync`. Do not delete `packagesForTargets` or `installPackages`.

## 5. Out of scope

1. **Ticket Status** — [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md) stays `coded`.
2. **Project Stage** — [Browser residency](plan/browser-residency/project.md) Stage stays `build`.
3. **Contract work** — [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md) still owns later removal of `LoadResponse.packages`, `packageChildMap`, `packagesForTargets`, and `installPackages`.
